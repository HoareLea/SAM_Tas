using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<TBD.FeatureShade> SetFeatureShades(this Building building, buildingElement buildingElement, FeatureShade featureShade)
        {
            if (building == null || buildingElement == null || featureShade == null)
            {
                return null;
            }

            buildingElement.RemoveFeatureShades();

            List<TBD.FeatureShade> result = new List<TBD.FeatureShade>();

            if (featureShade is null)
            {
                return result; 
            }

            TBD.FeatureShade featureShade_TBD = Convert.ToTBD(featureShade, building);
            if (featureShade_TBD != null)
            {
                buildingElement.AssignFeatureShade(featureShade_TBD);
                result.Add(featureShade_TBD);

                List<dayType> dayTypes = building.DayTypes();
                if (dayTypes != null)
                {
                    dayTypes.RemoveAll(x => x.name.Equals("HDD") || x.name.Equals("CDD"));
                    foreach (dayType dayType in dayTypes)
                    {
                        featureShade_TBD.SetDayType(dayType, true);
                    }
                }
            }

            return result;
        }
    }
}