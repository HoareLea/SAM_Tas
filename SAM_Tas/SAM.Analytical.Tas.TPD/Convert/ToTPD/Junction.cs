﻿using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Junction ToTPD(this DisplaySystemAirJunction displaySystemAirJunction, global::TPD.System system)
        {
            if (displaySystemAirJunction == null || system == null)
            {
                return null;
            }

            Junction result = system.AddJunction();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAirJunction.Name;
            @dynamic.Description = displaySystemAirJunction.Description;

            displaySystemAirJunction.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
