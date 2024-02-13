using SAM.Core.Tas;

namespace SAM.Weather.Tas
{
    public static partial class Convert
    {
        public static WeatherData ToSAM_WeatherData(string path_TSD, int year = 2018)
        {
            if (string.IsNullOrWhiteSpace(path_TSD) || !System.IO.File.Exists(path_TSD))
            {
                return null;
            }

            WeatherData result = null;

            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
            {
                result = ToSAM_WeatherData(sAMTSDDocument, year);
                sAMTSDDocument.Close();
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
            weatherYear_SAM.Update(weatherYear);

            WeatherData result = new WeatherData(weatherYear.name, weatherYear.description, weatherYear.latitude, weatherYear.longitude, weatherYear.altitude);
            result.SetValue(WeatherDataParameter.TimeZone, Core.Query.Description(Core.Query.UTC(weatherYear.timeZone)));

            result.Add(weatherYear_SAM);
            return result;
        }

        public static WeatherData ToSAM_WeatherData(this TWD.WeatherYear weatherYear)
        {
            if(weatherYear == null)
            {
                return null;
            }

            WeatherData result = new WeatherData(weatherYear.name, weatherYear.description, weatherYear.latitude, weatherYear.longitude, weatherYear.altitude);
            result.SetValue(WeatherDataParameter.TimeZone, (double)weatherYear.timeZone);

            GroundTemperature groundTemperature = new GroundTemperature(
                double.NaN, 
                double.NaN,
                double.NaN,
                double.NaN,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature,
                weatherYear.groundTemperature);

            Core.SAMCollection<GroundTemperature> groundTemperatures = new Core.SAMCollection<GroundTemperature>() {groundTemperature };
            result.SetValue(WeatherDataParameter.GroundTemperatures, groundTemperatures);

            WeatherYear weatherYear_Temp = weatherYear.ToSAM();
            if(weatherYear_Temp != null)
            {
                result.Add(weatherYear_Temp);
            }

            return result;
        }
    }
}