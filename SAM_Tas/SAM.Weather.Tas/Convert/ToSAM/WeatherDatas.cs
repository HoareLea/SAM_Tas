using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Weather.Tas
{
    public static partial class Convert
    {
        public static List<WeatherData> ToSAM_WeatherDatas(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string extension = System.IO.Path.GetExtension(path).ToLower().Trim();
            if (extension.EndsWith("tbd"))
            {
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path))
                {
                    WeatherData weatherData = ToSAM_WeatherData(sAMTBDDocument);
                    if(weatherData != null)
                    {
                        return new List<WeatherData>() { weatherData};
                    }
                }
            }
            else if (extension.EndsWith("tsd"))
            {
                using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path))
                {
                    WeatherData weatherData = ToSAM_WeatherData(sAMTSDDocument);
                    if (weatherData != null)
                    {
                        return new List<WeatherData>() { weatherData };
                    }
                }
            }
            else if(extension.EndsWith("twd"))
            {
                using (SAMTWDDocument sAMTWDDocument = new SAMTWDDocument(path, true))
                {
                    TWD.Document document = sAMTWDDocument.Document;

                    List<TWD.WeatherFolder> weatherFolders = Query.WeatherFolders(document?.weatherRoot, true);
                    if (weatherFolders != null && weatherFolders.Count != 0)
                    {
                        List<WeatherData> result = new List<WeatherData>();
                        foreach (TWD.WeatherFolder weatherFolder in weatherFolders)
                        {
                            List<TWD.WeatherYear> weatherYears = weatherFolder.WeatherYears();
                            if (weatherYears != null && weatherYears.Count != 0)
                            {
                                foreach (TWD.WeatherYear weatherYear in weatherYears)
                                {
                                    WeatherData weatherData = weatherYear.ToSAM_WeatherData();
                                    if (weatherData != null)
                                    {
                                        result.Add(weatherData);
                                    }
                                }
                            }

                        }
                        return result;
                    }
                }
            }

            return null;
        }
    }
}