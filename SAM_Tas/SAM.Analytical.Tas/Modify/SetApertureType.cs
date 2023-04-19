using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.ApertureType SetApertureType(this Building building, buildingElement buildingElement, ISingleOpeningProperties singleOpeningProperties, string name = null, int index = -1)
        {
            if(building == null || buildingElement == null || singleOpeningProperties == null)
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

            if(index != -1)
            {
                name_Temp = string.Format("{0} {1}", name_Temp, index);
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

            if (singleOpeningProperties.TryGetValue(OpeningPropertiesParameter.Description, out string description))
            {
                result.description = description;
            }

            result.dischargeCoefficient = (float)singleOpeningProperties.GetDischargeCoefficient();

            profile profile = result.GetProfile();
            profile.value = 1;
            profile.factor = (float)singleOpeningProperties.GetFactor();

            if (singleOpeningProperties.TryGetValue(OpeningPropertiesParameter.Function, out string function))
            {
                profile.type = ProfileTypes.ticFunctionProfile;
                profile.function = function;
            }

            if(singleOpeningProperties is ProfileOpeningProperties)
            {
                ProfileOpeningProperties profileOpeningProperties = (ProfileOpeningProperties)singleOpeningProperties;
                Profile profile_SAM = profileOpeningProperties.Profile;
                if(profile_SAM != null)
                {
                    profile.type = ProfileTypes.ticHourlyProfile;
                    schedule schedule = building.Schedules()?.Find(x => x.name == profile_SAM.Name);
                    if(schedule == null)
                    {
                        schedule = building.AddSchedule();
                        schedule.name = profile_SAM.Name;
                    }

                    double[] values = profile_SAM.GetDailyValues();
                    if(values != null && values.Length == 24)
                    {
                        for(int i=0; i < 24; i++)
                        {
                            schedule.values[i] = System.Convert.ToInt32(values[i]);
                        }
                    }

                    profile.schedule = schedule;
                }
            }

            List<dayType> dayTypes = building.DayTypes();
            if (dayTypes != null)
            {
                dayTypes.RemoveAll(x => x.name.Equals("HDD") || x.name.Equals("CDD"));
                foreach (dayType dayType in dayTypes)
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