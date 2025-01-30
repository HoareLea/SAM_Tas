using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static LoadComponent ToTPD(this DisplaySystemLoadComponent displaySystemAirJunction, global::TPD.System system)
        {
            if (displaySystemAirJunction == null || system == null)
            {
                return null;
            }

            LoadComponent result = system.AddLoadComponent();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAirJunction.Name;
            @dynamic.Description = displaySystemAirJunction.Description;

            displaySystemAirJunction.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
