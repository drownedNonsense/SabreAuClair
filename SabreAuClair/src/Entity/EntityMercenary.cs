using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace SabreAuClair {
    public class EntityMercenary : EntityHumanoid, IHireable {

        //=======================
        // D E F I N I T I O N S
        //=======================

            public IPlayer Commander { get; set; }
            public Entity  Entity => this;

            /** <summary> Mercenary's maximum walkspeed </summary> **/ public float WalkSpeed { get; private set; }

            /** <summary> Reference to the mercenary's inventory </summary> **/ private readonly InventoryBase inventory;

            public override bool StoreWithChunk => true;

            public override IInventory GearInventory => this.inventory;

            public override ItemSlot RightHandItemSlot => this.inventory[15];
            public override ItemSlot LeftHandItemSlot  => this.inventory[16];

            /** <summary> Reference to the head armor slot </summary> **/ public ItemSlot ArmorHeadSlot => this.inventory[(int)EnumCharacterDressType.ArmorHead];
            /** <summary> Reference to the body armor slot </summary> **/ public ItemSlot ArmorBodySlot => this.inventory[(int)EnumCharacterDressType.ArmorBody];
            /** <summary> Reference to the legs armor slot </summary> **/ public ItemSlot ArmorLegsSlot => this.inventory[(int)EnumCharacterDressType.ArmorLegs];


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public EntityMercenary() : base() {
                this.inventory = new InventoryGear(null, null);
            } // EntityMercenary ..


            public override void Initialize(
                EntityProperties properties,
                ICoreAPI api,
                long chunkindex3d
            ) {

                base.Initialize(properties, api, chunkindex3d);
                this.inventory.LateInitialize("gearinv-" + this.EntityId, api);
                this.WatchedAttributes.RegisterModifiedListener("harvested", this.UpdateHarvestableInv);

            } // void ..


            public override void OnEntitySpawn() {

                if (this.HerdId == 0)
                    this.HerdId = this.World.Rand.NextInt64();
                
                if (this.Commander == null)
                    this.WatchedAttributes.SetBool("commandAggro", (this.HerdId & 1) == 1);

                base.OnEntitySpawn();
                if (this.World.Side == EnumAppSide.Client)
                    (this.Properties.Client.Renderer as EntityShapeRenderer).DoRenderHeldItem = true;

            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                public override bool ReceiveDamage(
                    DamageSource damageSource,
                    float damage
                ) {

                    if (float.IsNegative(damage)) return base.ReceiveDamage(damageSource, 0);
                    if (this.Api.Side.IsClient()) return base.ReceiveDamage(damageSource, damage);

                    damage = this.ApplyShieldProtection(damageSource, damage);

                    switch (damageSource.Type) {
                        case EnumDamageType.BluntAttack:    break;
                        case EnumDamageType.PiercingAttack: break;
                        case EnumDamageType.SlashingAttack: break;
                        default: return base.ReceiveDamage(damageSource, damage);
                    } // switch ..

                    switch (damageSource.Source) {
                        case EnumDamageSource.Internal: return base.ReceiveDamage(damageSource, damage);
                        case EnumDamageSource.Suicide:  return base.ReceiveDamage(damageSource, damage);
                        default: break;
                    } // switch ..


                    IInventory inv = this.GearInventory;
                    float rand     = this.World.Rand.NextSingle();

                    ItemSlot armorSlot =
                      float.IsNegative(rand -= 0.2f) ? inv[(int)EnumCharacterDressType.ArmorHead]
                    : float.IsNegative(rand -= 0.5f) ? inv[(int)EnumCharacterDressType.ArmorBody]
                    :                                  inv[(int)EnumCharacterDressType.ArmorLegs];

                    // Apply full damage if no armor is in this slot
                    if (armorSlot.Empty
                        || armorSlot.Itemstack.Item is not ItemWearable
                        || armorSlot.Itemstack.Collectible.GetRemainingDurability(armorSlot.Itemstack) <= 0
                    ) return base.ReceiveDamage(damageSource, damage);


                    ProtectionModifiers protMods = (armorSlot.Itemstack.Item as ItemWearable).ProtectionModifiers;

                    int   weaponTier  = damageSource.DamageTier;
                    float flatDmgProt = protMods.FlatDamageReduction;
                    float percentProt = protMods.RelativeProtection;


                    for (int tier = 1; tier <= weaponTier; tier++) {

                        bool aboveTier = tier > protMods.ProtectionTier;

                        float flatLoss = aboveTier ? protMods.PerTierFlatDamageReductionLoss[1] : protMods.PerTierFlatDamageReductionLoss[0];
                        float percLoss = aboveTier ? protMods.PerTierRelativeProtectionLoss[1]  : protMods.PerTierRelativeProtectionLoss[0];

                        if (aboveTier && protMods.HighDamageTierResistant) {
                            flatLoss /= 2;
                            percLoss /= 2;
                        } // if ..


                        flatDmgProt -= flatLoss;
                        percentProt *= 1 - percLoss;
                    } // for ..


                    // Durability loss is the one before the damage reductions
                    float durabilityLoss  = 0.5f + damage * MathF.Max(0.5f, (weaponTier - protMods.ProtectionTier) * 3);
                    int durabilityLossInt = GameMath.RoundRandom(this.World.Rand, durabilityLoss);

                    // Now reduce the damage
                    damage  = MathF.Max(0, damage - flatDmgProt);
                    damage *= 1 - MathF.Max(0, percentProt);

                    armorSlot.Itemstack.Collectible.DamageItem(this.World, this, armorSlot, durabilityLossInt);

                    if (armorSlot.Empty)
                        this.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), this);

                    return base.ReceiveDamage(damageSource, damage);

                } // bool ..


                /// <summary>
                /// Handles basic shield protection
                /// </summary>
                /// <param name="damageSource"></param>
                /// <param name="damage"></param>
                /// <returns></returns>
                private float ApplyShieldProtection(
                    DamageSource damageSource,
                    float damage
                ) {

                    ItemSlot[] shieldSlots = new ItemSlot[] { this.LeftHandItemSlot, this.RightHandItemSlot };
                    foreach (ItemSlot shieldSlot in shieldSlots) {

                        JsonObject attr = shieldSlot.Itemstack?.ItemAttributes?["shield"];
                        if (attr == null || !attr.Exists) continue;

                        float damageAbsorb = attr["damageAbsorption"]["passive"].AsFloat(0);
                        float chance       = attr["protectionChance"]["passive"].AsFloat(0);

                        float dx;
                        float dy;
                        float dz;

                        if (damageSource.HitPosition is Vec3d hitPosition) {

                            dx = (float)hitPosition.X;
                            dy = (float)hitPosition.Y;
                            dz = (float)hitPosition.Z;

                        } else if (damageSource.SourceEntity is Entity sourceEntity) {

                            dx = (float)sourceEntity.SidedPos.X - (float)this.SidedPos.X;
                            dy = (float)sourceEntity.SidedPos.Y - (float)this.SidedPos.Y;
                            dz = (float)sourceEntity.SidedPos.Z - (float)this.SidedPos.Z;

                        } else if (damageSource.SourcePos is Vec3d sourcePos) {

                            dx = (float)sourcePos.X - (float)this.SidedPos.X;
                            dy = (float)sourcePos.Y - (float)this.SidedPos.Y;
                            dz = (float)sourcePos.Z - (float)this.SidedPos.Z;

                        } else break;


                        if (this.World.Rand.NextDouble() < chance) {

                            damage = MathF.Max(0, damage - damageAbsorb);

                            string location = shieldSlot.Itemstack.ItemAttributes["blockSound"].AsString("held/shieldblock");
                            this.World.PlaySoundAt(AssetLocation.Create(location, shieldSlot.Itemstack.Collectible.Code.Domain).WithPathPrefixOnce("sounds/").WithPathAppendixOnce(".ogg"), this, null, true, 32, 2f);

                            if (this.World.Side.IsServer()) {
                                
                                shieldSlot.Itemstack.Collectible.DamageItem(this.World, damageSource.SourceEntity, shieldSlot, Math.Max((int)damage, 1));
                                shieldSlot.MarkDirty();

                            } // if .;
                        } // if ..
                    } // foreach ..

                    return damage;
                    
                } // float ..


                /// <summary>
                /// Updates armor effects on mercenaries
                /// </summary>
                private void UpdateWearableStats() {

                    StatModifiers allmod = new ();
                    float walkSpeedmul   = this.Stats.GetBlended("armorWalkSpeedAffectedness");

                    foreach (ItemSlot slot in this.GearInventory) {

                        if (slot.Empty || slot.Itemstack.Item is not ItemWearable) continue;

                        StatModifiers statmod = (slot.Itemstack.Item as ItemWearable).StatModifers;

                        if (statmod == null) continue;

                        bool broken = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack) == 0;

                        allmod.healingeffectivness += broken ? MathF.Min(0, statmod.healingeffectivness)         : statmod.healingeffectivness;
                        allmod.walkSpeed           += (statmod.walkSpeed < 0) ? statmod.walkSpeed * walkSpeedmul : allmod.walkSpeed += broken ? 0 : statmod.walkSpeed;                        
                        allmod.rangedWeaponsAcc    += broken ? MathF.Min(0, statmod.rangedWeaponsAcc)            : statmod.rangedWeaponsAcc;

                    } // foreach ..


                    this.Stats
                        .Set("walkspeed", "wearablemod", allmod.walkSpeed, true)
                        .Set("healingeffectivness", "wearablemod", allmod.healingeffectivness, true)
                        .Set("rangedWeaponsAcc", "wearablemod", allmod.rangedWeaponsAcc, true);

                    this.WalkSpeed = this.Stats.GetBlended("walkspeed");

                } // void ..


                public override void OnInteract(
                    EntityAgent byEntity,
                    ItemSlot slot,
                    Vec3d hitPosition,
                    EnumInteractMode mode
                ) {

                    base.OnInteract(byEntity, slot, hitPosition, mode);
                    if (mode == EnumInteractMode.Interact
                        && byEntity.Controls.Sneak
                        && byEntity is EntityPlayer player
                    ) {

                        if (slot.Empty && this.Alive && player.PlayerUID == this.Commander?.PlayerUID) this.inventory.DropAll(this.SidedPos.XYZ);
                        else if (this.Alive && player.PlayerUID == this.Commander?.PlayerUID) {

                            ItemSlot targetSlot = slot.Itemstack?.ItemAttributes?["clothescategory"].AsString() switch {
                                "armorhead" => this.ArmorHeadSlot,
                                "armorbody" => this.ArmorBodySlot,
                                "armorlegs" => this.ArmorLegsSlot,
                                _           => byEntity.Controls.CtrlKey ? this.LeftHandItemSlot : this.RightHandItemSlot,
                            }; // ..


                            if (this.GearInventory != null)
                                if (slot.TryFlipWith(targetSlot)) {

                                    slot.MarkDirty();

                                    if (targetSlot.Itemstack?.Item is ItemWearable)
                                        this.UpdateWearableStats();

                                    if (this.World.Side == EnumAppSide.Client)
                                        (this.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();      
                                } // if ..
                        } // if ..
                    } // if ..
                } // void ..


            //---------
            // D A T A
            //---------

                /// <summary>
                /// Sends mercenary gears to the harvestable inventory while highly damaging them
                /// </summary>
                public void UpdateHarvestableInv() {
                    if (this.WatchedAttributes.GetBool("harvested")) {

                        InventoryGeneric harvestableInv = new (4, "harvestableContents-" + this.EntityId, this.Api);
                        harvestableInv[0] = this.ActiveHandItemSlot;
                        harvestableInv[1] = this.ArmorHeadSlot;
                        harvestableInv[2] = this.ArmorBodySlot;
                        harvestableInv[3] = this.ArmorLegsSlot;

                        foreach (ItemSlot slot in harvestableInv)
                            slot.Itemstack?.Collectible.DamageItem(
                                this.World,
                                this,
                                slot,
                                (int)(slot.Itemstack.Collectible.GetMaxDurability(slot.Itemstack) * (0.4f + this.World.Rand.NextSingle()))
                            ); // ..

                        harvestableInv.ToTreeAttributes(this.WatchedAttributes.GetOrAddTreeAttribute("harvestableInv"));
                        
                    } // if ..
                } // void ..


                public override void ToBytes(
                    BinaryWriter writer,
                    bool forClient
                ) {

                    TreeAttribute tree;
                    this.WatchedAttributes["gearInv"] = tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes(tree);

                    base.ToBytes(writer, forClient);

                } // void ..


                public override void FromBytes(
                    BinaryReader reader,
                    bool forClient
                ) {

                    base.FromBytes(reader, forClient);

                    if (this.WatchedAttributes["gearInv"] is TreeAttribute gear) this.inventory.FromTreeAttributes(gear);

                } // void ..
    } // class ..
} // namespace ..
