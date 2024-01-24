using SAM.Core.Tas;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool Simulate(string path_TBD, string path_TSD, int day_First, int day_Last)
        {
            if (string.IsNullOrWhiteSpace(path_TBD)  || string.IsNullOrWhiteSpace(path_TSD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = Simulate(sAMTBDDocument, path_TSD, day_First, day_Last);
                if(result)
                {
                    sAMTBDDocument.Save();
                }
            }

            return result;
        }

        public static bool Simulate(SAMTBDDocument sAMTBDDocument, string path_TSD, int day_First, int day_Last)
        {
            return Simulate(sAMTBDDocument?.TBDDocument, path_TSD, day_First, day_Last);
        }

        public static bool Simulate(TBD.TBDDocument tBDDocument, string path_TSD, int day_First, int day_Last)
        {
            if (tBDDocument == null || string.IsNullOrWhiteSpace(path_TSD))
                return false;

            tBDDocument.simulate(day_First, day_Last, 0, 1, 0, 0, path_TSD, 1, 0);

            return Core.Query.WaitToUnlock(path_TSD);
        }
    }
}