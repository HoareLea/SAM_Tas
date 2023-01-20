using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.ApertureType> ApertureTypes(this TBD.buildingElement buildingElement)
        {
            if(buildingElement == null)
            {
                return null;
            }

            int index = 0;

            List<TBD.ApertureType> result = new List<TBD.ApertureType>();

            TBD.ApertureType apertureType = buildingElement.GetApertureType(index);
            while (apertureType != null)
            {
                result.Add(apertureType);
                index++;
                apertureType = buildingElement.GetApertureType(index);
            }

            return result;
        }

        public static List<TBD.ApertureType> ApertureTypes(this TBD.Building building)
        {
            if (building == null)
            {
                return null;
            }

            int index = 0;

            List<TBD.ApertureType> result = new List<TBD.ApertureType>();

            TBD.ApertureType apertureType = building.GetApertureType(index);
            while (apertureType != null)
            {
                result.Add(apertureType);
                index++;
                apertureType = building.GetApertureType(index);
            }

            return result;
        }
    }
}