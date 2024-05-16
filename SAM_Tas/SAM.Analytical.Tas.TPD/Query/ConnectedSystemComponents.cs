using SAM.Core;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<SystemComponent> ConnectedSystemComponents(this SystemComponent systemComponent, Direction direction)
        {
            List<Duct> ducts = Ducts(systemComponent, direction);
            if(ducts == null)
            {
                return null;
            }

            List<SystemComponent> result = new List<SystemComponent>();
            foreach (Duct duct in ducts)
            {
                SystemComponent systemComponent_Temp = null;

                systemComponent_Temp = duct.GetDownstreamComponent();
                if(systemComponent_Temp != null && systemComponent_Temp != systemComponent && !result.Contains(systemComponent_Temp))
                {
                    result.Add(systemComponent_Temp);
                }

                systemComponent_Temp = duct.GetUpstreamComponent();
                if (systemComponent_Temp != null && systemComponent_Temp != systemComponent && !result.Contains(systemComponent_Temp))
                {
                    result.Add(systemComponent_Temp);
                }
            }

            return result;
        }
    }
}