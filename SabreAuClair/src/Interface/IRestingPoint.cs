using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;

public interface IRestingPoint : IPointOfInterest {

    /** <summary> Healing effectiveness bonus when resting in the area </summary> **/  float HealEffectivenessBonus { get; }
    /** <summary> Area of effect in block radius </summary> **/                        float RestAreaRadius         { get; }

   /** <summary> Indicates whether or not the resting point has too much resting entities </summary> **/ bool OverPopulated           { get; }
   /** <summary> Indicates whether or not he resting point is valid </summary> **/                       bool IsValid                 { get; }
    
    /** <summary> Adds a new resting entity </summary> **/                          void AddOccupier(Entity entity);
    /** <summary> Removes an already resting entity </summary> **/                  void RemoveOccupier(Entity entity);
    /** <summary> Indicates whether or not an entity is resting here </summary> **/ bool IsOccupied(Entity entity);

} // interface ..
