using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> AddDesignDays(this string path_TBD, IEnumerable<DesignDay> coolingDesignDays, IEnumerable<DesignDay> heatingDesignDays, int repetitions = 30)
        {
            if (string.IsNullOrWhiteSpace(path_TBD) || (coolingDesignDays == null && heatingDesignDays == null))
                return null;

            List<Guid> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = AddDesignDays(sAMTBDDocument, coolingDesignDays, heatingDesignDays, repetitions);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<Guid> AddDesignDays(this SAMTBDDocument sAMTBDDocument, IEnumerable<DesignDay> coolingDesignDays, IEnumerable<DesignDay> heatingDesignDays, int repetitions = 30)
        {
            if (sAMTBDDocument == null)
                return null;

            return AddDesignDays(sAMTBDDocument.TBDDocument, coolingDesignDays, heatingDesignDays, repetitions);
        }

        public static List<Guid> AddDesignDays(this TBD.TBDDocument tBDDocument, IEnumerable<DesignDay> coolingDesignDays, IEnumerable<DesignDay> heatingDesignDays, int repetitions = 30)
        {
            return AddDesignDays(tBDDocument?.Building, coolingDesignDays, heatingDesignDays, repetitions);
        }

        public static List<Guid> AddDesignDays(this TBD.Building building, IEnumerable<DesignDay> coolingDesignDays, IEnumerable<DesignDay> heatingDesignDays, int repetitions = 30)
        {
            if(building == null)
            {
                return null;
            }

            building.ClearDesignDays();

            TBD.Calendar calendar = building.GetCalendar();

            List<TBD.dayType> dayTypes = Query.DayTypes(calendar);
            if (dayTypes.Find(x => x.name == "HDD") == null)
            {
                TBD.dayType dayType = calendar.AddDayType();
                dayType.name = "HDD";
            }

            if (dayTypes.Find(x => x.name == "CDD") == null)
            {
                TBD.dayType dayType = calendar.AddDayType();
                dayType.name = "CDD";
            }

            dayTypes = building.DayTypes();

            List<Guid> result = new List<Guid>();

            if(coolingDesignDays != null && coolingDesignDays.Count() != 0)
            {
                TBD.dayType dayType = dayTypes?.Find(x => x.name == "CDD");
                List<TBD.CoolingDesignDay> coolingDesignDays_TBD = building.CoolingDesignDays();
                foreach(DesignDay designDay in coolingDesignDays)
                {
                    if(designDay == null)
                    {
                        continue;
                    }

                    TBD.CoolingDesignDay coolingDesignDay_TBD = coolingDesignDays_TBD?.Find(x => x.name == designDay.Name);
                    if(coolingDesignDay_TBD == null)
                    {
                        coolingDesignDay_TBD = building.AddCoolingDesignDay();
                    }

                    coolingDesignDay_TBD.Update(designDay, dayType, repetitions);
                    result.Add(Guid.Parse(coolingDesignDay_TBD.GUID));
                }
            }

            if (heatingDesignDays != null && heatingDesignDays.Count() != 0)
            {
                TBD.dayType dayType = dayTypes?.Find(x => x.name == "HDD");

                List<TBD.HeatingDesignDay> heatingDesignDays_TBD = building.HeatingDesignDays();
                foreach (DesignDay designDay in heatingDesignDays)
                {
                    if (designDay == null)
                    {
                        continue;
                    }

                    TBD.HeatingDesignDay heatingDesignDay_TBD = heatingDesignDays_TBD?.Find(x => x.name == designDay.Name);
                    if (heatingDesignDay_TBD == null)
                    {
                        heatingDesignDay_TBD = building.AddHeatingDesignDay();
                    }

                    heatingDesignDay_TBD.Update(designDay, dayType, repetitions);
                    result.Add(Guid.Parse(heatingDesignDay_TBD.GUID));
                }
            }

            return result;

        }
    }
}