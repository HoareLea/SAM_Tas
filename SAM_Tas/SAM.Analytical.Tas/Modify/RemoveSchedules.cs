using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<string> RemoveSchedules(this string path_TBD, string sufix, bool caseSensitive = true, bool trim = false)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<string> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = RemoveSchedules(sAMTBDDocument, sufix, caseSensitive, trim);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<string> RemoveSchedules(this SAMTBDDocument sAMTBDDocument, string sufix, bool caseSensitive = true, bool trim = false)
        {
            if (sAMTBDDocument == null)
                return null;

            return RemoveSchedules(sAMTBDDocument.TBDDocument, sufix, caseSensitive, trim);
        }

        public static List<string> RemoveSchedules(this TBD.TBDDocument tBDDocument, string sufix, bool caseSensitive = true, bool trim = false)
        {
            if (tBDDocument == null || string.IsNullOrWhiteSpace(sufix))
                return null;

            TBD.Building building = tBDDocument.Building;
            if (building == null)
                return null;

            List<string> result = new List<string>();

            List<TBD.schedule> schedules = building.Schedules();
            if (schedules == null || schedules.Count == 0)
                return result;

            string sufix_Temp = sufix;

            if (trim)
                sufix_Temp = sufix_Temp.Trim();

            if (!caseSensitive)
                sufix_Temp = sufix_Temp.ToUpper();

            foreach (TBD.schedule schedule in schedules)
            {
                string name = schedule?.name;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (trim)
                    name = name.Trim();

                if (!caseSensitive)
                    name = name.ToUpper();

                if (name.EndsWith(sufix_Temp))
                    result.Add(schedule.name);
            }

            foreach (string name in result)
                tBDDocument.DeleteObjectByName(name);

            return result;
        }
    }
}