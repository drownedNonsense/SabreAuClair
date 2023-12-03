using Vintagestory.API.Common;


namespace SabreAuClair {
    public class BehaviorMercenaryInteraction : CollectibleBehavior {

        public BehaviorMercenaryInteraction(CollectibleObject collObj) : base(collObj) {}


        public override void OnHeldInteractStart(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            bool firstEvent,
            ref EnumHandHandling handHandling,
            ref EnumHandling handling
        ) {

            if (entitySel?.Entity is EntityMercenary mercenary) {
                
                mercenary.OnInteract(byEntity, slot, blockSel?.HitPosition, EnumInteractMode.Interact);
                handHandling = EnumHandHandling.PreventDefault;
                handling     = EnumHandling.PreventDefault;

            } // if ..
        } // void ..
    } // class ..
} // namespace ..
