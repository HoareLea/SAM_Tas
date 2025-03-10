using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PVPanel ToTPD(this DisplaySystemPhotovoltaicPanel displaySystemPhotovoltaicPanel, PlantRoom plantRoom, PVPanel pVPanel = null)
        {
            if (displaySystemPhotovoltaicPanel == null || plantRoom == null)
            {
                return null;
            }

            PVPanel result = pVPanel;
            if(result == null)
            {
                result = plantRoom.AddPVPanel();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemPhotovoltaicPanel.Name;
            @dynamic.Description = displaySystemPhotovoltaicPanel.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.PanelEfficiency?.Update(displaySystemPhotovoltaicPanel.PanelEfficiency, energyCentre);
            result.InverterSize?.Update(displaySystemPhotovoltaicPanel.InverterSize, plantRoom);
            result.Multiplicity = System.Convert.ToDouble(displaySystemPhotovoltaicPanel.Multiplicity);
            result.InverterEfficiency?.Update(displaySystemPhotovoltaicPanel.InverterEfficiency, energyCentre);
            @dynamic.UseZoneSurface = displaySystemPhotovoltaicPanel.UseZoneSurface;
            result.Area = displaySystemPhotovoltaicPanel.Area;
            result.Inclination?.Update(displaySystemPhotovoltaicPanel.Inclination, energyCentre);
            result.Orientation?.Update(displaySystemPhotovoltaicPanel.Orientation, energyCentre);
            result.Reflectance = displaySystemPhotovoltaicPanel.Reflectance;
            result.MinIrradiance = displaySystemPhotovoltaicPanel.MinIrradiance;
            result.NOCT = displaySystemPhotovoltaicPanel.NOCT;
            result.PowerTempCoeff = displaySystemPhotovoltaicPanel.PowerTemperatureCoefficient;
            @dynamic.UseSTC = displaySystemPhotovoltaicPanel.UseSTC;
            result.OutputAtSTC = displaySystemPhotovoltaicPanel.OutputAtSTC;
            result.DeratingFactor = displaySystemPhotovoltaicPanel.DeratingFactor;

            FuelSource fuelSource = plantRoom.FuelSource(displaySystemPhotovoltaicPanel.GetValue<string>(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            if(pVPanel == null)
            {
                displaySystemPhotovoltaicPanel.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
