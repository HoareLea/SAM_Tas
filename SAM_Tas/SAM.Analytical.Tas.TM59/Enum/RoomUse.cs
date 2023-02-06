using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    public enum RoomUse
    {
        [Description("Undefined")] Undefined = -1,
        [Description("Bedroom")] Bedroom = 0,
        [Description("Other")] Other = 2,
        [Description("Living Room / Kitchen")] LivingRoomOrKitchen = 1,
    }
}
