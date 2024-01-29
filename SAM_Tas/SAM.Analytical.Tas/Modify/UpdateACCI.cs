using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateACCI(this TBD.Building building, IEnumerable<string> spaceNames)
        {
            if (building == null)
            {
                return false;
            }

            List<TBD.InternalCondition> internalConditions = building.InternalConditions();
            if(internalConditions == null || internalConditions.Count == 0)
            {
                return false;
            }


            List<double> dryBulbTemperatures = Weather.Tas.Query.AnnualParameter<double>(building.GetWeatherYear(), Weather.WeatherDataType.DryBulbTemperature);
            if(dryBulbTemperatures == null || dryBulbTemperatures.Count != 8760)
            {
                return false;
            }

            bool result = false;
            foreach (TBD.InternalCondition internalCondition in internalConditions)
            {
                if(internalCondition.name.EndsWith(" - HDD") || internalCondition.name.EndsWith(" - CDD") || internalCondition.name.EndsWith("New Internal Condition") )
                {
                    continue;
                }

                if(spaceNames != null && spaceNames.Count() == 0)
                {
                    bool contains = false;                    
                    List<TBD.zone> zones = internalCondition.Zones();
                    if(zones != null)
                    {
                        foreach(TBD.zone zone in zones)
                        {
                            if(spaceNames.Contains(zone?.name))
                            {
                                contains = true;
                                break;
                            }
                        }
                    }

                    if(!contains)
                    {
                        continue;
                    }
                }

                TBD.profile profile_UpperLimit = internalCondition.GetThermostat().GetProfile((int)TBD.Profiles.ticUL);
                if (profile_UpperLimit == null)
                {
                    continue;
                }

                TBD.profile profile_LowerLimit = internalCondition.GetThermostat().GetProfile((int)TBD.Profiles.ticLL);
                if (profile_LowerLimit == null)
                {
                    continue;
                }

                profile_UpperLimit.type = TBD.ProfileTypes.ticYearlyProfile;
                profile_LowerLimit.type = TBD.ProfileTypes.ticYearlyProfile;

                for (int i = 1; i <= 8760; i++)
                {
                    Range<double> dryBulbTemperatureRange = Weather.Query.DryBulbTemperatureRange(dryBulbTemperatures[i - 1]);
                    if(dryBulbTemperatureRange == null)
                    {
                        continue;
                    }

                    profile_UpperLimit.yearlyValues[i] = System.Convert.ToSingle(dryBulbTemperatureRange.Max);
                    profile_LowerLimit.yearlyValues[i] = System.Convert.ToSingle(dryBulbTemperatureRange.Min);
                    result = true;
                }

            }

            return result;
        }

        public static bool UpdateACCI(this SAMTBDDocument sAMTBDDocument, IEnumerable<string> spaceNames)
        {
            if (sAMTBDDocument == null)
                return false;

            return UpdateACCI(sAMTBDDocument.TBDDocument?.Building, spaceNames);
        }

        public static bool UpdateACCI(string path_TBD, IEnumerable<string> spaceNames)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
            {
                return false;
            }

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateACCI(sAMTBDDocument, spaceNames);
                if (result)
                {
                    sAMTBDDocument.Save();
                }
            }

            return result;
        }
    }
}