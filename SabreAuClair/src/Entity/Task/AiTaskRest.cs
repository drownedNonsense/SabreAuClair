using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public class AiTaskRest : AiTaskBase {
        
        //=======================
        // D E F I N I T I O N S
        //=======================

            protected POIRegistry poiRegistry;
            protected IRestingPoint targetPoi;

            protected float  restHours;
            protected double restEndHour;
            protected DayTimeFrame[] duringDayTimeFrames;

            protected bool nowStuck;
            protected bool restAnimStarted;

            protected float moveSpeed;
            protected float seekingRange;
            protected long lastPOISearchTotalMs;

            protected AnimationMetaData restAnimMeta;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public AiTaskRest(EntityAgent entity) : base(entity) {

                this.poiRegistry = entity.Api.ModLoader.GetModSystem<POIRegistry>();
                entity.WatchedAttributes.SetBool("doesSit", true);

            } // ..


            public override void LoadConfig(
                JsonObject taskConfig,
                JsonObject aiConfig
            ) {

                base.LoadConfig(taskConfig, aiConfig);

                this.moveSpeed           = taskConfig["movespeed"].AsFloat(0.02f);
                this.restHours           = taskConfig["restHours"].AsFloat(4f);
                this.seekingRange        = taskConfig["seekingRange"].AsFloat(16);
                this.duringDayTimeFrames = taskConfig["duringDayTimeFrames"].AsObject<DayTimeFrame[]>(null);

                if (taskConfig["restAnimation"].Exists)
                    this.restAnimMeta = new AnimationMetaData() {
                        Code           = taskConfig["restAnimation"].AsString()?.ToLowerInvariant(),
                        Animation      = taskConfig["restAnimation"].AsString()?.ToLowerInvariant(),
                        AnimationSpeed = taskConfig["restAnimationSpeed"].AsFloat(1f)
                    }.Init();
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            public override bool ShouldExecute() {

                if (this.cooldownUntilTotalHours > this.entity.World.Calendar.TotalHours)                                   return false;
                if (this.lastPOISearchTotalMs + 15000 > this.entity.World.ElapsedMilliseconds)                              return false;
                if (this.whenInEmotionState    != null && this.bhEmo?.IsInEmotionState(this.whenInEmotionState)   != true)  return false;
                if (this.whenNotInEmotionState != null && this.bhEmo?.IsInEmotionState(this.whenNotInEmotionState) == true) return false;

                if (this.duringDayTimeFrames != null) {

                    double hourOfDay = this.entity.World.Calendar.HourOfDay / this.entity.World.Calendar.HoursPerDay * 24f + (this.entity.World.Rand.NextSingle() * 0.3f - 0.15f);
                    if (!this.duringDayTimeFrames.Any(x => x.FromHour < hourOfDay && hourOfDay < x.ToHour))
                        return false;

                } // if ..


                this.lastPOISearchTotalMs = this.entity.World.ElapsedMilliseconds;
                this.targetPoi            = this.FindPOI(this.seekingRange) as IRestingPoint;

                return targetPoi != null;

            } // bool ..


            /// <summary>
            /// Finds nearest rest POI
            /// </summary>
            /// <param name="radius"></param>
            /// <returns></returns>
            private IPointOfInterest FindPOI(float radius) =>
                this.poiRegistry.GetWeightedNearestPoi(
                    this.entity.ServerPos.XYZ, radius, (poi) => poi is IRestingPoint restingPoint
                        && restingPoint.IsValid
                        && !restingPoint.OverPopulated
                ); // ..


                public override void StartExecute() {
                    if (this.animMeta != null) {

                        this.animMeta.EaseInSpeed = 1f;
                        this.animMeta.EaseOutSpeed = 1f;
                        this.entity.AnimManager.StartAnimation(this.animMeta);

                    } // if ..

                    this.nowStuck  = false;
                    this.pathTraverser.NavigateTo_Async(
                        this.targetPoi.Position,
                        this.moveSpeed,
                        -0.09f,
                        this.OnGoalReached,
                        this.OnStuck,
                        null,
                        1000,
                        1
                    ); // ..

                    this.restAnimStarted = false;

                } // void ..


                public override bool CanContinueExecute() => this.pathTraverser.Ready;
                public override bool ContinueExecute(float dt) {

                    if (this.targetPoi.OverPopulated) {
                        this.OnBadTarget();
                        return false;
                    } // if ..
                    

                    Vec3d pos      = this.targetPoi.Position;
                    float distance = pos.HorizontalSquareDistanceTo(this.entity.ServerPos.X, this.entity.ServerPos.Z);

                    this.pathTraverser.CurrentTarget.X = pos.X;
                    this.pathTraverser.CurrentTarget.Y = pos.Y;
                    this.pathTraverser.CurrentTarget.Z = pos.Z;
                

                    if (distance <= this.targetPoi.RestAreaRadius + 0.1f) {

                        this.pathTraverser.Stop();
                        if (this.restAnimMeta is AnimationMetaData restAnimMeta && !this.restAnimStarted) {

                            if (this.animMeta is AnimationMetaData animMeta)
                                this.entity.AnimManager.StopAnimation(animMeta.Code);

                            this.entity.AnimManager.StartAnimation(restAnimMeta);                        
                            this.entity.Stats.Set("healingeffectivness", "rest", this.targetPoi.HealEffectivenessBonus, true);
                            this.targetPoi.AddOccupier(this.entity);

                            this.restAnimStarted = true;
                            this.restEndHour     = this.entity.World.Calendar.TotalHours + this.restHours;

                        } // if ..


                        if (this.entity.World.Calendar.TotalHours >= restEndHour)
                            return false;
                    } else
                        if (!this.pathTraverser.Active) {

                            float rndx = this.entity.World.Rand.Next() * 0.3f - 0.15f;
                            float rndz = this.entity.World.Rand.Next() * 0.3f - 0.15f;
                            this.pathTraverser.NavigateTo(
                                this.targetPoi.Position.AddCopy(rndx, 0, rndz),
                                this.moveSpeed,
                                -0.14f,
                                this.OnGoalReached,
                                this.OnStuck,
                                false,
                                500
                            ); // ..
                        } // if ..


                    return !this.nowStuck;
                } // bool ..


                public override void OnEntityHurt(DamageSource source, float damage) {
                    base.OnEntityHurt(source, damage);
                    this.FinishExecute(true);
                } // void ..


                public override void FinishExecute(bool cancelled) {

                    base.FinishExecute(cancelled);
                    this.pathTraverser.Stop();

                    if (this.restAnimMeta is AnimationMetaData restAnimMeta)
                        this.entity.AnimManager.StopAnimation(restAnimMeta.Code);

                    this.targetPoi?.RemoveOccupier(this.entity);
                    this.entity.Stats.Set("healingeffectivness", "rest", 0f, true);

                    this.cooldownUntilTotalHours = this.entity.World.Calendar.TotalHours + 4;

                } // void ..


                /// <summary>
                /// Calls `OnBadTarget()` when stuck
                /// </summary>
                private void OnStuck() {

                    this.nowStuck = true;
                    this.OnBadTarget();

                } // void ..


                /// <summary>
                /// Tries to find a new rest POI
                /// </summary>
                private void OnBadTarget() {

                    IRestingPoint newTarget = null;
                    if (this.entity.World.Rand.Next() > 0.4f)
                        newTarget = FindPOI(this.seekingRange * 0.5f) as IRestingPoint;

                    if (newTarget != null) {

                        this.targetPoi   = newTarget;
                        this.nowStuck    = false;
                        this.pathTraverser.NavigateTo_Async(
                            this.targetPoi.Position,
                            this.moveSpeed,
                            -0.09f,
                            this.OnGoalReached,
                            this.OnStuck,
                            null,
                            1000,
                            1
                        ); // ..
                        this.restAnimStarted = false;
                        
                    } // if ..
                } // void ..


                
                private void OnGoalReached() =>
                    this.pathTraverser.Active = true;

    } // class ..
} // namespace ..
