using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalGroup ToTPD(this DisplayElectricalSystemCollection displayElectricalSystemCollection, PlantRoom plantRoom)
        {
            if(displayElectricalSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddElectricalGroup();
            result.Name = displayElectricalSystemCollection.Name;
            result.Description = displayElectricalSystemCollection.Description;
            result.ElectricalGroupType = displayElectricalSystemCollection.ElectricalSystemCollectionType.ToTPD();

            displayElectricalSystemCollection.SetLocation(result as PlantComponent);

            return result as ElectricalGroup;
        }
    }
}
