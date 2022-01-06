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

            if (System.IO.File.Exists(path_TSD))
            {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(path_TSD);
                while (true)
                {
                    if (!Core.Query.Locked(fileInfo))
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            }

            return true;
        }
    }
}