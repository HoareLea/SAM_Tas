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

            Modify.Update((result as dynamic).HeatingSetpoint, displaySystemFourPipeHeatPump.HeatingSetpoint, energyCentre);
            Modify.Update((result as dynamic).CoolingSetpoint, displaySystemFourPipeHeatPump.CoolingSetpoint, energyCentre);
            Modify.Update((result as dynamic).HeatingEfficiency, displaySystemFourPipeHeatPump.HeatingEfficiency, energyCentre);
            Modify.Update((result as dynamic).CoolingEfficiency, displaySystemFourPipeHeatPump.CoolingEfficiency, energyCentre);

            Modify.Update((result as dynamic).HeatingDuty, displaySystemFourPipeHeatPump.HeatingDuty, plantRoom);
            Modify.Update((result as dynamic).CoolingDuty, displaySystemFourPipeHeatPump.CoolingDuty, plantRoom);

            @dynamic.Capacity1 = displaySystemFourPipeHeatPump.Capacity_1;
            @dynamic.DesignPressureDrop1 = displaySystemFourPipeHeatPump.DesignPressureDrop_1;
            @dynamic.Capacity2 = displaySystemFourPipeHeatPump.Capacity_2;
            @dynamic.DesignPressureDrop2 = displaySystemFourPipeHeatPump.DesignPressureDrop_2;
            @dynamic.DesignDeltaT1 = displaySystemFourPipeHeatPump.DesignTemperatureDifference_1;
            @dynamic.DesignDeltaT2 = displaySystemFourPipeHeatPump.DesignTemperatureDifference_2;

            Modify.Update((result as dynamic).MotorEfficiency, displaySystemFourPipeHeatPump.MotorEfficiency, energyCentre);
            Modify.Update((result as dynamic).AncillaryLoad, displaySystemFourPipeHeatPump.AncillaryLoad, energyCentre);
            
            @dynamic.ADFCoolMode = displaySystemFourPipeHeatPump.ADFCoolingMode;
            @dynamic.ADFHeatMode = displaySystemFourPipeHeatPump.ADFHeatingMode;


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
