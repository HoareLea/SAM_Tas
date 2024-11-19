using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SolarPanel ToTPD(this DisplaySystemSolarPanel displaySystemSolarPanel, PlantRoom plantRoom)
        {
            if (displaySystemSolarPanel == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddSolarPanel();
            result.Name = displaySystemSolarPanel.Name;
            result.Description = displaySystemSolarPanel.Description;

            displaySystemSolarPanel.SetLocation(result as PlantComponent);

            return result as SolarPanel;
        }
    }
}
