using SAM.Analytical.Systems;
using System;
using System.Collections.Generic;
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

            SystemComponent systemComponent = result as SystemComponent;

            displaySystemDXCoil.SetLocation(systemComponent);

            return result as DXCoil;
        }
    }
}
