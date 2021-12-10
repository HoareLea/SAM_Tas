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

            Dictionary<WeatherDataType, List<double>> dictionary = new Dictionary<WeatherDataType, List<double>>();
            dictionary[WeatherDataType.CloudCover] = buildingData.AnnualBuildingResult<double>(tsdBuildingArray.cloudCover);
            dictionary[WeatherDataType.DiffuseSolarRadiation] = buildingData.AnnualBuildingResult<double>(tsdBuildingArray.diffuseRadiation);
            dictionary[WeatherDataType.RelativeHumidity] = buildingData.AnnualBuildingResult<double>(tsdBuildingArray.externalHumidity);
            dictionary[WeatherDataType.DryBulbTemperature] = buildingData.AnnualBuildingResult<double>(tsdBuildingArray.externalTemperature);
            dictionary[WeatherDataType.GlobalSolarRadiation] = buildingData.AnnualBuildingResult<double>(tsdBuildingArray.globalRadiation);
            dictionary[WeatherDataType.WindDirection] = buildingData.AnnualBuildingResult<double>(tsdBuildingArray.windDirection);
            dictionary[WeatherDataType.WindSpeed] = buildingData.AnnualBuildingResult<double>(tsdBuildingArray.windSpeed);

            return Create.WeatherYear(year, dictionary);
        }
    }
}