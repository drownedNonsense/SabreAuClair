using Vintagestory.API.Common;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public class AiTaskHireableSeekFoodAndEat : AiTaskSeekFoodAndEat {
        
        //=======================
        // D E F I N I T I O N S
        //=======================
            
            protected IHireable hireable;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public AiTaskHireableSeekFoodAndEat(EntityAgent entity) : base(entity) {}


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            public override bool ShouldExecute() {

                if ((this.hireable = this.entity as IHireable) == null) return false;
                if (this.hireable.Commander != null)                    return false;
                return base.ShouldExecute();

            } // void ..
    } // class ..
} // namespace ..