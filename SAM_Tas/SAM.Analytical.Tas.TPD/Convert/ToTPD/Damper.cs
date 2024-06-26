﻿using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Damper ToTPD(this DisplaySystemDamper displaySystemDamper, global::TPD.System system)
        {
            if(displaySystemDamper == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddDamper();

            displaySystemDamper.SetLocation(result as SystemComponent);

            return result as Damper;
        }
    }
}
