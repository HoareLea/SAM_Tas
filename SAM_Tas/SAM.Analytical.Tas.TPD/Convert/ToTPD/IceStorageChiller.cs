using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static IceStorageChiller ToTPD(this DisplaySystemIceStorageChiller displaySystemIceStorageChiller, PlantRoom plantRoom, IceStorageChiller iceStorageChiller = null)
        {
            if (displaySystemIceStorageChiller == null || plantRoom == null)
            {
                return null;
            }

            IceStorageChiller result = iceStorageChiller;
            if(result == null)
            {
                result = plantRoom.AddIceStorageChiller();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemIceStorageChiller.Name;
            @dynamic.Description = displaySystemIceStorageChiller.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemIceStorageChiller.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemIceStorageChiller.Efficiency, energyCentre);
            result.IceMakingEfficiency?.Update(displaySystemIceStorageChiller.IceMakingEfficiency, energyCentre);
            result.Duty?.Update(displaySystemIceStorageChiller.Duty, plantRoom);
            result.DesignDeltaT1 = displaySystemIceStorageChiller.DesignTemperatureDifference;
            result.Capacity1 = displaySystemIceStorageChiller.Capacity;
            result.DesignPressureDrop1 = displaySystemIceStorageChiller.DesignPressureDrop;
            result.IceCapacity?.Update(displaySystemIceStorageChiller.IceCapacity, plantRoom);
            result.InitialIceReserve = displaySystemIceStorageChiller.InitialIceReserve;
            result.CondenserFanLoad?.Update(displaySystemIceStorageChiller.CondenserFanLoad, energyCentre);
            result.MotorEfficiency?.Update(displaySystemIceStorageChiller.MotorEfficiency, energyCentre);
            result.IceMeltChillerFraction = displaySystemIceStorageChiller.IceMeltChillerFraction;
            result.AncillaryLoad?.Update(displaySystemIceStorageChiller.AncillaryLoad, energyCentre);
            result.LossesInSizing = displaySystemIceStorageChiller.LossesInSizing.ToTPD();

            @dynamic.IsWaterSource = false;

            Modify.SetSchedule((PlantComponent)result, displaySystemIceStorageChiller.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemIceStorageChiller.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemIceStorageChiller.GetValue<string>(Core.Systems.SystemObjectParameter.FanEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemIceStorageChiller.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(3, fuelSource);
            }

            if(iceStorageChiller == null)
            {
                displaySystemIceStorageChiller.SetLocation(result as PlantComponent);
            }

            return result;
        }

        public static IceStorageChiller ToTPD(this DisplaySystemWaterSourceIceStorageChiller displaySystemWaterSourceIceStorageChiller, PlantRoom plantRoom, IceStorageChiller iceStorageChiller = null)
        {
            if (displaySystemWaterSourceIceStorageChiller == null || plantRoom == null)
            {
                return null;
            }

            IceStorageChiller result = iceStorageChiller;
            if(result == null)
            {
                result = plantRoom.AddIceStorageChiller();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceIceStorageChiller.Name;
            @dynamic.Description = displaySystemWaterSourceIceStorageChiller.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemWaterSourceIceStorageChiller.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemWaterSourceIceStorageChiller.Efficiency, energyCentre);
            result.IceMakingEfficiency?.Update(displaySystemWaterSourceIceStorageChiller.IceMakingEfficiency, energyCentre);
            result.Duty?.Update(displaySystemWaterSourceIceStorageChiller.Duty, plantRoom);
            result.DesignDeltaT1 = displaySystemWaterSourceIceStorageChiller.DesignTemperatureDifference1;
            result.Capacity1 = displaySystemWaterSourceIceStorageChiller.Capacity1;
            result.DesignPressureDrop1 = displaySystemWaterSourceIceStorageChiller.DesignPressureDrop1;
            result.DesignDeltaT2 = displaySystemWaterSourceIceStorageChiller.DesignTemperatureDifference2;
            result.Capacity2 = displaySystemWaterSourceIceStorageChiller.Capacity2;
            result.DesignPressureDrop2 = displaySystemWaterSourceIceStorageChiller.DesignPressureDrop2;
            result.IceCapacity?.Update(displaySystemWaterSourceIceStorageChiller.IceCapacity, plantRoom);
            result.InitialIceReserve = displaySystemWaterSourceIceStorageChiller.InitialIceReserve;
            result.CondenserFanLoad?.Update(displaySystemWaterSourceIceStorageChiller.CondenserFanLoad, energyCentre);
            result.MotorEfficiency?.Update(displaySystemWaterSourceIceStorageChiller.MotorEfficiency, energyCentre);
            result.IceMeltChillerFraction = displaySystemWaterSourceIceStorageChiller.IceMeltChillerFraction;
            result.AncillaryLoad?.Update(displaySystemWaterSourceIceStorageChiller.AncillaryLoad, energyCentre);
            result.LossesInSizing = displaySystemWaterSourceIceStorageChiller.LossesInSizing.ToTPD();

            @dynamic.IsWaterSource = true;

            Modify.SetSchedule((PlantComponent)result, displaySystemWaterSourceIceStorageChiller.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemWaterSourceIceStorageChiller.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemWaterSourceIceStorageChiller.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(3, fuelSource);
            }

            if(iceStorageChiller == null)
            {
                displaySystemWaterSourceIceStorageChiller.SetLocation(result as PlantComponent);
            }

            return result;
        }

        
    }
}

