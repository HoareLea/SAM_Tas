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

            displaySystemIceStorageChiller.SetLocation(result as PlantComponent);

            return result as IceStorageChiller;
        }
    }
}

