using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FourPipeHeatPump ToTPD(this DisplaySystemFourPipeHeatPump displaySystemFourPipeHeatPump, PlantRoom plantRoom, FourPipeHeatPump fourPipeHeatPump = null)
        {
            if (displaySystemFourPipeHeatPump == null || plantRoom == null)
            {
                return null;
            }

            FourPipeHeatPump result = fourPipeHeatPump;
            if(result == null)
            {
                result = plantRoom.AddFourPipeHeatPump();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemFourPipeHeatPump.Name;
            @dynamic.Description = displaySystemFourPipeHeatPump.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.HeatingSetpoint?.Update(displaySystemFourPipeHeatPump.HeatingSetpoint, energyCentre);
            result.CoolingSetpoint?.Update(displaySystemFourPipeHeatPump.CoolingSetpoint, energyCentre);
            result.HeatingEfficiency?.Update(displaySystemFourPipeHeatPump.HeatingEfficiency, energyCentre);
            result.CoolingEfficiency?.Update(displaySystemFourPipeHeatPump?.CoolingEfficiency, energyCentre);
            result.HeatingDuty?.Update(displaySystemFourPipeHeatPump.HeatingDuty, plantRoom);
            result.CoolingDuty?.Update(displaySystemFourPipeHeatPump.CoolingDuty, plantRoom);
            result.Capacity1 = displaySystemFourPipeHeatPump.Capacity_1;
            result.DesignPressureDrop1 = displaySystemFourPipeHeatPump.DesignPressureDrop_1;
            result.Capacity2 = displaySystemFourPipeHeatPump.Capacity_2;
            result.DesignPressureDrop2 = displaySystemFourPipeHeatPump.DesignPressureDrop_2;
            result.DesignDeltaT1 = displaySystemFourPipeHeatPump.DesignTemperatureDifference_1;
            result.DesignDeltaT2 = displaySystemFourPipeHeatPump.DesignTemperatureDifference_2;
            result.MotorEfficiency?.Update(displaySystemFourPipeHeatPump.MotorEfficiency, energyCentre);
            result.AncillaryLoad?.Update(displaySystemFourPipeHeatPump.AncillaryLoad, energyCentre);
            result.ADFCoolMode = displaySystemFourPipeHeatPump.ADFCoolingMode;
            result.ADFHeatMode = displaySystemFourPipeHeatPump.ADFHeatingMode;


            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemFourPipeHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemFourPipeHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if(fourPipeHeatPump == null)
            {
                displaySystemFourPipeHeatPump.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
