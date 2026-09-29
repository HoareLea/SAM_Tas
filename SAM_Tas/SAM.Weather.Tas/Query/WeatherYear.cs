// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System.Collections.Generic;
using TSD;

namespace SAM.Weather.Tas
{
    public static partial class Query
    {
        public static WeatherYear WeatherYear(this string path_TSD, int year = 2018)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
                return null;

            WeatherYear result = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
            {
                result = WeatherYear(sAMTSDDocument, year);
            }

            return result;
        }

        public static WeatherYear WeatherYear(this SAMTSDDocument sAMTSDDocument, int year = 2018)
        {
            if (sAMTSDDocument == null)
                return null;

            return WeatherYear(sAMTSDDocument.TSDDocument, year);
        }

        public static WeatherYear WeatherYear(this TSDDocument tSDDocument, int year = 2018)
        {
            if (tSDDocument == null)
                return null;

            return WeatherYear(tSDDocument.SimulationData, year);
        }

        public static WeatherYear WeatherYear(this SimulationData simulationData, int year = 2018)
        {
            if (simulationData == null)
                return null;

            return WeatherYear(simulationData.GetBuildingData(), year);
        }

        public static WeatherYear WeatherYear(this BuildingData buildingData, int year = 2018)
        {
            if (buildingData == null)
                return null;

            //The seven weather arrays read DAY BY DAY - all seven for day 1, then day 2 - over calendar days 1..365,
            //instead of one GetAnnualBuildingResult per array: each annual read walks the whole year, and once a
            //TSD's decoded year no longer fits TSD.exe's ~550 MB day cache every walk decodes it again (~8 s per
            //array on the x30 Part O TSD: 46.1 s -> 7.1 s measured). The daily answers joined in order are, bit for
            //bit, the annual ones - -1 padding of a part-year simulation included - and every value is converted
            //exactly as AnnualBuildingResult converts it. SAM_Tas Documentation/evidence/TSD-RESULT-READ-PERFORMANCE.md.
            Dictionary<WeatherDataType, tsdBuildingArray> tsdBuildingArrays = new Dictionary<WeatherDataType, tsdBuildingArray>()
            {
                { WeatherDataType.CloudCover, tsdBuildingArray.cloudCover },
                { WeatherDataType.DiffuseSolarRadiation, tsdBuildingArray.diffuseRadiation },
                { WeatherDataType.RelativeHumidity, tsdBuildingArray.externalHumidity },
                { WeatherDataType.DryBulbTemperature, tsdBuildingArray.externalTemperature },
                { WeatherDataType.GlobalSolarRadiation, tsdBuildingArray.globalRadiation },
                { WeatherDataType.WindDirection, tsdBuildingArray.windDirection },
                { WeatherDataType.WindSpeed, tsdBuildingArray.windSpeed },
            };

            Dictionary<WeatherDataType, List<double>> dictionary = new Dictionary<WeatherDataType, List<double>>();
            foreach (WeatherDataType weatherDataType in tsdBuildingArrays.Keys)
            {
                dictionary[weatherDataType] = new List<double>(8760);
            }

            HashSet<WeatherDataType> weatherDataTypes_Answered = new HashSet<WeatherDataType>();
            for (int day = 1; day <= 365; day++)
            {
                foreach (KeyValuePair<WeatherDataType, tsdBuildingArray> keyValuePair in tsdBuildingArrays)
                {
                    List<double> values = BuildingResultValues<double>(buildingData.GetDailyBuildingResult(day, (int)keyValuePair.Value));
                    if (values != null)
                    {
                        dictionary[keyValuePair.Key].AddRange(values);
                        weatherDataTypes_Answered.Add(keyValuePair.Key);
                    }
                }
            }

            //An array TSD never answered stays null, as the annual read left it.
            foreach (WeatherDataType weatherDataType in tsdBuildingArrays.Keys)
            {
                if (!weatherDataTypes_Answered.Contains(weatherDataType))
                {
                    dictionary[weatherDataType] = null;
                }
            }

            return Create.WeatherYear(year, dictionary);
        }
    }
}