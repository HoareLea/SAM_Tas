using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SprayHumidifier ToTPD(this DisplaySystemSprayHumidifier displaySystemSprayHumidifier, global::TPD.System system, SprayHumidifier sprayHumidifier = null)
        {
            if(displaySystemSprayHumidifier == null || system == null)
            {
                return null;
            }

            SprayHumidifier result = sprayHumidifier;
            if(result == null)
            {
                result = system.AddSprayHumidifier();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemSprayHumidifier.Name;
            @dynamic.Description = displaySystemSprayHumidifier.Description;

            PlantRoom plantRoom = system.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemSprayHumidifier.Setpoint, energyCentre);
            result.Effectiveness?.Update(displaySystemSprayHumidifier.Effectiveness, energyCentre);
            result.WaterFlowCapacity?.Update(displaySystemSprayHumidifier.WaterFlowCapacity, system);
            result.ElectricalLoad?.Update(displaySystemSprayHumidifier.ElectricalLoad, energyCentre);

            result.Flags = 0;
            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            CollectionLink collectionLink = displaySystemSprayHumidifier.GetValue<CollectionLink>(AirSystemComponentParameter.ElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    @dynamic.SetElectricalGroup1(electricalGroup);
                }
            }

            Modify.SetSchedule((SystemComponent)result, displaySystemSprayHumidifier.ScheduleName);

            if(sprayHumidifier == null)
            {
                displaySystemSprayHumidifier.SetLocation(result as SystemComponent);
            }

            return result;
        }

        public static SprayHumidifier ToTPD(this DisplaySystemDirectEvaporativeCooler displaySystemDirectEvaporativeCooler, global::TPD.System system, SprayHumidifier sprayHumidifier = null)
        {
            if (displaySystemDirectEvaporativeCooler == null || system == null)
            {
                return null;
            }

            SprayHumidifier result = sprayHumidifier;
            if(result == null)
            {
                result = system.AddSprayHumidifier();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemDirectEvaporativeCooler.Name;
            @dynamic.Description = displaySystemDirectEvaporativeCooler.Description;

            PlantRoom plantRoom = system.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom?.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemDirectEvaporativeCooler.Setpoint, energyCentre);
            result.Effectiveness?.Update(displaySystemDirectEvaporativeCooler.Effectiveness, energyCentre);
            result.WaterFlowCapacity?.Update(displaySystemDirectEvaporativeCooler.WaterFlowCapacity, system);
            result.ElectricalLoad?.Update(displaySystemDirectEvaporativeCooler.ElectricalLoad, energyCentre);
            result.TankVolume?.Update(displaySystemDirectEvaporativeCooler.TankVolume, system);
            result.TankHours = System.Convert.ToInt32(displaySystemDirectEvaporativeCooler.HoursBeforePurgingTank);

            result.Flags = (int)tpdSprayHumidifierFlags.tpdSprayHumidifierEvaporativeCooler;
            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            CollectionLink collectionLink = displaySystemDirectEvaporativeCooler.GetValue<CollectionLink>(AirSystemComponentParameter.ElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = plantRoom?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    @dynamic.SetElectricalGroup1(electricalGroup);
                }
            }

            Modify.SetSchedule((SystemComponent)result, displaySystemDirectEvaporativeCooler.ScheduleName);

            displaySystemDirectEvaporativeCooler.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
