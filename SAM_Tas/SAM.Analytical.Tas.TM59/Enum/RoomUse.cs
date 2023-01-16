using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    public enum RoomUse
    {
        [Description("Undefined")] Undefined,
        [Description("Bedroom")] Bedroom = 0,
        [Description("Other")] Other = 1,
        [Description("Living Room / Kitchen")] LivingRoomOrKitchen = 2,
    }
}
