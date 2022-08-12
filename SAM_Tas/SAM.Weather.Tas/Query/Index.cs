namespace SAM.Weather.Tas
{
    public static partial class Query
    {
        public static int AnnualParameterIndex(this WeatherDataType weatherDataType)
        {
            int result = -1;
            switch (weatherDataType)
            {
                case WeatherDataType.DryBulbTemperature:
                    result = 0;
                    break;

                case WeatherDataType.RelativeHumidity:
                    result = 1;
                    break;

                case WeatherDataType.GlobalSolarRadiation:
                    result = 2;
                    break;

                case WeatherDataType.DiffuseSolarRadiation:
                    result = 3;
                    break;

                case WeatherDataType.CloudCover:
                    result = 4;
                    break;

                case WeatherDataType.WindSpeed:
                    result = 5;
                    break;

                case WeatherDataType.WindDirection:
                    result = 6;
                    break;
            }


            return result;
        }
    }
}