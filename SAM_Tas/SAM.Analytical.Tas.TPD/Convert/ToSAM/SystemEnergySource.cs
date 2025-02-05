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

            if (fuelSource.Electrical == 1)
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
