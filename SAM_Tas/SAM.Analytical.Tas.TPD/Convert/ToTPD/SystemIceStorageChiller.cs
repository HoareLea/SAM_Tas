using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static IceStorageChiller ToTPD(this DisplaySystemIceStorageChiller displaySystemIceStorageChiller, PlantRoom plantRoom)
        {
            if (displaySystemIceStorageChiller == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddIceStorageChiller();
            result.Name = displaySystemIceStorageChiller.Name;
            result.Description = displaySystemIceStorageChiller.Description;
            result.IsWaterSource = false;

            displaySystemIceStorageChiller.SetLocation(result as PlantComponent);

            return result as IceStorageChiller;
        }

        public static IceStorageChiller ToTPD(this DisplaySystemWaterSourceIceStorageChiller displaySystemWaterSourceIceStorageChiller, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceIceStorageChiller == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddIceStorageChiller();
            result.Name = displaySystemWaterSourceIceStorageChiller.Name;
            result.Description = displaySystemWaterSourceIceStorageChiller.Description;
            result.IsWaterSource = true;

            displaySystemWaterSourceIceStorageChiller.SetLocation(result as PlantComponent);

            return result as IceStorageChiller;
        }

        
    }
}

