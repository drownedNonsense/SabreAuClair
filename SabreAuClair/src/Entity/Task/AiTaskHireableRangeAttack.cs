using System;
using System.Linq;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public class AiTaskHierableRangeAttack : AiTaskBaseTargetable {

        //=======================
        // D E F I N I T I O N S
        //=======================
            
            protected IHireable hireable;

            protected int durationMs;
            protected int releaseAtMs;

            protected long lastSearchTotalMs;
            protected float maxDist    = 15f;
            protected float minDist    = 4f;
            protected int searchWaitMs = 2000;

            protected float NowMaxDist => this.entity.WatchedAttributes.GetBool("commandAggro") ? this.maxDist * 1.5f : this.maxDist;

            protected float accum = 0f;

            protected bool didShoot;
            protected bool immobile;

            protected float minTurnAnglePerSec;
            protected float maxTurnAnglePerSec;
            protected float curTurnRadPerSec;

            protected float projectileDamage;
            protected AssetLocation projectileCode;

            protected float maxTurnAngleRad;
            protected float maxOffAngleThrowRad;
            protected float spawnAngleRad;

            protected Item[] items;

            protected int aimingRenderVariant;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public AiTaskHierableRangeAttack(EntityAgent entity) : base(entity) {}


            public override void LoadConfig(
                JsonObject taskConfig,
                JsonObject aiConfig
            ) {

                base.LoadConfig(taskConfig, aiConfig);
                this.durationMs       = taskConfig["durationMs"].AsInt(1500);
                this.releaseAtMs      = taskConfig["releaseAtMs"].AsInt(1000);
                this.projectileDamage = taskConfig["projectileDamage"].AsFloat(1f);
                this.maxDist          = taskConfig["maxDist"].AsFloat(15f);
                this.minDist          = taskConfig["minDist"].AsFloat(4f);
                this.projectileCode   = AssetLocation.Create(taskConfig["projectileCode"].AsString("sabreauclair:hireable-arrow"));

                this.aimingRenderVariant = taskConfig["aimingRenderVariant"].AsInt(0);

                this.immobile = taskConfig["immobile"].AsBool(false);

                this.maxTurnAngleRad     = taskConfig["maxTurnAngleDeg"].AsFloat(360) * GameMath.DEG2RAD;
                this.maxOffAngleThrowRad = taskConfig["maxOffAngleThrowDeg"].AsFloat(0) * GameMath.DEG2RAD;
                this.spawnAngleRad       = this.entity.Attributes.GetFloat("spawnAngleRad");

                List<Item> items = new();
                foreach (string wildcard in taskConfig["itemCodes"].AsArray<string>())
                    items.AddRange(this.world.SearchItems(new AssetLocation(wildcard)));

                this.items = items.ToArray();

            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================
            
            public override bool ShouldExecute() {

                if ((this.hireable = this.entity as IHireable) == null) return false;

                if (this.entity.ActiveHandItemSlot.Empty)                                      return false;
                if (!this.items.Any(x => this.entity.ActiveHandItemSlot.Itemstack?.Item == x)) return false;

                // React immediately on hurt, otherwise only 1/10 chance of execution
                if (rand.NextSingle() > 0.1f && (this.whenInEmotionState == null || this.bhEmo?.IsInEmotionState(this.whenInEmotionState) != true)) return false;

                if (this.whenInEmotionState    != null && this.bhEmo?.IsInEmotionState(this.whenInEmotionState) != true)    return false;
                if (this.whenNotInEmotionState != null && this.bhEmo?.IsInEmotionState(this.whenNotInEmotionState) == true) return false;
                if (this.lastSearchTotalMs   + this.searchWaitMs > this.entity.World.ElapsedMilliseconds)                   return false;
                if (this.whenInEmotionState == null && rand.NextDouble() > 0.5f)                                            return false;
                if (this.cooldownUntilMs     > this.entity.World.ElapsedMilliseconds)                                       return false;

                float range = this.NowMaxDist;
                this.lastSearchTotalMs = this.entity.World.ElapsedMilliseconds;

                this.targetEntity = this.partitionUtil.GetNearestInteractableEntity(
                    this.entity.ServerPos.XYZ,
                    range,
                    (e) => this.IsTargetableEntity(e, range) && this.AimableDirection(e)
                ); // ..

                if (this.targetEntity == null) return false;
                if (this.targetEntity.ServerPos.SquareDistanceTo(this.entity.ServerPos) < this.minDist * this.minDist) {
                    this.bhEmo.TryTriggerState("retreat", this.targetEntity.EntityId);
                    return false;
                } // if ..

                this.projectileDamage = this.entity.ActiveHandItemSlot.Itemstack.ItemAttributes["damage"].AsFloat(this.projectileDamage);

                return true;

            } // bool ..


            public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false) {
                if (base.IsTargetableEntity(e, range, ignoreEntityCode)) return true;
                else return this.HireableIsTargetableEntity(this.hireable, e, this.attackedByEntity);
            } // bool ..


            public override bool CanSense(Entity e, double range) {
                if (!base.CanSense(e, range)) return false;
                return this.LightBasedCanSense(e, range);
            } // bool ..


            private bool AimableDirection(Entity e) {

                if (!this.immobile) return true;

                float aimYaw = this.GetAimYaw(e);
                return aimYaw > this.spawnAngleRad - this.maxTurnAngleRad - this.maxOffAngleThrowRad && aimYaw < this.spawnAngleRad + this.maxTurnAngleRad + this.maxOffAngleThrowRad;

            } // bool ..


            public override void StartExecute() {

                base.StartExecute();

                this.accum    = 0;
                this.didShoot = false;

                if (this.entity?.Properties.Server?.Attributes != null) {
                    if (this.entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder") is ITreeAttribute pathfinder) {

                        this.minTurnAnglePerSec = pathfinder.GetFloat("minTurnAnglePerSec", 250);
                        this.maxTurnAnglePerSec = pathfinder.GetFloat("maxTurnAnglePerSec", 450);

                    } // if ..
                } else {

                    this.minTurnAnglePerSec = 250;
                    this.maxTurnAnglePerSec = 450;

                } // if ..

                this.curTurnRadPerSec  = this.minTurnAnglePerSec + this.entity.World.Rand.NextSingle() * (this.maxTurnAnglePerSec - this.minTurnAnglePerSec);
                this.curTurnRadPerSec *= GameMath.DEG2RAD * 50 * 0.02f;
                
                this.entity.ActiveHandItemSlot.Itemstack?.Attributes.SetInt("renderVariant", this.aimingRenderVariant);
                this.entity.ActiveHandItemSlot.MarkDirty();

            } // void ..



            public override bool ContinueExecute(float dt) {
                
                float desiredYaw = GameMath.Clamp(this.GetAimYaw(this.targetEntity), spawnAngleRad - maxTurnAngleRad, spawnAngleRad + maxTurnAngleRad);
                float yawDist    = GameMath.AngleRadDistance(this.entity.ServerPos.Yaw, desiredYaw);
                this.entity.ServerPos.Yaw += GameMath.Clamp(yawDist, -this.curTurnRadPerSec * dt, this.curTurnRadPerSec * dt);
                this.entity.ServerPos.Yaw %= GameMath.TWOPI;

                if (this.animMeta != null) {

                    this.animMeta.EaseInSpeed  = 1f;
                    this.animMeta.EaseOutSpeed = 1f;
                    this.entity.AnimManager.StartAnimation(this.animMeta);
                    
                } // if ..

                this.accum += dt;

                if (this.accum > this.releaseAtMs / 1000f && !this.didShoot) {

                    this.didShoot = true;

                    AssetLocation location = projectileCode.Clone();
                    EntityProperties type  = entity.World.GetEntityType(location);
                    EntityHireableProjectile projectile = this.entity.World.ClassRegistry.CreateEntity(type) as EntityHireableProjectile;
                    projectile.FiredBy            = entity;
                    projectile.Damage             = projectileDamage;
                    projectile.ProjectileStack    = new ItemStack(this.entity.World.GetItem(new AssetLocation(type.Attributes["itemStack"].AsString()) ?? location));
                    projectile.DropOnImpactChance = 0;

                    Vec3d pos       = this.entity.ServerPos.XYZ.Add(0, this.entity.LocalEyePos.Y, 0);
                    Vec3d targetPos = this.targetEntity.ServerPos.XYZ.Add(0, this.targetEntity.LocalEyePos.Y, 0) + this.targetEntity.ServerPos.Motion * 8;
                    double dist   = Math.Pow(pos.SquareDistanceTo(targetPos), 0.1);
                    double height = dist * dist * 0.003 * projectile.WatchedAttributes.GetDouble("gravityFactor", 1);

                    Vec3d velocity = (targetPos + new Vec3d(0, height, 0) - pos).Normalize() * this.entity.Stats.GetBlended("bowDrawingStrength");

                    projectile.ServerPos.SetPos(this.entity.ServerPos.BehindCopy(0.21).XYZ.Add(0, this.entity.LocalEyePos.Y, 0));
                    projectile.ServerPos.Motion.Set(velocity);
                    entity.World.SpawnEntity(projectile);

                    this.entity.ActiveHandItemSlot.Itemstack.Item.DamageItem(this.world, this.entity, this.entity.ActiveHandItemSlot);
                    
                } // if ..

                return this.accum < this.durationMs * 0.001f;

            } // bool ..


            public override void FinishExecute(bool cancelled) {
                base.FinishExecute(cancelled);
                this.entity.ActiveHandItemSlot.Itemstack?.Attributes.SetInt("renderVariant", 0);
                this.entity.ActiveHandItemSlot.MarkDirty();
            } // void ..


            private float GetAimYaw(Entity targetEntity) {

                Vec3f targetVec = new (
                    (float)targetEntity.ServerPos.X - (float)entity.ServerPos.X,
                    (float)targetEntity.ServerPos.Y - (float)entity.ServerPos.Y,
                    (float)targetEntity.ServerPos.Z - (float)entity.ServerPos.Z
                ); // ..
                
                return MathF.Atan2(targetVec.X, targetVec.Z);

            } // float ..
    } // class ..
} // namespace ..
