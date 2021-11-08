using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool AssignOpeningTypes(this TBD.Building building, TBD.buildingElement buildingElement, IEnumerable<TBD.dayType> dayTypes, OpeningType openingType)
        {
            if (building == null || buildingElement == null || openingType == null || dayTypes == null)
                return false;

            TBD.ApertureType apertureType_Day = null;
            string function;

            function = null;
            if(openingType.TryGetValue("SAM_ApertureFunctionDay", out function, true) && !string.IsNullOrWhiteSpace(function))
            {
                apertureType_Day = building.AddApertureType(null);
                apertureType_Day.name = buildingElement.name + "_Day";
                foreach (TBD.dayType aDayType in dayTypes)
                    apertureType_Day.SetDayType(aDayType, true);

                int sheltered;
                if(openingType.TryGetValue("SAM_ApertureShelteredDay", out sheltered, true))
                    apertureType_Day.sheltered = sheltered;
                else
                    apertureType_Day.sheltered = 0;

                string description_ApertureType;
                if (openingType.TryGetValue("SAM_ApertureDescriptionDay", out description_ApertureType, true))
                    apertureType_Day.description = description_ApertureType;

                TBD.profile profile = apertureType_Day.GetProfile();
                profile.type = TBD.ProfileTypes.ticFunctionProfile;
                profile.function = function;

                double factor;
                if (openingType.TryGetValue("SAM_ApertureOpeningProportionDay", out factor, true))
                    profile.factor = (float)factor;

                double setbackValue;
                if (openingType.TryGetValue("SAM_ApertureOpeningProportionDaySetBack", out setbackValue, true))
                    profile.setbackValue = (float)setbackValue;

                string description_Profile;
                if (openingType.TryGetValue("SAM_ApertureProfileDescriptionDay", out description_Profile, true))
                    profile.description = description_Profile;

                string scheduleValues;
                if (openingType.TryGetValue("SAM_ApertureScheduleDay", out scheduleValues, true))
                {
                    string name_Schedule = string.Format("{0}_{1}", apertureType_Day.name, "APSCHED");

                    List<int> values = scheduleValues.Ints();

                    TBD.schedule schedule = Create.Schedule(building, name_Schedule, values);
                    if (schedule != null)
                        profile.schedule = schedule;
                }
            }

            TBD.ApertureType apertureType_Night = null;
            function = null;
            if (openingType.TryGetValue("SAM_ApertureFunctionNight", out function, true) && !string.IsNullOrWhiteSpace(function))
            {
                apertureType_Day = building.AddApertureType(null);
                apertureType_Day.name = buildingElement.name + "_Night";
                foreach (TBD.dayType aDayType in dayTypes)
                    apertureType_Day.SetDayType(aDayType, true);

                int sheltered;
                if (openingType.TryGetValue("SAM_ApertureShelteredNight", out sheltered, true))
                    apertureType_Day.sheltered = sheltered;
                else
                    apertureType_Day.sheltered = 0;

                string description_ApertureType;
                if (openingType.TryGetValue("SAM_ApertureDescriptionNight", out description_ApertureType, true))
                    apertureType_Day.description = description_ApertureType;

                TBD.profile profile = apertureType_Day.GetProfile();
                profile.type = TBD.ProfileTypes.ticFunctionProfile;
                profile.function = function;

                double factor;
                if (openingType.TryGetValue("SAM_ApertureOpeningProportionNight", out factor, true))
                    profile.factor = (float)factor;

                double setbackValue;
                if (openingType.TryGetValue("SAM_ApertureOpeningProportionNightSetBack", out setbackValue, true))
                    profile.setbackValue = (float)setbackValue;

                string description_Profile;
                if (openingType.TryGetValue("SAM_ApertureProfileDescriptionNight", out description_Profile, true))
                    profile.description = description_Profile;

                string scheduleValues;
                if (openingType.TryGetValue("SAM_ApertureScheduleNight", out scheduleValues, true))
                {
                    string name_Schedule = string.Format("{0}_{1}", apertureType_Day.name, "APSCHED");

                    List<int> values = scheduleValues.Ints();

                    TBD.schedule schedule = Create.Schedule(building, name_Schedule, values);
                    if (schedule != null)
                        profile.schedule = schedule;
                }
            }

            if (apertureType_Day != null)
                buildingElement.AssignApertureType(apertureType_Day);

            if (apertureType_Night != null)
                buildingElement.AssignApertureType(apertureType_Night);

            return true;
        }
    }
}