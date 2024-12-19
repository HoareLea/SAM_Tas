using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<FuelSource> FuelSources(this EnergyCentre energyCentre)
        {
            if (energyCentre is null)
            {
                return null;
            }

            List<FuelSource> result = new List<FuelSource>();
            for (int i = 1; i <= energyCentre.GetFuelSourceCount(); i++)
            {
                FuelSource fuelSource = energyCentre.GetFuelSource(i);
                if(fuelSource == null)
                {
                    continue;
                }

                result.Add(fuelSource);
            }

            return result;
        }

        public static List<FuelSource> FuelSources(this PlantComponent plantComponent)
        {
            if(plantComponent == null)
            {
                return null;
            }

            List<FuelSource> result = new List<FuelSource>();

            dynamic @dynamic = (dynamic)plantComponent;

            int count = @dynamic.GetFuelSourceCount();
            if (count > 0)
            {
                for (int i = 1; i <= count; i++)
                {
                    result.Add(@dynamic.GetFuelSource(i));
                }
            }

            return result;
        }
    }
}