using SAM.Core.Tas;

namespace SAM.Weather.Tas
{
    public static partial class Convert
    {
        public static WeatherData ToSAM_WeatherData(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            WeatherData result = null;

            string extension = System.IO.Path.GetExtension(path).ToLower().Trim();
            if (extension.EndsWith("tbd"))
            {
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path))
                {
                    result = ToSAM_WeatherData(sAMTBDDocument);
                }
            }
            else if(extension.EndsWith("tsd"))
            {
                using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path))
                {
                    result = ToSAM_WeatherData(sAMTSDDocument);
                }
            }

            return result;
        }

        public static WeatherData ToSAM_WeatherData(this SAMTSDDocument sAMTSDDocument, int year = 2018)
        {
            if (sAMTSDDocument == null)
            {
                return null;
            }

            return ToSAM_WeatherData(sAMTSDDocument.TSDDocument, year);
        }

        public static WeatherData ToSAM_WeatherData(this SAMTBDDocument sAMTBDDocument)
        {
            if (sAMTBDDocument == null)
            {
                return null;
            }

            return ToSAM_WeatherData(sAMTBDDocument.TBDDocument);
        }

        public static WeatherData ToSAM_WeatherData(this TBD.TBDDocument tBDDocument)
        {
            if (tBDDocument == null)
            {
                return null;
            }

            return ToSAM_WeatherData(tBDDocument.Building);
        }

        public static WeatherData ToSAM_WeatherData(this TSD.TSDDocument tSDDocument, int year = 2018)
        {
            if (tSDDocument == null)
            {
                return null;
            }

            return ToSAM_WeatherData(tSDDocument.SimulationData, year);
        }

        public static WeatherData ToSAM_WeatherData(this TBD.Building building)
        {
            if(building == null)
            {
                return null;
            }

            TBD.WeatherYear weatherYear_TBD = building.GetWeatherYear();
            if (weatherYear_TBD == null)
            {
                return null;
            }

            return ToSAM_WeatherData(weatherYear_TBD);
        }

        public static WeatherData ToSAM_WeatherData(this TSD.SimulationData simulationData, int year = 2018)
        {
            if (simulationData == null)
            {
                return null;
            }

            WeatherYear weatherYear = simulationData.GetBuildingData().WeatherYear(year);

            WeatherData result = new WeatherData();
            result.Add(weatherYear);

            return result;
        }

        public static WeatherData ToSAM_WeatherData(this TBD.WeatherYear weatherYear)
        {
            if (weatherYear == null)
            {
                return null;
            }

            WeatherYear weatherYear_SAM = new WeatherYear(weatherYear.year);
            weatherYear.Update(weatherYear_SAM);

            WeatherData result = new WeatherData(weatherYear.name, weatherYear.description, weatherYear.latitude, weatherYear.longitude, weatherYear.altitude);
            result.SetValue(WeatherDataParameter.TimeZone, Core.Query.Description(Core.Query.UTC(weatherYear.timeZone)));

            result.Add(weatherYear_SAM);
            return result;
        }
    }
}