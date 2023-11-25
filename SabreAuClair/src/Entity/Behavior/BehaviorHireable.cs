using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Collections.Generic;
using Vintagestory.API.Util;


namespace SabreAuClair {
    public class EntityBehaviorHireable : EntityBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            public override string PropertyName() => "hireable";

            /** <summary> Reference to the last ingame day the hireable will be in its company </summary> **/ protected double    hiredUntil;
            /** <summary> Reference to the entity's hireable interface </summary> **/                         protected IHireable hireable;

            private static ItemStack RustyGear;
            private ItemStack[] CommanderToolStacks;
            

        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public EntityBehaviorHireable(Entity entity) : base(entity) {}
            public override void Initialize(EntityProperties properties, JsonObject attributes) {

                base.Initialize(properties, attributes);
                this.hireable   = this.entity as IHireable;
                this.hiredUntil = this.entity.WatchedAttributes.GetDouble("hiredUntil");
                
                if (this.entity.WatchedAttributes.GetString("guardedPlayerUid") is string commanderUid)
                    this.hireable.Commander = this.entity.World.PlayerByUid(commanderUid);

                this.entity.WatchedAttributes.RegisterModifiedListener("guardedPlayerUid", this.RegisterCompanyMember);

                if (this.entity.Alive)
                    this.entity
                        .Api
                        .ModLoader
                        .GetModSystem<CompanyRegistery>()
                        .RegisterCompanyMember(this.hireable);

                if (this.entity.Api.Side == EnumAppSide.Client)
                    this.CommanderToolStacks = ObjectCacheUtil.GetOrCreate(this.entity.Api, "commanderToolStacks", delegate {

                        List<ItemStack> commanderToolStacks = new ();
                        foreach (Item item in entity.World.Items)
                            if (item != null
                                && item.Code != null
                                && (item?.IsCommanderTool() ?? false)
                            ) commanderToolStacks.Add(new ItemStack(item));


                        foreach (CollectibleObject collectible in entity.Api.World.Collectibles)
                            if (collectible.IsCommanderTool())
                                commanderToolStacks.AddRange(collectible.GetHandBookStacks(entity.Api as ICoreClientAPI));

                        return commanderToolStacks.ToArray();
                    }); // ..
            } // void ..


            public override void OnEntityDespawn(EntityDespawnData despawn) =>
                this.entity
                    .Api
                    .ModLoader
                    .GetModSystem<CompanyRegistery>()
                    .RemoveCompanyMember(this.hireable);

            public override void OnEntityDeath(DamageSource damageSourceForDeath) =>
                this.entity
                    .Api
                    .ModLoader
                    .GetModSystem<CompanyRegistery>()
                    .RemoveCompanyMember(this.hireable);


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            public override void OnGameTick(float deltaTime) {
                base.OnGameTick(deltaTime);
                if (hiredUntil < this.entity.World.Calendar.TotalDays && this.hireable.Commander != null) {

                    this.hireable.Commander = null;
                    this.entity.WatchedAttributes.RemoveAttribute("guardedPlayerUid");

                    this.entity
                        .Api
                        .ModLoader
                        .GetModSystem<CompanyRegistery>()
                        .RemoveCompanyMember(this.hireable);

                } // if ..
            } // void ..


            public override void GetInfoText(StringBuilder infotext) {

                if (!this.entity.Alive) return;

                double remainingDays = this.hiredUntil - this.entity.World.Calendar.TotalDays;
                if (this.hireable.Commander is IPlayer commander && double.IsPositive(remainingDays))
                     infotext.AppendLine(Lang.Get("Fighting in {0}'s company for {1:0.#} days", commander.PlayerName, remainingDays.ToString("#.#")));
                else infotext.AppendLine(Lang.Get("Knight-errant"));
            } // void ..


            /// <summary>
            /// Called whenever the hireable commander changes
            /// </summary>
            private void RegisterCompanyMember() =>
                this.entity
                    .Api
                    .ModLoader
                    .GetModSystem<CompanyRegistery>()
                    .RegisterCompanyMember(this.hireable);


            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------
                    
