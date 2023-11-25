using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public static class ModContent {

        /// <summary>
        /// Indicates whether or not a collectible can be used to lead a formation
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsCommanderTool(this CollectibleObject self) {

            if (self.Code.Path.StartsWith("officersabre")) return true;
            else if (SabreAuClairModSystem.GlobalConstants.CommanderToolCodes?.Any(code => self.Code?.Path == code) ?? false) return true;
            else return self.Code.EndVariant() switch {
                "ruined" => !SabreAuClairModSystem.GlobalConstants.IgnoreRuinedCommanderTools,
                "admin"  => !SabreAuClairModSystem.GlobalConstants.IgnoreAdminCommanderTools,
                _        => true,
            } && SabreAuClairModSystem.GlobalConstants.UseAnyBladeAsCommanderTool && self is ItemSword;
        } // bool ..
        

        /// <summary>
        /// Indicates whether or not a hireable should target a given entity
        /// </summary>
        /// <param name="self"></param>
        /// <param name="hireable"></param>
        /// <param name="e"></param>
        /// <param name="attackedByEntity"></param>
        /// <returns></returns>
        public static bool HireableIsTargetableEntity(
            this AiTaskBaseTargetable self,
            IHireable hireable,
            Entity e,
            Entity attackedByEntity
        ) {

            if (!e.Alive)             return false;
            if (e is not EntityAgent) return false;

            if (self.entity.HerdId != 0 && self.entity.HerdId == (e as EntityAgent)?.HerdId) return false;

            if (e is IHireable targetMercenary)
                if (hireable.Commander == null) return true;
                else if (hireable.Commander.PlayerUID == targetMercenary.Commander?.PlayerUID) return false;

            if (e is EntityPlayer playerEntity) {
                if (playerEntity.Player.WorldData?.CurrentGameMode != EnumGameMode.Survival) return false;

                if (hireable.Commander == null)                              return self.entity.GlobalTier() > playerEntity.GlobalTier();
                if (hireable.Commander?.PlayerUID == playerEntity.PlayerUID) return false;
                if (hireable.Commander?.Groups is PlayerGroupMembership[] groups)
                    foreach (PlayerGroupMembership group in groups)
                        return !(playerEntity.Player.Groups?.Any(x => x.GroupUid == group.GroupUid))
                        ?? self.entity.WatchedAttributes.GetBool("commandAggro") || self.entity.GlobalTier() > playerEntity.GlobalTier();

                return self.entity.WatchedAttributes.GetBool("commandAggro") ||  self.entity.GlobalTier() > playerEntity.GlobalTier();

            } else return e == attackedByEntity;
        } // bool ..


        /// <summary>
        /// Computes a tier based on the entity agent's gears
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static int GlobalTier(this EntityAgent self) {
            if (self == null || self.GearInventory == null) return 0;

            int tier = 0;

            // Check and add protection tier for each gear type
            tier += GetProtectionTier(self.GearInventory, EnumCharacterDressType.ArmorHead);
            tier += GetProtectionTier(self.GearInventory, EnumCharacterDressType.ArmorBody);
            tier += GetProtectionTier(self.GearInventory, EnumCharacterDressType.ArmorLegs);

            return tier;
        } // int ..


        /// <summary>
        /// Computes a tier based on a given gear slot
        /// </summary>
        /// <param name="gearInventory"></param>
        /// <param name="dressType"></param>
        /// <returns></returns>
        private static int GetProtectionTier(
            IInventory gearInventory,
            EnumCharacterDressType dressType
        ) {
            try { return (gearInventory?[(int)dressType]?.Itemstack?.Collectible as ItemWearable)?.ProtectionModifiers.ProtectionTier ?? 0; }
            catch (System.NullReferenceException) { return 0; }
        } // int ..


        /// <summary>
        /// Indicates whether or not a hireable can see its target based on distance and light
        /// </summary>
        /// <param name="self"></param>
        /// <param name="e"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static bool LightBasedCanSense(
            this AiTaskBaseTargetable self,
            Entity e,
            double range
        ) => self.world
                .BlockAccessor
                .GetLightLevel(e.ServerPos.AsBlockPos, EnumLightLevelType.MaxLight) << 1 > range;

    } // class ..
} // namespace ..