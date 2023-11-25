using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;


namespace SabreAuClair {
    public interface IHireable {
        /** <summary> Reference to the player commander </summary> **/ public IPlayer Commander { get; set; }
        /** <summary> Reference to the affected entity </summary> **/  public Entity  Entity    { get; }
    } // interface ..
} // namespace ..
