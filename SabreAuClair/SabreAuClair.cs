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


            SabreAuClairModSystem.GlobalConstants = api.LoadModConfig<SabreAuClairModConfig>("SabreAuClairModConfig.json");
            if (SabreAuClairModSystem.GlobalConstants == null) {

                SabreAuClairModSystem.GlobalConstants = new ();
                api.StoreModConfig(SabreAuClairModSystem.GlobalConstants, "SabreAuClairModConfig.json");
                
            } // if ..
        } // void ..


        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);
            foreach (Item item in api.World.Items)
                if (item != null && item.Code != null) {
                    if (item is ItemSpear || item is ItemCleaver)
                        if (!item.CollectibleBehaviors.Any(x => x is BehaviorMercenaryInteraction))
                            item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new BehaviorMercenaryInteraction(item)).ToArray();

                    else if (item?.IsCommanderTool() ?? false)
                        if (!item.CollectibleBehaviors.Any(x => x is BehaviorCommanderTool))
                            item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new BehaviorCommanderTool(item)).ToArray();
                } // foreach ..
        } // void ..


        public override bool ShouldLoad(EnumAppSide forSide) => true;
        public static SabreAuClairModConfig GlobalConstants { get; private set; }

    } // class ..
} // namespace ..
