using SAM.Analytical;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static PanelType PanelType(this int bEType)
        {
            throw new System.NotImplementedException();
            
            switch(bEType)
            {
                case 1:
                    return Analytical.PanelType.Air;
            }

            return Analytical.PanelType.Undefined;
        }
    }
}