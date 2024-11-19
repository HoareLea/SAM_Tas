using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static GroundSource ToTPD(this DisplaySystemVerticalBorehole displaySystemVerticalBorehole, PlantRoom plantRoom)
        {
            if (displaySystemVerticalBorehole == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddGroundSource();
            result.Name = displaySystemVerticalBorehole.Name;
            result.Description = displaySystemVerticalBorehole.Description;

            displaySystemVerticalBorehole.SetLocation(result as PlantComponent);

            return result as GroundSource;
        }
    }
}
