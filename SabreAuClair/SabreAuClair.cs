using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public class SabreAuClairModSystem : ModSystem {
        public override void Start(ICoreAPI api) {

            base.Start(api);

            api.RegisterBlockEntityBehaviorClass("RestingPoint", typeof(BlockEntityBehaviorRestingPoint));
            
            api.RegisterEntity("EntityMercenary", typeof(EntityMercenary));
            api.RegisterEntity("EntityHireableProjectile", typeof(EntityHireableProjectile));

            api.RegisterEntityBehaviorClass("hireable", typeof(EntityBehaviorHireable));
            api.RegisterEntityBehaviorClass("randomGear", typeof(EntityBehaviorRandomGear));

            api.RegisterCollectibleBehaviorClass("CommanderTool",        typeof(BehaviorCommanderTool));
            api.RegisterCollectibleBehaviorClass("MercenaryInteraction", typeof(BehaviorMercenaryInteraction));

            AiTaskRegistry.Register("hireableseekentity",      typeof(AiTaskHierableSeekEntity));
            AiTaskRegistry.Register("hireablemeleeattack",     typeof(AiTaskHierableMeleeAttack));
            AiTaskRegistry.Register("hireablerangeattack",     typeof(AiTaskHierableRangeAttack));
            AiTaskRegistry.Register("hireableseekfoodandeat",  typeof(AiTaskHireableSeekFoodAndEat));
            AiTaskRegistry.Register("hireablestayinformation", typeof(AiTaskHierableStayInFormation));
            AiTaskRegistry.Register("rest",                    typeof(AiTaskRest));

            JsonObject modConfig  = api.LoadModConfig("SabreAuClairModConfig.json");
            SabreAuClairModSystem.GlobalConstants = new SabreAuClairModSystem.ModConstants(
                GameMath.Max(modConfig?["MercenaryDailyWage"].AsInt(1)            ??    1, 1),
                GameMath.Max(modConfig?["CompanyCapacity"].AsInt(16)              ??   16, 1),
                GameMath.Clamp(modConfig?["LineFormationRowRatio"].AsFloat(0.25f) ?? 0.25f, 0, 1),
                modConfig?["CommanderToolCodes"].AsArray<string>(),
                modConfig?["UseAnyBladeAsCommanderTool"].AsBool(false) ?? false,
                modConfig?["IgnoreRuinedCommanderTools"].AsBool(true)  ?? true,
                modConfig?["IgnoreAdminCommanderTools"].AsBool(true)   ?? true
            ); // ..
        } // void ..


        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);
            foreach (Item item in api.World.Items)
                if (item != null && item.Code != null) {
                    if (item is ItemSpear || item is ItemCleaver)
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new BehaviorMercenaryInteraction(item)).ToArray();

                    else if (item?.IsCommanderTool() ?? false)
                        if (!item.CollectibleBehaviors.Any(x => x is BehaviorCommanderTool))
                            item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new BehaviorCommanderTool(item)).ToArray();
                } // foreach ..
        } // void ..


        public override bool ShouldLoad(EnumAppSide forSide) => true;

        public static ModConstants GlobalConstants;
        public readonly struct ModConstants {

            public readonly int      MercenaryDailyWage;
            public readonly int      CompanyCapacity;
            public readonly float    LineFormationRowRatio;
            public readonly string[] CommanderToolCodes;
            public readonly bool     UseAnyBladeAsCommanderTool;
            public readonly bool     IgnoreRuinedCommanderTools;
            public readonly bool     IgnoreAdminCommanderTools;

            public ModConstants(
                int      mercenaryDailyWage,
                int      companyCapacity,
                float    lineFormationRowRatio,
                string[] commanderToolCodes,
                bool     useAnyBladeAsCommanderTool,
                bool     ignoreRuinedCommanderTools,
                bool     ignoreAdminCommanderTools
            ) {
                this.MercenaryDailyWage         = mercenaryDailyWage;
                this.CompanyCapacity            = companyCapacity;
                this.LineFormationRowRatio      = lineFormationRowRatio;
                this.CommanderToolCodes         = commanderToolCodes;
                this.UseAnyBladeAsCommanderTool = useAnyBladeAsCommanderTool;
                this.IgnoreRuinedCommanderTools = ignoreRuinedCommanderTools;
                this.IgnoreAdminCommanderTools  = ignoreAdminCommanderTools;
            } // ModConstants ..
        } // struct ..
    } // class ..
} // namespace ..
