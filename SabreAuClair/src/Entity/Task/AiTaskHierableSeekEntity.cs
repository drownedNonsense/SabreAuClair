using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public class AiTaskHierableSeekEntity : AiTaskBaseTargetable {

        //=======================
        // D E F I N I T I O N S
        //=======================

            protected IHireable hireable;

            protected Vec3d targetPos;

            protected float moveSpeed = 0.02f;
            protected float seekingRange = 25f;
            protected float tacticalRetreatRange = 20f;
            protected float maxFollowTime = 60;

            protected float NowSeekingRange => this.entity.WatchedAttributes.GetBool("commandAggro") ? this.seekingRange * 1.5f : this.seekingRange;

            protected bool stopNow = false;
            protected bool active = false;
            protected float currentFollowTime = 0;

            protected bool alarmBand = false;

            protected EnumAttackPattern attackPattern;

            protected long finishedMs;

            protected long lastSearchTotalMs;
            protected long tacticalRetreatBeginTotalMs;
            protected long attackModeBeginTotalMs;
            protected long lastHurtByTargetTotalMs;

            protected float extraTargetDistance = 0f;

            protected bool lastPathfindOk;

            protected int searchWaitMs = 4000;

            protected bool RemainInTacticalRetreat => this.entity.World.ElapsedMilliseconds - this.tacticalRetreatBeginTotalMs < 20000;
            protected bool RemainInOffensiveMode   => this.entity.World.ElapsedMilliseconds - this.attackModeBeginTotalMs < 20000;

            protected Vec3d lastGoalReachedPos;
            protected Dictionary<long, int> futilityCounters;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public AiTaskHierableSeekEntity(EntityAgent entity) : base(entity) {}

            public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
                
                base.LoadConfig(taskConfig, aiConfig);

                moveSpeed            = taskConfig["movespeed"].AsFloat(0.02f);
                extraTargetDistance  = taskConfig["extraTargetDistance"].AsFloat(0f);
                seekingRange         = taskConfig["seekingRange"].AsFloat(25);
                maxFollowTime        = taskConfig["maxFollowTime"].AsFloat(60);
                alarmBand            = taskConfig["alarmBand"].AsBool(false);
                retaliateAttacks     = taskConfig["retaliateAttacks"].AsBool(true);

            } // void ..


            public override bool ShouldExecute() {

                if ((this.hireable = this.entity as IHireable) == null) return false;

                if (rand.NextSingle() > 0.1f && (this.whenInEmotionState == null || this.bhEmo?.IsInEmotionState(this.whenInEmotionState) != true)) return false;

                if (this.whenInEmotionState    != null && this.bhEmo?.IsInEmotionState(this.whenInEmotionState) != true)    return false;
                if (this.whenNotInEmotionState != null && this.bhEmo?.IsInEmotionState(this.whenNotInEmotionState) == true) return false;
                if (this.lastSearchTotalMs   + this.searchWaitMs > this.entity.World.ElapsedMilliseconds)                   return false;
                if (this.whenInEmotionState == null && rand.NextDouble() > 0.5f)                                            return false;
                if (this.cooldownUntilMs     > this.entity.World.ElapsedMilliseconds)                                       return false;

                this.lastSearchTotalMs = this.entity.World.ElapsedMilliseconds;

                this.targetEntity = (this.retaliateAttacks
                    && this.attackedByEntity != null
                    && this.attackedByEntity.Alive
                    && this.attackedByEntity.IsInteractable
                    && this.IsTargetableEntity(this.attackedByEntity, this.NowSeekingRange)
                ) ? this.attackedByEntity
                    : this.partitionUtil.GetNearestInteractableEntity(
                    this.entity.ServerPos.XYZ,
                    this.seekingRange * 2f,
                    (e) => this.IsTargetableEntity(e, this.NowSeekingRange)
                ); // ..

                this.attackedByEntityMs      = this.entity.World.ElapsedMilliseconds;
                this.lastHurtByTargetTotalMs = this.entity.World.ElapsedMilliseconds;

                this.targetPos = this.targetEntity?.ServerPos.XYZ;
                return this.targetEntity != null;

            } // bool ..


            public float MinDistanceToTarget => this.extraTargetDistance + MathF.Max(0.1f, this.targetEntity.SelectionBox.XSize / 2 + this.entity.SelectionBox.XSize / 4);


            public override void StartExecute() {

                foreach (Entity entity in this.world.GetEntitiesAround(
                    this.entity.Pos.XYZ,
                    this.seekingRange * 2F,
                    this.seekingRange,
                    (e) => {
                        if (this.entity.HerdId != 0 && this.entity.HerdId == e.WatchedAttributes.GetLong("herdId")) return true;

                        if (e is IHireable targetMercenary)
                            if (hireable.Commander == null) return this.entity.WatchedAttributes.GetBool("commandAggro");
                            else if (hireable.Commander.PlayerUID == targetMercenary.Commander?.PlayerUID) return true;

                        return false;

                    } // ..
                )) if (entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager.GetTask<AiTaskHierableSeekEntity>() is AiTaskHierableSeekEntity task)
                    task.targetEntity ??= this.targetEntity;

                this.stopNow           = false;
                this.active            = true;
                this.currentFollowTime = 0;

                if (this.RemainInTacticalRetreat) {

                    TryTacticalRetreat();
                    return;

                } // if ..


                this.attackPattern = EnumAttackPattern.DirectAttack;

                
                // 1 in 20 times we do an expensive search
                int searchDepth = (this.world.Rand.NextSingle() < 0.05f) ? 10000 : 3500;

                this.pathTraverser.NavigateTo_Async(
                    this.targetPos.Clone(),
                    this.moveSpeed,
                    this.MinDistanceToTarget,
                    this.OnGoalReached,
                    this.OnStuck,
                    this.OnSeekUnable,
                    searchDepth,
                    1
                ); // ..
            } // void ..


            private void OnSeekUnable() {

                this.attackPattern = EnumAttackPattern.BesiegeTarget;
                this.pathTraverser.NavigateTo_Async(
                    this.targetPos.Clone(),
                    this.moveSpeed,
                    this.MinDistanceToTarget,
                    this.OnGoalReached,
                    this.OnStuck,
                    this.OnSiegeUnable,
                    3500,
                    3
                ); // ..
            } // void ..


            private void OnSiegeUnable() {
                if (this.targetPos.DistanceTo(this.entity.ServerPos.XYZ) < this.seekingRange)
                    if (!this.TryCircleTarget())
                        this.OnCircleTargetUnable();
            } // void ..


            public void OnCircleTargetUnable() =>
                this.TryTacticalRetreat();


            private bool TryCircleTarget() {

                bool giveUpWhenNoPath = this.targetPos.SquareDistanceTo(this.entity.Pos) < 12 * 12;
                int searchDepth       = 3500;
                this.attackPattern    = EnumAttackPattern.CircleTarget;
                this.lastPathfindOk   = false;

                // If we cannot find a path to the target, let's circle it!
                float angle = (float)Math.Atan2(this.entity.ServerPos.X - this.targetPos.X, this.entity.ServerPos.Z - this.targetPos.Z);
                
                for (int i = 0; i < 3; i++) {

                    // We need to avoid crossing the path of the target, so we do only small angle variation between us and the target 
                    double randAngle = angle + 0.5 + this.world.Rand.NextDouble() / 2;

                    double distance = 4 + this.world.Rand.NextDouble() * 6;

                    double dx = GameMath.Sin(randAngle) * distance;
                    double dz = GameMath.Cos(randAngle) * distance;
                    this.targetPos.Add(dx, 0, dz);

                    int tries    = 0;
                    bool ok      = false;
                    BlockPos tmp = new (this.targetPos.XInt, this.targetPos.YInt, this.targetPos.ZInt);

                    int dy = 0;
                    while (tries < 5) {

                        // Down ok?
                        if (this.world
                            .BlockAccessor
                            .GetBlock(tmp.X, tmp.Y - dy, tmp.Z)
                            .SideSolid[BlockFacing.UP.Index]
                        && !this.world
                            .CollisionTester
                            .IsColliding(this.world.BlockAccessor, this.entity.SelectionBox, new Vec3d(tmp.X + 0.5, tmp.Y - dy + 1, tmp.Z + 0.5), false)
                        ) {

                            ok = true;
                            this.targetPos.Y -= dy;
                            this.targetPos.Y++;
                            break;

                        } // if ..

                        // Up ok?
                        if (this.world
                            .BlockAccessor
                            .GetBlock(tmp.X, tmp.Y + dy, tmp.Z)
                            .SideSolid[BlockFacing.UP.Index]
                        && !world
                            .CollisionTester
                            .IsColliding(world.BlockAccessor, entity.SelectionBox, new Vec3d(tmp.X + 0.5, tmp.Y + dy + 1, tmp.Z + 0.5), false)
                        ) {

                            ok = true;
                            this.targetPos.Y += dy;
                            this.targetPos.Y++;
                            break;

                        } // if ..

                        tries++;
                        dy++;

                    } // while ..


                    if (ok) {

                        this.pathTraverser.NavigateTo_Async(
                            this.targetPos.Clone(),
                            this.moveSpeed,
                            this.MinDistanceToTarget,
                            this.OnGoalReached,
                            this.OnStuck,
                            this.OnCircleTargetUnable,
                            searchDepth,
                            1
                        ); // if ..
                        return true;

                    } // if ..
                } // for ..

                return false;
            } // bool ..


            private void TryTacticalRetreat() {

                if (this.RemainInOffensiveMode) return;
                if (this.RemainInTacticalRetreat) {

                    base.updateTargetPosFleeMode(this.targetPos);
                    float size = targetEntity.SelectionBox.XSize;
                    this.pathTraverser.WalkTowards(
                        this.targetPos,
                        this.moveSpeed,
                        size + 0.2f,
                        this.OnGoalReached,
                        this.OnStuck
                    ); // ..

                    if (this.attackPattern != EnumAttackPattern.TacticalRetreat)
                        this.tacticalRetreatBeginTotalMs = this.entity.World.ElapsedMilliseconds;

                    this.attackPattern    = EnumAttackPattern.TacticalRetreat;
                    this.attackedByEntity = null;
                } // if ..
            } // void ..


            public override bool CanContinueExecute() {
                if (this.pathTraverser.Ready)
                    this.lastPathfindOk = true;

                return this.pathTraverser.Ready || this.attackPattern == EnumAttackPattern.TacticalRetreat;

            } // bool ..


            float lastPathUpdateSeconds;
            public override bool ContinueExecute(float dt) {

                if (this.currentFollowTime == 0)
                    if (!this.stopNow || this.world.Rand.NextDouble() < 0.25)
                        base.StartExecute();

                this.tacticalRetreatRange = MathF.Max(20f, this.tacticalRetreatRange - dt * 0.25f);

                this.currentFollowTime     += dt;
                this.lastPathUpdateSeconds += dt;

                if (this.attackPattern == EnumAttackPattern.TacticalRetreat && this.world.Rand.NextDouble() < 0.2) {
                    base.updateTargetPosFleeMode(this.targetPos);
                    this.pathTraverser.CurrentTarget.X = this.targetPos.X;
                    this.pathTraverser.CurrentTarget.Y = this.targetPos.Y;
                    this.pathTraverser.CurrentTarget.Z = this.targetPos.Z;
                } // if ..


                if (this.attackPattern != EnumAttackPattern.TacticalRetreat) {
                    if (!this.lastPathfindOk)
                        this.TryTacticalRetreat();

                    if (this.attackPattern == EnumAttackPattern.DirectAttack
                    && this.lastPathUpdateSeconds >= 0.75f
                    && this.targetPos.SquareDistanceTo(
                        this.targetEntity.ServerPos.X,
                        this.targetEntity.ServerPos.Y,
                        this.targetEntity.ServerPos.Z
                    ) >= 3 * 3) {

                        this.targetPos.Set(
                            this.targetEntity.ServerPos.X + this.targetEntity.ServerPos.Motion.X * 10,
                            this.targetEntity.ServerPos.Y,
                            this.targetEntity.ServerPos.Z + this.targetEntity.ServerPos.Motion.Z * 10
                        ); // ..

                        this.pathTraverser.NavigateTo(
                            this.targetPos,
                            this.moveSpeed,
                            this.MinDistanceToTarget,
                            this.OnGoalReached,
                            this.OnStuck,
                            false,
                            2000,
                            1
                        ); // ..

                        this.lastPathUpdateSeconds = 0;

                    } // if ..


                    if (this.attackPattern == EnumAttackPattern.DirectAttack || this.attackPattern == EnumAttackPattern.BesiegeTarget) {
                        this.pathTraverser.CurrentTarget.X = this.targetEntity.ServerPos.X;
                        this.pathTraverser.CurrentTarget.Y = this.targetEntity.ServerPos.Y;
                        this.pathTraverser.CurrentTarget.Z = this.targetEntity.ServerPos.Z;
                    } // if ..
                } // if ..


                Cuboidd targetBox = this.targetEntity.SelectionBox.ToDouble().Translate(this.targetEntity.ServerPos.X, this.targetEntity.ServerPos.Y, this.targetEntity.ServerPos.Z);
                Vec3d   pos       = this.entity.ServerPos.XYZ.Add(0, this.entity.SelectionBox.Y2 / 2, 0).Ahead(this.entity.SelectionBox.XSize / 2, 0, this.entity.ServerPos.Yaw);
                double  distance  = targetBox.ShortestDistanceFrom(pos);


                bool inCreativeMode = (this.targetEntity as EntityPlayer)?.Player?.WorldData.CurrentGameMode == EnumGameMode.Creative;

                float minDist    = this.MinDistanceToTarget;
                bool  doContinue = this.targetEntity.Alive && !this.stopNow && !inCreativeMode && this.pathTraverser.Active;

                if (this.attackPattern == EnumAttackPattern.TacticalRetreat)
                    return doContinue && this.currentFollowTime < 9 && distance < this.tacticalRetreatRange;

                else return 
                        doContinue
                        && this.currentFollowTime < this.maxFollowTime
                        && distance < this.seekingRange
                        && (distance > minDist || (this.targetEntity is EntityAgent entityAgent && entityAgent.ServerControls.TriesToMove));
            } // bool ..

            


            public override void FinishExecute(bool cancelled) {
                base.FinishExecute(cancelled);
                this.finishedMs = this.entity.World.ElapsedMilliseconds;
                this.pathTraverser.Stop();
                this.active = false;
            } // void ..


            public override bool Notify(string key, object data) {
                if (key == "seekEntity") {
                    this.targetEntity = (Entity)data;
                    this.targetPos = this.targetEntity.ServerPos.XYZ;
                    return true;
                } // if ..

                return false;
            } // bool ..


            public override void OnEntityHurt(DamageSource source, float damage) {
                base.OnEntityHurt(source, damage);

                if (this.targetEntity == source.GetCauseEntity() || !this.active) {

                    this.lastHurtByTargetTotalMs = this.entity.World.ElapsedMilliseconds;
                    float dist = this.targetEntity == null ? 0 : (float)this.targetEntity.ServerPos.DistanceTo(this.entity.ServerPos);
                    this.tacticalRetreatRange = MathF.Max(this.tacticalRetreatRange, dist + 15);

                } // if ..
            } // void ..

            private void OnStuck() => stopNow = true;

            private void OnGoalReached() {

                if (this.attackPattern == EnumAttackPattern.DirectAttack || this.attackPattern == EnumAttackPattern.BesiegeTarget) {
                    if (this.lastGoalReachedPos != null && this.lastGoalReachedPos.SquareDistanceTo(this.entity.Pos) < 0.001)
                        if (this.futilityCounters == null) this.futilityCounters = new Dictionary<long, int>();
                        else {

                            this.futilityCounters.TryGetValue(this.targetEntity.EntityId, out int futilityCounter);
                            futilityCounter++;
                            this.futilityCounters[this.targetEntity.EntityId] = futilityCounter;
                            if (futilityCounter > 19) return;

                        } // if ..

                    this.lastGoalReachedPos = new Vec3d(this.entity.Pos);
                    this.pathTraverser.Retarget();
                    return;

                } // if ..
            } // void ..


            public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false) {
                if (base.IsTargetableEntity(e, range, ignoreEntityCode)) return true;
                else return this.HireableIsTargetableEntity(this.hireable, e, this.attackedByEntity);
            } // bool ..
            

            public override bool CanSense(Entity e, double range) {

                double nowRange = this.attackedByEntity == e ? range * 2 : range;
                bool result = base.CanSense(e, nowRange) && this.LightBasedCanSense(e, nowRange);

                // Do not target entities which have a positive futility value, but slowly decrease that value so that they can eventually be retargeted
                if (result && this.futilityCounters != null && this.futilityCounters.TryGetValue(e.EntityId, out int futilityCounter) && futilityCounter > 0) {
                    futilityCounter -= 2;
                    this.futilityCounters[e.EntityId] = futilityCounter;
                    return false;
                } // if ..

                return result;

            } // bool ..
    } // class ..
} // namespace ..
