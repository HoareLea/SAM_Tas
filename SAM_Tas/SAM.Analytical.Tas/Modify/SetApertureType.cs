using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.ApertureType SetApertureType(this Building building, buildingElement buildingElement, IOpeningProperties openingProperties, string name = null)
        {
            if(building == null || buildingElement == null || openingProperties == null)
            {
                return null;
            }

            string name_Temp = name;
            if(name_Temp == null)
            {
                name_Temp = buildingElement.name;
            }

            if(string.IsNullOrWhiteSpace(name_Temp))
            {
                return null;
            }

            TBD.ApertureType result = building.ApertureType(name_Temp);
            if(result == null)
            {
                result = building.AddApertureType(null);
                result.name = name_Temp;
            }

            if(result == null)
            {
                return null;
            }

            if (openingProperties.TryGetValue(OpeningPropertiesParameter.Description, out string description))
            {
                result.description = description;
            }

            result.dischargeCoefficient = (float)openingProperties.GetDischargeCoefficient();

            profile profile = result.GetProfile();
            profile.value = 1;

            if (openingProperties.TryGetValue(OpeningPropertiesParameter.Function, out string function))
            {
                profile.type = TBD.ProfileTypes.ticFunctionProfile;
                profile.function = function;
            }

            List<dayType> dayTypes = building.DayTypes();
            if (dayTypes != null)
            {
                dayTypes.RemoveAll(x => x.name.Equals("HDD") || x.name.Equals("CDD"));
                foreach (TBD.dayType dayType in dayTypes)
                    result.SetDayType(dayType, true);
            }

            if(buildingElement.ApertureTypes().Find(x => x.name == result.name) == null)
            {
                buildingElement.AssignApertureType(result);
            }

            return result;
        }
    }
}