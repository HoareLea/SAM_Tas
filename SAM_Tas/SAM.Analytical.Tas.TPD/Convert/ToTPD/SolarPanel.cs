using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SolarPanel ToTPD(this DisplaySystemSolarPanel displaySystemSolarPanel, PlantRoom plantRoom, SolarPanel solarPanel = null)
        {
            if (displaySystemSolarPanel == null || plantRoom == null)
            {
                return null;
            }

            SolarPanel result = solarPanel;
            if(result == null)
            {
                result = plantRoom.AddSolarPanel();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemSolarPanel.Name;
            @dynamic.Description = displaySystemSolarPanel.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.EtaZero = displaySystemSolarPanel.EtaZero;
            result.AlphaOne = displaySystemSolarPanel.AlphaOne;
            result.AlphaTwo = displaySystemSolarPanel.AlphaTwo;
            result.Multiplicity = System.Convert.ToDouble(displaySystemSolarPanel.Multiplicity);
            result.Capacity = displaySystemSolarPanel.Capacity;
            result.DesignPressureDrop = displaySystemSolarPanel.DesignPressureDrop;
            @dynamic.NoNegativeLoad = displaySystemSolarPanel.NoNegativeLoad;
            @dynamic.UseZoneSurface = displaySystemSolarPanel.UseZoneSurface;
            result.Area = displaySystemSolarPanel.SweptArea;
            result.Inclination?.Update(displaySystemSolarPanel.Inclination, energyCentre);
            result.Orientation?.Update(displaySystemSolarPanel.Orientation, energyCentre);
            result.Reflectance = displaySystemSolarPanel.Reflectance;
            result.DesignFlowPerM2 = displaySystemSolarPanel.DesignFlowPerM2;

            if(solarPanel == null)
            {
                displaySystemSolarPanel.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
