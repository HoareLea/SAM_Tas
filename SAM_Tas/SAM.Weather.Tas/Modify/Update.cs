namespace SAM.Weather.Tas
{
    public static partial class Modify
    {
        public static bool Update(this TBD.WeatherDay weatherDay_TBD, WeatherDay weatherDay)
        {
            if(weatherDay_TBD == null || weatherDay == null)
            {
                return false;
            }

            for(int i = 1; i <= 24; i++)
            {
                double value = double.NaN;

                if(weatherDay.TryGetValue(WeatherDataType.CloudCover, i - 1, out value))
                {
                    weatherDay_TBD.cloudCover[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.DryBulbTemperature, i - 1, out value))
                {
                    weatherDay_TBD.dryBulb[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.WindSpeed, i - 1, out value))
                {
                    weatherDay_TBD.windSpeed[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.DiffuseSolarRadiation, i - 1, out value))
                {
                    weatherDay_TBD.diffuseRadiation[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.GlobalSolarRadiation, i - 1, out value))
                {
                    weatherDay_TBD.globalRadiation[i] = System.Convert.ToSingle(value);
                }
                else
                {
                    value = weatherDay.CalculatedGlobalRadiation(i - 1);
                    weatherDay_TBD.globalRadiation[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.RelativeHumidity, i - 1, out value))
                {
                    weatherDay_TBD.humidity[i] = System.Convert.ToSingle(value);
                }


                if (weatherDay.TryGetValue(WeatherDataType.WindDirection, i - 1, out value))
                {
                    weatherDay_TBD.windDirection[i] = System.Convert.ToSingle(value);
                }
            }

            return true;

        }
    }
}