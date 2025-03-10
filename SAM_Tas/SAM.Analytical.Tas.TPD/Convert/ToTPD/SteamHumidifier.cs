using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SteamHumidifier ToTPD(this DisplaySystemSteamHumidifier displaySystemSteamHumidifier, global::TPD.System system, SteamHumidifier steamHumidifier = null)
        {
            if(displaySystemSteamHumidifier == null || system == null)
            {
                return null;
            }

            SteamHumidifier result = steamHumidifier;
            if(result == null)
            {
                result = system.AddSteamHumidifier();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemSteamHumidifier.Name;
            @dynamic.Description = displaySystemSteamHumidifier.Description;

            PlantRoom plantRoom = system.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemSteamHumidifier.Setpoint, energyCentre);
            result.WaterSupplyTemp?.Update(displaySystemSteamHumidifier.WaterSupplyTemperature, energyCentre);
            result.Duty?.Update(displaySystemSteamHumidifier.Duty, system);
            result.WaterTempSource = displaySystemSteamHumidifier.WaterTemperatureSource.ToTPD();

            Modify.SetSchedule((SystemComponent)result, displaySystemSteamHumidifier.ScheduleName);

            CollectionLink collectionLink = displaySystemSteamHumidifier.GetValue<CollectionLink>(AirSystemComponentParameter.ElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    @dynamic.SetElectricalGroup1(electricalGroup);
                }
            }

            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            if(steamHumidifier == null)
            {
                displaySystemSteamHumidifier.SetLocation(result as SystemComponent);
            }

            return result;
        }
    }
}
