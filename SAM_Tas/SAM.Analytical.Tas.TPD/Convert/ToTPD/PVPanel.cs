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

            PVPanel result = plantRoom.AddPVPanel();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemPhotovoltaicPanel.Name;
            @dynamic.Description = displaySystemPhotovoltaicPanel.Description;

            result.PanelEfficiency?.Update(displaySystemPhotovoltaicPanel.PanelEfficiency);
            result.InverterSize?.Update(displaySystemPhotovoltaicPanel.InverterSize, plantRoom);
            result.Multiplicity = System.Convert.ToDouble(displaySystemPhotovoltaicPanel.Multiplicity);
            result.InverterEfficiency?.Update(displaySystemPhotovoltaicPanel.InverterEfficiency);
            result.UseZoneSurface = displaySystemPhotovoltaicPanel.UseZoneSurface.ToTPD();
            result.Area = displaySystemPhotovoltaicPanel.Area;
            result.Inclination?.Update(displaySystemPhotovoltaicPanel.Inclination);
            result.Orientation?.Update(displaySystemPhotovoltaicPanel.Orientation);
            result.Reflectance = displaySystemPhotovoltaicPanel.Reflectance;
            result.MinIrradiance = displaySystemPhotovoltaicPanel.MinIrradiance;
            result.NOCT = displaySystemPhotovoltaicPanel.NOCT;
            result.PowerTempCoeff = displaySystemPhotovoltaicPanel.PowerTemperatureCoefficient;
            result.UseSTC = displaySystemPhotovoltaicPanel.UseSTC.ToTPD();
            result.OutputAtSTC = displaySystemPhotovoltaicPanel.OutputAtSTC;
            result.DeratingFactor = displaySystemPhotovoltaicPanel.DeratingFactor;

            displaySystemPhotovoltaicPanel.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
