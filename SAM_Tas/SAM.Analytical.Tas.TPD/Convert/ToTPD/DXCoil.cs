using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DXCoil ToTPD(this DisplaySystemDXCoil displaySystemDXCoil, global::TPD.System system)
        {
            if(displaySystemDXCoil == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddDXCoil();
            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            displaySystemDXCoil.SetLocation(result as SystemComponent);

            return result as DXCoil;
        }
    }
}
