using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DesiccantWheel ToTPD(this DisplaySystemDesiccantWheel displaySystemDesiccantWheel, global::TPD.System system)
        {
            if(displaySystemDesiccantWheel == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddDesiccantWheel();
            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            displaySystemDesiccantWheel.SetLocation(result as SystemComponent);

            return result as DesiccantWheel;
        }
    }
}
