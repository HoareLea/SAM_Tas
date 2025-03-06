using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalGroup ToTPD(this DisplayElectricalSystemCollection displayElectricalSystemCollection, PlantRoom plantRoom, ElectricalGroup electricalGroup = null)
        {
            if(displayElectricalSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            ElectricalGroup result = electricalGroup;
            if(result == null)
            {
                result = plantRoom.AddElectricalGroup();
            }

            dynamic @dynamic = result;

            @dynamic.Name = displayElectricalSystemCollection.Name;
            @dynamic.Description = displayElectricalSystemCollection.Description;
            result.ElectricalGroupType = displayElectricalSystemCollection.ElectricalSystemCollectionType.ToTPD();

            FuelSource fuelSource = plantRoom.FuelSource(displayElectricalSystemCollection.GetValue<string>(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName));
            if (fuelSource != null)
            {
                @dynamic.SetFuelSource(1, fuelSource);
            }

            if(electricalGroup == null)
            {
                displayElectricalSystemCollection.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
