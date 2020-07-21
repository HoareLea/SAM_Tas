using SAM.Analytical;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static ApertureType ApertureType(this int bEType)
        {
            switch(bEType)
            {
                case 14:
                    return Analytical.ApertureType.Door;
                case 12:
                    return Analytical.ApertureType.Window;
            }

            return Analytical.ApertureType.Undefined;
        }
    }
}