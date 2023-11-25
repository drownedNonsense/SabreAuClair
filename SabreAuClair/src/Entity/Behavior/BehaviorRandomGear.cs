using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;


namespace SabreAuClair {
    public class EntityBehaviorRandomGear : EntityBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            public override string PropertyName() => "randomGear";

            /** <summary> Reference to the affected entity </summary> **/ protected EntityAgent entityAgent;

            /** <summary> Reference to the head armor slot </summary> **/ public ItemSlot ArmorHeadSlot => this.entityAgent.GearInventory[(int)EnumCharacterDressType.ArmorHead];
            /** <summary> Reference to the body armor slot </summary> **/ public ItemSlot ArmorBodySlot => this.entityAgent.GearInventory[(int)EnumCharacterDressType.ArmorBody];
            /** <summary> Reference to the legs armor slot </summary> **/ public ItemSlot ArmorLegsSlot => this.entityAgent.GearInventory[(int)EnumCharacterDressType.ArmorLegs];


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public EntityBehaviorRandomGear(Entity entity) : base(entity) {}

            public override void Initialize(EntityProperties properties, JsonObject attributes) {

                base.Initialize(properties, attributes);
                this.entityAgent = this.entity as EntityAgent;

            } // void ..


            public override void OnEntitySpawn() {
                base.OnEntitySpawn();
                this.InitializeRandomGear(this.entity.Properties.Attributes);
            } // void ..


            /// <summary>
            /// Intitializes the entity's randomized armor gear and equipment
            /// </summary>
            /// <param name="attributes"></param>
            public virtual void InitializeRandomGear(JsonObject attributes) {

                JsonObject randArmor = attributes["selectFromRandomArmor"];
                if (randArmor.Exists) {

                    JsonObject[] randArmorArray = randArmor.AsArray();
                    JsonObject   armor          = randArmorArray[this.entity.World.Rand.Next(randArmorArray.Length)];

                    if (armor["armorHead"].AsString() is string armorHead)
                        if (this.entity.World.GetItem(new AssetLocation(armorHead)) is Item item)
                            this.ArmorHeadSlot.Itemstack = new ItemStack(item);
                        else this.ArmorHeadSlot.Itemstack = null;

                    if (armor["armorBody"].AsString() is string armorBody)
                        if (this.entity.World.GetItem(new AssetLocation(armorBody)) is Item item)
                            this.ArmorBodySlot.Itemstack = new ItemStack(item);
                        else this.ArmorBodySlot.Itemstack = null;

                    if (armor["armorLegs"].AsString() is string armorLegs)
                        if (this.entity.World.GetItem(new AssetLocation(armorLegs)) is Item item)
                            this.ArmorLegsSlot.Itemstack = new ItemStack(item);
                        else this.ArmorLegsSlot.Itemstack = null;

                } // if ..


                JsonObject randWeapons = attributes["selectFromRandomWeapons"];
                if (randWeapons.Exists) {

                    string[] randWeaponsArray = randWeapons.AsArray<string>();
                    if (this.entity.World.GetItem(new AssetLocation(randWeaponsArray[this.entity.World.Rand.Next(randWeaponsArray.Length)])) is Item item)
                        this.entityAgent.ActiveHandItemSlot.Itemstack = new ItemStack(item);

                } // if ..
            } // void ..
    } // class ..
} // namespace ..