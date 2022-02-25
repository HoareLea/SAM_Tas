using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Weather.Tas
{
    public static partial class Convert
    {
        public static List<WeatherData> ToSAM_WeatherDatas(string path_TWD)
        {
            if (string.IsNullOrWhiteSpace(path_TWD))
            {
                return null;
            }

            List<WeatherData> result = null;

            using (SAMTWDDocument sAMTWDDocument = new SAMTWDDocument(path_TWD, true))
            {
                TWD.Document document = sAMTWDDocument.Document;

            }


            return result;
        }
    }
}