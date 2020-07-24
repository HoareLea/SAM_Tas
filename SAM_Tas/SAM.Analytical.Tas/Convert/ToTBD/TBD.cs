using SAM.Core.Tas;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static bool ToTBD(this string path_T3D, string path_TBD, int day_First, int day_Last, int step, bool autoAssignConstructions)
        {
            if (string.IsNullOrWhiteSpace(path_T3D) || string.IsNullOrWhiteSpace(path_TBD))
                return false;

            bool result = false;
            using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
            {
                result = ToTBD(sAMT3DDocument, path_TBD, day_First, day_Last, step, autoAssignConstructions);
            }

            return result;
        }

        public static bool ToTBD(this SAMT3DDocument sAMT3DDocument, string path_TBD, int day_First, int day_Last, int step, bool autoAssignConstructions)
        {
            if (sAMT3DDocument == null)
                return false;

            return ToTBD(sAMT3DDocument.T3DDocument, path_TBD, day_First, day_Last, step, autoAssignConstructions);
        }

        public static bool ToTBD(this T3DDocument t3DDocument, string path_TBD, int day_First, int day_Last, int step, bool autoAssignConstructions)
        {
            if (t3DDocument == null || string.IsNullOrWhiteSpace(path_TBD))
                return false;

            int int_autoAssignConstructions = 0;
            if (autoAssignConstructions)
                int_autoAssignConstructions = 1;

            return t3DDocument.ExportNew(day_First, day_Last, step, 1, 1, 1, path_TBD, int_autoAssignConstructions, 0, 0);
        }
    }
}
