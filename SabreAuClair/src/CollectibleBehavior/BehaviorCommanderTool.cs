using Vintagestory.API.Client;
using Vintagestory.API.Common;


namespace SabreAuClair {
    public class BehaviorCommanderTool : CollectibleBehavior {
        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================
            
            public BehaviorCommanderTool(CollectibleObject collObj) : base(collObj) {}


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                public override WorldInteraction[] GetHeldInteractionHelp(
                    ItemSlot inSlot,
                    ref EnumHandling handling
                ) {
                    handling = EnumHandling.PassThrough;
                    return new WorldInteraction[] {
                        new() {
                            ActionLangCode = "blockhelp-company-lead",
                            MouseButton    = EnumMouseButton.Right,
                        }, // ..
                        new() {
                            ActionLangCode = "blockhelp-company-attack",
                            MouseButton    = EnumMouseButton.Right,
                            HotKeyCode     = "ctrl"
                        }, // ..
                    }; // ..
                } // ..


                public override void OnHeldInteractStart(
                    ItemSlot slot,
                    EntityAgent byEntity,
                    BlockSelection blockSel,
                    EntitySelection entitySel,
                    bool firstEvent,
                    ref EnumHandHandling handHandling,
                    ref EnumHandling handling
                ) {
                    if(firstEvent
                        && byEntity is EntityPlayer entityPlayer
                        && entitySel?.Entity == null
                    ) {

                        entityPlayer.Stats.Set("walkspeed", "command", -0.5f);

                        CompanyRegistery companyRegistery = entityPlayer.Api.ModLoader.GetModSystem<CompanyRegistery>();

                        companyRegistery.SetCompanyCommand(entityPlayer.Player, "commandHold", false);
                        companyRegistery.SetCompanyCommand(entityPlayer.Player, "commandAggro", byEntity.Controls.CtrlKey);

                        if(byEntity.Controls.CtrlKey) entityPlayer.AnimManager.StartAnimation("commandertoolattack");
                        else                          entityPlayer.AnimManager.StartAnimation("commandertoollead");

                        handHandling = EnumHandHandling.PreventDefault;
                        handling     = EnumHandling.PreventDefault;

                    } // if ..
                } // void ..


                public override bool OnHeldInteractStep(
                    float secondsUsed,
                    ItemSlot slot,
                    EntityAgent byEntity,
                    BlockSelection blockSel,
                    EntitySelection entitySel,
                    ref EnumHandling handling
                ) {
                    handling = EnumHandling.PreventSubsequent;
                    return true;
                } // bool ..


                public override bool OnHeldInteractCancel(
                    float secondsUsed,
                    ItemSlot slot,
                    EntityAgent byEntity,
                    BlockSelection blockSel,
                    EntitySelection entitySel,
                    EnumItemUseCancelReason cancelReason,
                    ref EnumHandling handled
                ) {
                    handled = EnumHandling.PreventDefault;
                    return true;
                } // bool ..


                public override void OnHeldInteractStop(
                    float secondsUsed,
                    ItemSlot slot,
                    EntityAgent byEntity,
                    BlockSelection blockSel,
                    EntitySelection entitySel,
                    ref EnumHandling handling
                ) {
                    handling = EnumHandling.PreventDefault;
                    if(byEntity is EntityPlayer entityPlayer) {

                        entityPlayer.Stats.Set("walkspeed", "command", 0f);

                        CompanyRegistery companyRegistery = entityPlayer.Api.ModLoader.GetModSystem<CompanyRegistery>();

                        companyRegistery.SetCompanyCommand(entityPlayer.Player, "commandHold",  true);
                        companyRegistery.SetCompanyCommand(entityPlayer.Player, "commandAggro", false);

                        entityPlayer.AnimManager.StopAnimation("commandertoollead");
                        entityPlayer.AnimManager.StopAnimation("commandertoolattack");

                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
