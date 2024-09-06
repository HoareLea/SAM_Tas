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

            dynamic result = system.AddSprayHumidifier();
            result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            result.Setpoint.Value = 14;
            result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            displaySystemSprayHumidifier.SetLocation(result as SystemComponent);

            return result as SprayHumidifier;
        }
    }
}