                public override WorldInteraction[] GetInteractionHelp(
                    IClientWorldAccessor world,
                    EntitySelection es,
                    IClientPlayer player,
                    ref EnumHandling handled
                ) {
                    EntityBehaviorHireable.RustyGear ??= new ItemStack(world.GetItem(new AssetLocation("game:gear-rusty")), SabreAuClairModSystem.GlobalConstants.MercenaryDailyWage);

                    return new WorldInteraction[] {
                        new() {
                            ActionLangCode    = "blockhelp-hireable-pay",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = new ItemStack[1] { EntityBehaviorHireable.RustyGear },
                            GetMatchingStacks = (wi, bs, es) => {
                                if (!this.entity.Alive) return null;
                                return (this.hireable.Commander == null
                                    && this.hireable.Commander?.PlayerUID == player.PlayerUID
                                    || (this.entity as EntityAgent)?.GlobalTier() <= player.Entity.GlobalTier()) ? wi.Itemstacks : null;
                            } // ..
                        }, new() {
                            ActionLangCode    = "blockhelp-hireable-joinformation",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = this.CommanderToolStacks,
                            GetMatchingStacks = (wi, bs, es) => this.entity.Alive
                                &&  this.hireable.Commander?.PlayerUID == player.PlayerUID
                                && !this.entity.WatchedAttributes.GetBool("commandFormation") ? wi.Itemstacks : null,
                        }, new() {
                            ActionLangCode    = "blockhelp-hireable-leaveformation",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = this.CommanderToolStacks,
                            GetMatchingStacks = (wi, bs, es) => this.entity.Alive
                                && this.hireable.Commander?.PlayerUID == player.PlayerUID
                                && this.entity.WatchedAttributes.GetBool("commandFormation") ? wi.Itemstacks : null,
                        }, new() {
                            ActionLangCode = "blockhelp-hireable-aggressive",
                            MouseButton    = EnumMouseButton.Right,
                            HotKeyCode     = "ctrl",
                            ShouldApply    = (wi, bs, es) => this.entity.Alive
                                && this.hireable.Commander?.PlayerUID == player.PlayerUID
                                && !this.entity.WatchedAttributes.GetBool("commandAggro"),
                        }, new() {
                            ActionLangCode = "blockhelp-hireable-defensive",
                            MouseButton    = EnumMouseButton.Right,
                            HotKeyCode     = "ctrl",
                            ShouldApply    = (wi, bs, es) => this.entity.Alive
                                && this.hireable.Commander?.PlayerUID == player.PlayerUID
                                && this.entity.WatchedAttributes.GetBool("commandAggro"),
                        }}; // ..
                } // WorldInteraction[] ..


                public override void OnInteract(
                    EntityAgent byEntity,
                    ItemSlot itemslot,
                    Vec3d hitPosition,
                    EnumInteractMode mode,
                    ref EnumHandling handled
                ) {

                    if (mode == EnumInteractMode.Interact
                        && this.entity.Alive
                        && !byEntity.Controls.Sneak
                        && byEntity is EntityPlayer playerEntity
                        && ((this.hireable.Commander == null && (this.entity as EntityAgent)?.GlobalTier() <= byEntity.GlobalTier())
                            || (playerEntity.PlayerUID == this.hireable.Commander?.PlayerUID))
                    ) {

                        if (itemslot.Itemstack?.Item is ItemRustyGear
                            && itemslot.Itemstack?.StackSize >= SabreAuClairModSystem.GlobalConstants.MercenaryDailyWage
                            && this.entity.Api.ModLoader.GetModSystem<CompanyRegistery>().TryAddMember(playerEntity.Player, this.hireable)
                        ) {
                        
                            itemslot.TakeOut(SabreAuClairModSystem.GlobalConstants.MercenaryDailyWage);
                            itemslot.MarkDirty();

                            this.hiredUntil = GameMath.Max(this.entity.World.Calendar.TotalDays, this.hiredUntil) + 1;
                            this.hireable.Commander = playerEntity.Player;
                            
                            this.entity.WatchedAttributes.SetString("guardedPlayerUid", playerEntity.PlayerUID);
                            this.entity.WatchedAttributes.SetDouble("hiredUntil", this.hiredUntil);
                            this.entity.World.PlaySoundAt(new AssetLocation("game:sounds/player/coin6.ogg"), this.entity);

                            this.entity.StartAnimation("hired");
                            handled = EnumHandling.PreventSubsequent;

                        } else if (this.hireable.Commander != null) {
                            if (byEntity.Controls.Sprint) {

                                this.entity.WatchedAttributes.SetBool("commandAggro", !this.entity.WatchedAttributes.GetBool("commandAggro"));
                                this.entity.StartAnimation("hired");
                                handled = EnumHandling.PreventSubsequent;

                            } else if (itemslot.Itemstack?.Collectible?.IsCommanderTool() ?? false) {
                                if (!this.entity.WatchedAttributes.GetBool("commandFormation")) {
                                    this.entity.WatchedAttributes.SetBool("commandHold", true);
                                    this.entity
                                        .Api
                                        .ModLoader
                                        .GetModSystem<CompanyRegistery>()?
                                        .AddCompanyMemberToFormation(this.hireable);

                                } else
                                    this.entity
                                        .Api
                                        .ModLoader
                                        .GetModSystem<CompanyRegistery>()?
                                        .RemoveCompanyMemberFromFormation(this.hireable);

                                this.entity.StartAnimation("hired");
                                handled = EnumHandling.PreventSubsequent;

                            } // if ..
                        } // if ..
                    } // if ..
                } // void ..
    } // class ..
} // namespace ..