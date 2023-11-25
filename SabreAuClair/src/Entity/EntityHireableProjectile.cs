using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public class EntityHireableProjectile : EntityProjectile {
        public override bool CanCollect(Entity byEntity) => false;
        public override void SetRotation() {

            // I didn't manage to get client side synchronized projectiles
            // ... so this call is a workaround to prevent weird looking projectiles
            this.Pos.Motion = this.ServerPos.Motion;
            base.SetRotation();
            
        } // void ..
    } // class ..
} // namespace ..
