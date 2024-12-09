using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PVPanel ToTPD(this DisplaySystemPhotovoltaicPanel displaySystemPhotovoltaicPanel, PlantRoom plantRoom)
        {
            if (displaySystemPhotovoltaicPanel == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddPVPanel();
            result.Name = displaySystemPhotovoltaicPanel.Name;
            result.Description = displaySystemPhotovoltaicPanel.Description;

            displaySystemPhotovoltaicPanel.SetLocation(result as PlantComponent);

            return result as PVPanel;
        }
    }
}
