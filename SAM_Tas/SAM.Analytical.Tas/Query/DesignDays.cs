using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.DesignDay> DesignDays(this TBD.CoolingDesignDay coolingDesignDay)
        {
            List<TBD.DesignDay> result = new List<TBD.DesignDay>();

            int index = 0;
            TBD.DesignDay designDay = coolingDesignDay?.GetDesignDay(index);
            while (designDay != null)
            {
                result.Add(designDay);
                index++;

                designDay = coolingDesignDay?.GetDesignDay(index);
            }

            return result;
        }

        public static List<TBD.DesignDay> DesignDays(this TBD.HeatingDesignDay heatingDesignDay)
        {
            List<TBD.DesignDay> result = new List<TBD.DesignDay>();

            int index = 0;
            TBD.DesignDay designDay = heatingDesignDay?.GetDesignDay(index);
            while (designDay != null)
            {
                result.Add(designDay);
                index++;

                designDay = heatingDesignDay?.GetDesignDay(index);
            }

            return result;
        }

        public static List<DesignDay> DesignDays(this string path, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays)
        {
            coolingDesignDays = null;
            heatingDesignDays = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            List<DesignDay> result = null;

            string extension = System.IO.Path.GetExtension(path).ToLower().Trim();
            if (extension.EndsWith("tbd"))
            {
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path))
                {
                    result = DesignDays(sAMTBDDocument, out coolingDesignDays, out heatingDesignDays);
                }
            }
            else if (extension.EndsWith("tsd"))
            {
                using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path))
                {
                    result = DesignDays(sAMTSDDocument, out coolingDesignDays, out heatingDesignDays);
                }
            }

            return result;
        }

        public static List<DesignDay> DesignDays(this SAMTSDDocument sAMTSDDocument, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays, int year = 2018)
        {
            coolingDesignDays = null;
            heatingDesignDays = null;

            if (sAMTSDDocument == null)
            {
                return null;
            }

            throw new NotImplementedException();


            //List<TSD.CoolingDesignData> coolingDesignDatas = sAMTSDDocument.TSDDocument?.SimulationData?.CoolingDesignDatas();
            //if(coolingDesignDatas != null)
            //{
            //    foreach(TSD.CoolingDesignData coolingDesignData in coolingDesignDatas)
            //    {
            //        int dayOfYear = coolingDesignData.firstDay;
            //        DateTime dateTime = new DateTime(year, 1, 1);
            //        dateTime.AddDays(dayOfYear);

            //        List<TSD.ZoneData> zoneDatas = coolingDesignData.ZoneDatas();
            //        if(zoneDatas != null && zoneDatas.Count != 0)
            //        {
            //            foreach(TSD.ZoneData zoneData in zoneDatas)
            //            {
            //                DesignDay designDay = new DesignDay(zoneData.name, System.Convert.ToInt16(dateTime.Year), System.Convert.ToByte(dateTime.Month), System.Convert.ToByte(dateTime.Day));

            //                float[] values = (zoneData.GetDailyZoneResult(coolingDesignData.firstDay, (short)TSD.tsdZoneArray.dryBulbTemp) as IEnumerable).Cast<float>().ToArray();
            //                if(values != null)
            //                {
            //                    for(int i = 0; i <= 23; i++)
            //                    {
            //                        designDay[Weather.WeatherDataType.DryBulbTemperature, i] = System.Convert.ToDouble(values[i]);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            
        }

        public static List<DesignDay> DesignDays(this SAMTBDDocument sAMTBDDocument, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays, int year = 2018)
        {
            coolingDesignDays = null;
            heatingDesignDays = null;

            if (sAMTBDDocument == null)
            {
                return null;
            }

            List<DesignDay> result = new List<DesignDay>();

            List<TBD.CoolingDesignDay> coolingDesignDays_TBD = sAMTBDDocument.TBDDocument?.Building.CoolingDesignDays();
            if(coolingDesignDays_TBD != null)
            {
                coolingDesignDays = new List<DesignDay>();
                foreach(TBD.CoolingDesignDay coolingDesignDay_TBD in coolingDesignDays_TBD)
                {
                    List<TBD.DesignDay> designDays_TBD = coolingDesignDay_TBD.DesignDays();
                    if(designDays_TBD != null && designDays_TBD.Count != 0)
                    {
                        foreach(TBD.DesignDay designDay_TBD in designDays_TBD)
                        {
                            DesignDay designDay = designDay_TBD?.ToSAM(year);
                            if(designDay == null)
                            {
                                continue;
                            }

                            result.Add(designDay);
                            coolingDesignDays.Add(designDay);
                        }
                    }
                }
            }

            List<TBD.HeatingDesignDay> heatingDesignDays_TBD = sAMTBDDocument.TBDDocument?.Building.HeatingDesignDays();
            if (heatingDesignDays_TBD != null)
            {
                heatingDesignDays = new List<DesignDay>();
                foreach (TBD.HeatingDesignDay heatingDesignDay_TBD in heatingDesignDays_TBD)
                {
                    List<TBD.DesignDay> designDays_TBD = heatingDesignDay_TBD.DesignDays();
                    if (designDays_TBD != null && designDays_TBD.Count != 0)
                    {
                        foreach (TBD.DesignDay designDay_TBD in designDays_TBD)
                        {
                            DesignDay designDay = designDay_TBD?.ToSAM(year);
                            if (designDay == null)
                            {
                                continue;
                            }

                            result.Add(designDay);
                            heatingDesignDays.Add(designDay);
                        }
                    }
                }
            }

            return result;

        }
    }
}