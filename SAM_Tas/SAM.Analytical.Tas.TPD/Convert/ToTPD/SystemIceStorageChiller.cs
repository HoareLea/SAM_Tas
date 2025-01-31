using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static IceStorageChiller ToTPD(this DisplaySystemIceStorageChiller displaySystemIceStorageChiller, PlantRoom plantRoom)
        {
            if (displaySystemIceStorageChiller == null || plantRoom == null)
            {
                return null;
            }

            IceStorageChiller result = plantRoom.AddIceStorageChiller();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemIceStorageChiller.Name;
            @dynamic.Description = displaySystemIceStorageChiller.Description;

            result.Setpoint?.Update(displaySystemIceStorageChiller.Setpoint);
            result.Efficiency?.Update(displaySystemIceStorageChiller.Efficiency);
            result.IceMakingEfficiency?.Update(displaySystemIceStorageChiller.IceMakingEfficiency);
            result.Duty?.Update(displaySystemIceStorageChiller.Duty, plantRoom);
            result.DesignDeltaT1 = displaySystemIceStorageChiller.DesignTemperatureDifference;
            result.Capacity1 = displaySystemIceStorageChiller.Capacity;
            result.DesignPressureDrop1 = displaySystemIceStorageChiller.DesignPressureDrop;
            result.IceCapacity?.Update(displaySystemIceStorageChiller.IceCapacity, plantRoom);
            result.InitialIceReserve = displaySystemIceStorageChiller.InitialIceReserve;
            result.CondenserFanLoad?.Update(displaySystemIceStorageChiller.CondenserFanLoad);
            result.MotorEfficiency?.Update(displaySystemIceStorageChiller.MotorEfficiency);
            result.IceMeltChillerFraction = displaySystemIceStorageChiller.IceMeltChillerFraction;
            result.AncillaryLoad?.Update(displaySystemIceStorageChiller.AncillaryLoad);
            result.LossesInSizing = displaySystemIceStorageChiller.LossesInSizing.ToTPD();

            @dynamic.IsWaterSource = false;

            Modify.SetSchedule((PlantComponent)result, displaySystemIceStorageChiller.ScheduleName);

            displaySystemIceStorageChiller.SetLocation(result as PlantComponent);

            return result;
        }

        public static IceStorageChiller ToTPD(this DisplaySystemWaterSourceIceStorageChiller displaySystemWaterSourceIceStorageChiller, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceIceStorageChiller == null || plantRoom == null)
            {
                return null;
            }

            IceStorageChiller result = plantRoom.AddIceStorageChiller();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceIceStorageChiller.Name;
            @dynamic.Description = displaySystemWaterSourceIceStorageChiller.Description;

            result.Setpoint?.Update(displaySystemWaterSourceIceStorageChiller.Setpoint);
            result.Efficiency?.Update(displaySystemWaterSourceIceStorageChiller.Efficiency);
            result.IceMakingEfficiency?.Update(displaySystemWaterSourceIceStorageChiller.IceMakingEfficiency);
            result.Duty?.Update(displaySystemWaterSourceIceStorageChiller.Duty, plantRoom);
            result.DesignDeltaT1 = displaySystemWaterSourceIceStorageChiller.DesignTemperatureDifference1;
            result.Capacity1 = displaySystemWaterSourceIceStorageChiller.Capacity1;
            result.DesignPressureDrop1 = displaySystemWaterSourceIceStorageChiller.DesignPressureDrop1;
            result.DesignDeltaT2 = displaySystemWaterSourceIceStorageChiller.DesignTemperatureDifference2;
            result.Capacity2 = displaySystemWaterSourceIceStorageChiller.Capacity2;
            result.DesignPressureDrop2 = displaySystemWaterSourceIceStorageChiller.DesignPressureDrop2;
            result.IceCapacity?.Update(displaySystemWaterSourceIceStorageChiller.IceCapacity, plantRoom);
            result.InitialIceReserve = displaySystemWaterSourceIceStorageChiller.InitialIceReserve;
            result.CondenserFanLoad?.Update(displaySystemWaterSourceIceStorageChiller.CondenserFanLoad);
            result.MotorEfficiency?.Update(displaySystemWaterSourceIceStorageChiller.MotorEfficiency);
            result.IceMeltChillerFraction = displaySystemWaterSourceIceStorageChiller.IceMeltChillerFraction;
            result.AncillaryLoad?.Update(displaySystemWaterSourceIceStorageChiller.AncillaryLoad);
            result.LossesInSizing = displaySystemWaterSourceIceStorageChiller.LossesInSizing.ToTPD();

            @dynamic.IsWaterSource = true;

            Modify.SetSchedule((PlantComponent)result, displaySystemWaterSourceIceStorageChiller.ScheduleName);

            displaySystemWaterSourceIceStorageChiller.SetLocation(result as PlantComponent);

            return result;
        }

        
    }
}

