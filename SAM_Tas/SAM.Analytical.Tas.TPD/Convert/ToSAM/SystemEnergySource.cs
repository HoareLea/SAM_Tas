using SAM.Core.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEnergySource ToSAM(this FuelSource fuelSource)
        {
            if(fuelSource == null)
            {
                return null;
            }

            SystemEnergySource result = null;

            dynamic @dynamic = fuelSource;

            if (@dynamic.Electrical)
            {
                result = new ElectricalEnergySource(fuelSource.Name);
            }
            else
            {
                result = new SystemEnergySource(fuelSource.Name);
            }

            return result;
        }
    }
}
