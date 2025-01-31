using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SteamHumidifier ToTPD(this DisplaySystemSteamHumidifier displaySystemSteamHumidifier, global::TPD.System system)
        {
            if(displaySystemSteamHumidifier == null || system == null)
            {
                return null;
            }

            SteamHumidifier result = system.AddSteamHumidifier();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemSteamHumidifier.Name;
            @dynamic.Description = displaySystemSteamHumidifier.Description;

            result.Setpoint?.Update(displaySystemSteamHumidifier.Setpoint);
            result.WaterSupplyTemp?.Update(displaySystemSteamHumidifier.WaterSupplyTemperature);
            result.Duty?.Update(displaySystemSteamHumidifier.Duty, system);
            result.WaterTempSource = displaySystemSteamHumidifier.WaterTemperatureSource.ToTPD();

            Modify.SetSchedule((SystemComponent)result, displaySystemSteamHumidifier.ScheduleName);

            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            displaySystemSteamHumidifier.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
