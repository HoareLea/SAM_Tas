// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        /// <summary>
        /// Reads the TBD aperture types assigned to a window/door building element back into a SAM
        /// <see cref="IOpeningProperties"/> (the inverse of <c>Modify.SetApertureType</c>), so that
        /// operable-opening data survives a TBD -> SAM -> TBD round-trip. Returns null when the
        /// building element carries no aperture types.
        /// </summary>
        public static IOpeningProperties ToSAM_OpeningProperties(this TBD.buildingElement buildingElement)
        {
            if (buildingElement == null)
            {
                return null;
            }

            List<TBD.ApertureType> apertureTypes = buildingElement.ApertureTypes();
            if (apertureTypes == null || apertureTypes.Count == 0)
            {
                return null;
            }

            List<ISingleOpeningProperties> singleOpeningProperties = new List<ISingleOpeningProperties>();
            foreach (TBD.ApertureType apertureType in apertureTypes)
            {
                ISingleOpeningProperties single = ToSAM_SingleOpeningProperties(apertureType);
                if (single != null)
                {
                    singleOpeningProperties.Add(single);
                }
            }

            if (singleOpeningProperties.Count == 0)
            {
                return null;
            }

            if (singleOpeningProperties.Count == 1)
            {
                return singleOpeningProperties[0];
            }

            return new MultipleOpeningProperties(singleOpeningProperties);
        }

        private static ISingleOpeningProperties ToSAM_SingleOpeningProperties(TBD.ApertureType apertureType)
        {
            if (apertureType == null)
            {
                return null;
            }

            // dischargeCoefficient and GetProfile() are accessed via dynamic to match how
            // Modify.SetApertureType writes them (they sit on the concrete COM type, not the
            // marshalled interface).
            dynamic apertureType_Dynamic = apertureType;

            double dischargeCoefficient = 0;
            try { dischargeCoefficient = System.Convert.ToDouble(apertureType_Dynamic.dischargeCoefficient); }
            catch { }

            double factor = 1;
            string function = null;
            Profile profile_SAM = null;
            DailyAvailabilitySchedule dailyAvailabilitySchedule = null;

            try
            {
                TBD.profile profile = apertureType_Dynamic.GetProfile();
                if (profile != null)
                {
                    factor = System.Convert.ToDouble(profile.factor);

                    if (profile.type == TBD.ProfileTypes.ticFunctionProfile && !string.IsNullOrWhiteSpace(profile.function))
                    {
                        function = profile.function;
                    }

                    TBD.schedule schedule = profile.schedule;
                    int[] values_Schedule = schedule.HourlyValues();
                    if (values_Schedule != null)
                    {
                        string name_Schedule = string.IsNullOrWhiteSpace(schedule.name) ? apertureType.name : schedule.name;

                        //A binary TAS schedule is an availability schedule and is read back as one, so a
                        //re-export goes through the first-class path and reuses the very schedule it came
                        //from instead of creating a second one.
                        dailyAvailabilitySchedule = schedule.DailyAvailabilitySchedule();
                        if (dailyAvailabilitySchedule != null && string.IsNullOrWhiteSpace(dailyAvailabilitySchedule.Name))
                        {
                            dailyAvailabilitySchedule = new DailyAvailabilitySchedule(name_Schedule, dailyAvailabilitySchedule);
                        }

                        //The general-valued Profile is kept as well, and is the ONLY carrier when the TAS
                        //schedule is not binary - a user-authored general curve must survive the round trip
                        //unchanged rather than be coerced into an availability mask. Where both are present
                        //the explicit Schedule governs the re-export; see ProfileOpeningProperties.
                        List<double> values = new List<double>(24);
                        for (int i = 0; i < values_Schedule.Length; i++)
                        {
                            values.Add(System.Convert.ToDouble(values_Schedule[i]));
                        }

                        profile_SAM = new Profile(name_Schedule, values);
                    }
                }
            }
            catch { }

            ProfileOpeningProperties openingProperties = new ProfileOpeningProperties(dischargeCoefficient, profile_SAM, dailyAvailabilitySchedule)
            {
                Factor = factor
            };

            if (!string.IsNullOrWhiteSpace(apertureType.description))
            {
                openingProperties.SetValue(OpeningPropertiesParameter.Description, apertureType.description);
            }

            if (!string.IsNullOrWhiteSpace(function))
            {
                openingProperties.SetValue(OpeningPropertiesParameter.Function, function);
            }

            return openingProperties;
        }
    }
}
