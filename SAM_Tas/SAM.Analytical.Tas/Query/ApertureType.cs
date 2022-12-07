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
                case 13:
                    return Analytical.ApertureType.Window;
                case 15:
                    return Analytical.ApertureType.Window;
            }

            return Analytical.ApertureType.Undefined;
        }

        public static TBD.ApertureType ApertureType(this TBD.buildingElement buildingElement, string name)
        {
            List<TBD.ApertureType> apertureTypes = buildingElement.ApertureTypes();

            return apertureTypes?.Find(x => x.name == name);
        }

        public static TBD.ApertureType ApertureType(this TBD.Building building, string name)
        {
            List<TBD.ApertureType> apertureTypes = building.ApertureTypes();

            return apertureTypes?.Find(x => x.name == name);
        }
    }
}