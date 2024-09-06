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

            dynamic result = system.AddExchanger();
            result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            result.Setpoint.Value = 14;
            result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            displaySystemSteamHumidifier.SetLocation(result as SystemComponent);

            return result as SteamHumidifier;
        }
    }
}
