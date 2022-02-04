using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<TBD.Profiles, Dictionary<string, double[]>> TemperatureSetPointDictionary(this TBD.Building building, params TBD.Profiles[] profiles)
        {
            List<TBD.zone> zones = building?.Zones();
            if (zones == null)
                return null;

            List<TBD.dayType> dayTypes = building.DayTypes();
            if (dayTypes == null)
                return null;

            Dictionary<TBD.Profiles, Dictionary<string, Dictionary<string, double[]>>> dictionary_DayType = new Dictionary<TBD.Profiles, Dictionary<string, Dictionary<string, double[]>>>();
            foreach (TBD.Profiles profiles_Enum in profiles)
            {
                dictionary_DayType[profiles_Enum] = new Dictionary<string, Dictionary<string, double[]>>();

                foreach (TBD.dayType dayType in dayTypes)
                {
                    Dictionary<string, double[]> dictionary_Zone = new Dictionary<string, double[]>();
                    dictionary_DayType[profiles_Enum][dayType.name] = dictionary_Zone;

                    foreach (TBD.zone zone in zones)
                    {
                        TBD.InternalCondition internalCondition = InternalCondition(zone, dayType);
                        if (internalCondition == null)
                            continue;

                        TBD.Thermostat thermostat = internalCondition.GetThermostat();
                        TBD.profile profile = thermostat.GetProfile((int)profiles_Enum);
                        double[] dailyValues = DailyValues(profile);
                        dictionary_Zone.Add(zone.GUID, dailyValues);
                    }
                }
            }

            dayTypes = building.DayTypes(0, 365);

            Dictionary<TBD.Profiles, Dictionary<string, double[]>> result = new Dictionary<TBD.Profiles, Dictionary<string, double[]>>();
            foreach (KeyValuePair<TBD.Profiles, Dictionary<string, Dictionary<string, double[]>>> keyValuePair in dictionary_DayType)
            {
                Dictionary<string, double[]> dictionary_Zone = new Dictionary<string, double[]>();

                foreach (TBD.zone zone in zones)
                {
                    bool exists = true;

                    double[] yearlyValues = new double[8760];
                    int aCount = 0;
                    for (int i = 0; i < 365; i++)
                    {
                        TBD.dayType dayType = dayTypes[i];

                        if (!keyValuePair.Value.ContainsKey(dayType.name) || !keyValuePair.Value[dayType.name].ContainsKey(zone.GUID))
                        {
                            exists = false;
                            break;
                        }

                        double[] dailyValues = keyValuePair.Value[dayType.name][zone.GUID];
                        for (int j = 0; j < 24; j++)
                        {
                            yearlyValues[aCount] = dailyValues[j];
                            aCount++;
                        }
                    }

                    if (exists)
                        dictionary_Zone.Add(zone.GUID, yearlyValues);
                }

                result[keyValuePair.Key] = dictionary_Zone;
            }

            return result;
        }
    }
}