using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PipeLossComponent ToTPD(this DisplaySystemPipeLossComponent displaySystemPipeLossComponent, PlantRoom plantRoom)
        {
            if (displaySystemPipeLossComponent == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddPipeLossComponent();
            result.Name = displaySystemPipeLossComponent.Name;
            result.Description = displaySystemPipeLossComponent.Description;

            displaySystemPipeLossComponent.SetLocation(result as PlantComponent);

            return result as PipeLossComponent;
        }
    }
}
