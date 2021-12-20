using SAM.Core.Tas;
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

        public static List<TBD.DesignDay> DesignDays(this string path, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays)
        {
            coolingDesignDays = null;
            heatingDesignDays = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            List<TBD.DesignDay> result = null;

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

        public static List<TBD.DesignDay> DesignDays(this SAMTSDDocument sAMTSDDocument, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays)
        {
            coolingDesignDays = null;
            heatingDesignDays = null;

            if (sAMTSDDocument == null)
            {
                return null;
            }

            throw new System.Exception();
        }

        public static List<TBD.DesignDay> DesignDays(this SAMTBDDocument sAMTBDDocument, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays)
        {
            throw new System.Exception();
        }
    }
}