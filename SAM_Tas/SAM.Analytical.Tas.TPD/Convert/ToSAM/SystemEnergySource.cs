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

            SystemEnergySource result = new SystemEnergySource(fuelSource.Name);

            return result;
        }
    }
}
