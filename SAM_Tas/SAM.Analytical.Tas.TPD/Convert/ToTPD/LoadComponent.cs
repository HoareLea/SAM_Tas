using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static LoadComponent ToTPD(this DisplaySystemLoadComponent displaySystemLoadComponent, global::TPD.System system)
        {
            if (displaySystemLoadComponent == null || system == null)
            {
                return null;
            }

            LoadComponent result = system.AddLoadComponent();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemLoadComponent.Name;
            @dynamic.Description = displaySystemLoadComponent.Description;

            result.Load?.Update(displaySystemLoadComponent.Load);

            displaySystemLoadComponent.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
