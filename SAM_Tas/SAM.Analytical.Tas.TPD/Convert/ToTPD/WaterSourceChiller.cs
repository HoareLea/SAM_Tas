using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static WaterSourceChiller ToTPD(this DisplaySystemWaterSourceChiller displaySystemWaterSourceChiller, PlantRoom plantRoom, WaterSourceChiller waterSourceChiller = null)
        {
            if (displaySystemWaterSourceChiller == null || plantRoom == null)
            {
                return null;
            }

            WaterSourceChiller result = waterSourceChiller;
            if(result == null)
            {
                result = plantRoom.AddWaterSourceChiller();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceChiller.Name;
            @dynamic.Description = displaySystemWaterSourceChiller.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemWaterSourceChiller.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemWaterSourceChiller.Efficiency, energyCentre);
            result.Duty?.Update(displaySystemWaterSourceChiller.Duty, plantRoom);
            result.Capacity1 = displaySystemWaterSourceChiller.Capacity1;
            result.Capacity2 = displaySystemWaterSourceChiller.Capacity2;
            result.DesignPressureDrop1 = displaySystemWaterSourceChiller.DesignPressureDrop1;
            result.DesignPressureDrop2 = displaySystemWaterSourceChiller.DesignPressureDrop2;
            result.DesignDeltaT1 = displaySystemWaterSourceChiller.DesignTemperatureDifference1;
            result.DesignDeltaT2 = displaySystemWaterSourceChiller.DesignTemperatureDifference2;
            result.MotorEfficiency?.Update(displaySystemWaterSourceChiller.MotorEfficiency, energyCentre);
            result.ExchCalcType = displaySystemWaterSourceChiller.ExchangerCalculationMethod.ToTPD();
            result.ExchangerEfficiency?.Update(displaySystemWaterSourceChiller.ExchangerEfficiency, energyCentre);
            result.HeatTransSurfArea = displaySystemWaterSourceChiller.HeatTransferSurfaceArea;
            result.HeatTransCoeff = displaySystemWaterSourceChiller.HeatTransferCoefficient;
            result.ExchangerType = displaySystemWaterSourceChiller.ExchangerType.ToTPD();
            result.AncillaryLoad?.Update(displaySystemWaterSourceChiller.AncillaryLoad, energyCentre);
            result.FreeCoolingType = displaySystemWaterSourceChiller.FreeCoolingType.ToTPD();
            result.LossesInSizing = displaySystemWaterSourceChiller.LossesInSizing.ToTPD();

            @dynamic.IsDirectAbsChiller = false;

            Modify.SetSchedule((PlantComponent)result, displaySystemWaterSourceChiller.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemWaterSourceChiller.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemWaterSourceChiller.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if(waterSourceChiller == null)
            {
                displaySystemWaterSourceChiller.SetLocation(result as PlantComponent);
            }

            return result;
        }

        public static WaterSourceChiller ToTPD(this DisplaySystemWaterSourceDirectAbsorptionChiller displaySystemWaterSourceDirectAbsorptionChiller, PlantRoom plantRoom, WaterSourceChiller waterSourceChiller = null)
        {
            if (displaySystemWaterSourceDirectAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            WaterSourceChiller result = waterSourceChiller;
            if(waterSourceChiller == null)
            {
                result = plantRoom.AddWaterSourceChiller();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceDirectAbsorptionChiller.Name;
            @dynamic.Description = displaySystemWaterSourceDirectAbsorptionChiller.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemWaterSourceDirectAbsorptionChiller.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemWaterSourceDirectAbsorptionChiller.Efficiency, energyCentre);
            result.Duty?.Update(displaySystemWaterSourceDirectAbsorptionChiller.Duty, plantRoom);

            result.DesignPressureDrop1 = displaySystemWaterSourceDirectAbsorptionChiller.DesignPressureDrop1;
            result.DesignPressureDrop2 = displaySystemWaterSourceDirectAbsorptionChiller.DesignPressureDrop2;
            result.DesignDeltaT1 = displaySystemWaterSourceDirectAbsorptionChiller.DesignTemperatureDifference1;
            result.DesignDeltaT2 = displaySystemWaterSourceDirectAbsorptionChiller.DesignTemperatureDifference2;
            result.Capacity1 = displaySystemWaterSourceDirectAbsorptionChiller.Capacity1;
            result.Capacity2 = displaySystemWaterSourceDirectAbsorptionChiller.Capacity2;

            result.MotorEfficiency?.Update(displaySystemWaterSourceDirectAbsorptionChiller.MotorEfficiency, energyCentre);
            result.ExchCalcType = displaySystemWaterSourceDirectAbsorptionChiller.ExchangerCalculationMethod.ToTPD();
            result.ExchangerEfficiency?.Update(displaySystemWaterSourceDirectAbsorptionChiller.ExchangerEfficiency, energyCentre);
            result.HeatTransSurfArea = displaySystemWaterSourceDirectAbsorptionChiller.HeatTransferSurfaceArea;
            result.HeatTransCoeff = displaySystemWaterSourceDirectAbsorptionChiller.HeatTransferCoefficient;
            result.ExchangerType = displaySystemWaterSourceDirectAbsorptionChiller.ExchangerType.ToTPD();
            result.AncillaryLoad?.Update(displaySystemWaterSourceDirectAbsorptionChiller.AncillaryLoad, energyCentre);
            result.FreeCoolingType = displaySystemWaterSourceDirectAbsorptionChiller.FreeCoolingType.ToTPD();
            result.LossesInSizing = displaySystemWaterSourceDirectAbsorptionChiller.LossesInSizing.ToTPD();

            @dynamic.IsDirectAbsChiller = true;

            Modify.SetSchedule((PlantComponent)result, displaySystemWaterSourceDirectAbsorptionChiller.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemWaterSourceDirectAbsorptionChiller.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemWaterSourceDirectAbsorptionChiller.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if (waterSourceChiller == null)
            {
                displaySystemWaterSourceDirectAbsorptionChiller.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
