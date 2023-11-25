using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;


namespace SabreAuClair {
    public class AiTaskHierableStayInFormation : AiTaskBase {

        //=======================
        // D E F I N I T I O N S
        //=======================

            protected IHireable hireable;

            protected CompanyRegistery companyRegistery;

            protected Vec3d targetPos;
            protected bool stopNow;
            protected float moveSpeed = 0.02f;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public AiTaskHierableStayInFormation(EntityAgent entity) : base(entity) {
                this.companyRegistery = entity.Api.ModLoader.GetModSystem<CompanyRegistery>();
            } // AiTaskHierableMoveInFormation ..

            public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
                
                base.LoadConfig(taskConfig, aiConfig);
                this.moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);

            } // void ..


            public override bool ShouldExecute() {

                if ((this.hireable = this.entity as IHireable) == null) return false;

                if (rand.NextSingle() > 0.1f && (this.whenInEmotionState == null || this.bhEmo?.IsInEmotionState(this.whenInEmotionState) != true)) return false;

                if (this.whenInEmotionState    != null && this.bhEmo?.IsInEmotionState(this.whenInEmotionState) != true)    return false;
                if (this.whenNotInEmotionState != null && this.bhEmo?.IsInEmotionState(this.whenNotInEmotionState) == true) return false;
                if (this.whenInEmotionState    == null && rand.NextSingle() > 0.5f)                                         return false;

                if (this.entity.WatchedAttributes.GetBool("commandHold")) return false;

                return this.companyRegistery.TryGetTargetPos(this.hireable, out this.targetPos);

            } // bool ..


            public float MinDistanceToTarget => MathF.Max(0.1f, this.entity.SelectionBox.XSize * 0.25f);


            public override void StartExecute() {

                base.StartExecute();
                this.stopNow = false;

            } // void ..


            public override bool ContinueExecute(float dt) {

                base.ContinueExecute(dt);

                this.stopNow |=  this.entity.WatchedAttributes.GetBool("commandHold");
                this.stopNow |= !this.entity.WatchedAttributes.GetBool("commandFormation");

                if (this.rand.NextSingle() > 0.1f)
                    this.companyRegistery.TryGetTargetPos(this.hireable, out this.targetPos);


                this.pathTraverser.NavigateTo_Async(
                    this.targetPos.Clone(),
                    this.moveSpeed,
                    this.MinDistanceToTarget,
                    () => this.stopNow = true,
                    () => this.stopNow = true,
                    () => this.stopNow = true,
                    3500,
                    2
                ); // ..

                return !this.stopNow;

            } // bool ..


            public override void FinishExecute(bool cancelled) {
                base.FinishExecute(cancelled);
                this.pathTraverser.Stop();
            } // void ..
    } // class ..
} // namespace ..
