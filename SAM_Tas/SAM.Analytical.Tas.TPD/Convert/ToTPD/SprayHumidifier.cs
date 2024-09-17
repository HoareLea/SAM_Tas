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
            result.Flags = 0;
            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            displaySystemSprayHumidifier.SetLocation(result as SystemComponent);

            return result as SprayHumidifier;
        }

        public static SprayHumidifier ToTPD(this DisplaySystemDirectEvaporativeCooler displaySystemDirectEvaporativeCooler, global::TPD.System system)
        {
            if (displaySystemDirectEvaporativeCooler == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddSprayHumidifier();
            result.Flags = 1;
            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            displaySystemDirectEvaporativeCooler.SetLocation(result as SystemComponent);

            return result as SprayHumidifier;
        }
    }
}
