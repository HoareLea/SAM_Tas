using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<FuelSource> FuelSources(this PlantRoom plantRoom)
        {
            return FuelSources(plantRoom?.GetEnergyCentre());
        }

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

        public static List<FuelSource> FuelSources(this SystemComponent systemComponent)
        {
            if (systemComponent == null)
            {
                return null;
            }

            List<FuelSource> result = new List<FuelSource>();

            dynamic @dynamic = (dynamic)systemComponent;

            HeatingGroup heatingGroup = @dynamic.GetHeatingGroup();
            CoolingGroup coolingGroup = @dynamic.GetCoolingGroup();
            DHWGroup dHWGroup = @dynamic.GetDHWGroup();
            RefrigerantGroup refrigerantGroup = @dynamic.GetRefrigerantGroup();
            SteamGroup steamGroup = @dynamic.GetSteamGroup();
            FuelGroup fuelGroup = @dynamic.GetFuelGroup();
            ElectricalGroup electricalGroup1 = @dynamic.GetElectricalGroup1();
            ElectricalGroup electricalGroup2 = @dynamic.GetElectricalGroup2();

            return result;
        }
    }
}