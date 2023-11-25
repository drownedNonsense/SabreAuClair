using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace SabreAuClair {
    class BlockEntityBehaviorRestingPoint : BlockEntityBehavior, IRestingPoint {

        //=======================
        // D E F I N I T I O N S
        //=======================

            public string Type => "restingPoint";

            /** <summary> Reference to each currently resting entities </summary> **/ protected List<Entity> entities = new();
            /** <summary> Reference to an optional valid block code </summary> **/    protected string validBlockCode;

            public bool OverPopulated => this.entities.Count >= this.entities.Capacity;
            public bool IsValid       => this.validBlockCode is not string code || this.Block.Code.Path == code;


            public float HealEffectivenessBonus { get; protected set; }
            public float RestAreaRadius         { get; protected set; }

            /** <summary> Resting point center </summary> **/  public Vec3d Position => this.Pos.ToVec3d() + new Vec3d(0.5, 0.5, 0.5);

            public void AddOccupier(Entity entity)    => this.entities.Add(entity);
            public void RemoveOccupier(Entity entity) => this.entities.Remove(entity);
            public bool IsOccupied(Entity entity)     => this.entities.Contains(entity);


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================
            
            public BlockEntityBehaviorRestingPoint(BlockEntity blockentity) : base(blockentity) {}

            public override void Initialize(ICoreAPI api, JsonObject properties) {

                base.Initialize(api, properties);

                this.HealEffectivenessBonus = properties["healEffectivenessBonus"].AsFloat(0f);
                this.validBlockCode         = properties["validBlockCode"].AsString();
                this.RestAreaRadius         = properties["restAreaRadius"].AsFloat();
                this.entities               = new List<Entity>(properties["capacity"].AsInt(1));


                if (api.Side.IsServer())
                    api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);

            } // void ..


            public override void OnBlockRemoved() {
                base.OnBlockRemoved();

                if (this.Api.Side.IsServer())
                    this.Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);

            } // void ..


            public override void OnBlockUnloaded() {
                base.OnBlockUnloaded();

                if (this.Api.Side.IsServer())
                    this.Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);

            } // void ..
    } // class ..
} // namespace ..
