using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public class AiTaskHierableMeleeAttack : AiTaskMeleeAttack {

        //=======================
        // D E F I N I T I O N S
        //=======================

            protected IHireable hireable;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public AiTaskHierableMeleeAttack(EntityAgent entity) : base(entity) {}


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================
            
            public override bool ShouldExecute() {

                if ((this.hireable = this.entity as IHireable) == null) return false;
                if (this.entity.ActiveHandItemSlot.Itemstack?.Item is Item item) {

                    this.damage      = item.AttackPower;
                    this.damageTier  = item.ToolTier;
                    this.attackRange = item.AttackRange;
                    
                } // if ..

                return base.ShouldExecute();
                
            } // bool ..


            public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false) {
                if (base.IsTargetableEntity(e, range, ignoreEntityCode)) return true;
                else return this.HireableIsTargetableEntity(this.hireable, e, this.attackedByEntity);
            } // bool ..
    } // class ..
} // namespace ..
