using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SprayHumidifier ToTPD(this DisplaySystemSprayHumidifier displaySystemSprayHumidifier, global::TPD.System system)
        {
            if(displaySystemSprayHumidifier == null || system == null)
            {
                return null;
            }

            SprayHumidifier result = system.AddSprayHumidifier();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemSprayHumidifier.Name;
            @dynamic.Description = displaySystemSprayHumidifier.Description;

            result.Setpoint?.Update(displaySystemSprayHumidifier.Setpoint);
            result.Effectiveness?.Update(displaySystemSprayHumidifier.Effectiveness);
            result.WaterFlowCapacity?.Update(displaySystemSprayHumidifier.WaterFlowCapacity, system);
            result.ElectricalLoad?.Update(displaySystemSprayHumidifier.ElectricalLoad);

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

            displaySystemSprayHumidifier.SetLocation(result as SystemComponent);

            return result;
        }

        public static SprayHumidifier ToTPD(this DisplaySystemDirectEvaporativeCooler displaySystemDirectEvaporativeCooler, global::TPD.System system)
        {
            if (displaySystemDirectEvaporativeCooler == null || system == null)
            {
                return null;
            }

            SprayHumidifier result = system.AddSprayHumidifier();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemDirectEvaporativeCooler.Name;
            @dynamic.Description = displaySystemDirectEvaporativeCooler.Description;

            result.Setpoint?.Update(displaySystemDirectEvaporativeCooler.Setpoint);
            result.Effectiveness?.Update(displaySystemDirectEvaporativeCooler.Effectiveness);
            result.WaterFlowCapacity?.Update(displaySystemDirectEvaporativeCooler.WaterFlowCapacity, system);
            result.ElectricalLoad?.Update(displaySystemDirectEvaporativeCooler.ElectricalLoad);
            result.TankVolume?.Update(displaySystemDirectEvaporativeCooler.TankVolume, system);
            result.TankHours = System.Convert.ToInt32(displaySystemDirectEvaporativeCooler.HoursBeforePurgingTank);

            result.Flags = (int)tpdSprayHumidifierFlags.tpdSprayHumidifierEvaporativeCooler;
            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            CollectionLink collectionLink = displaySystemDirectEvaporativeCooler.GetValue<CollectionLink>(AirSystemComponentParameter.ElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
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
