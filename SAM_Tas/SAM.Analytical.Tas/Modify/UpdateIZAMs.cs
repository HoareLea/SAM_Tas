using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<string> UpdateIZAMs(this string path_TBD, IEnumerable<Space> spaces)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<string> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateIZAMs(sAMTBDDocument, spaces);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<string> UpdateIZAMs(this SAMTBDDocument sAMTBDDocument, IEnumerable<Space> spaces)
        {
            if (sAMTBDDocument == null)
                return null;

            return UpdateIZAMs(sAMTBDDocument.TBDDocument, spaces);
        }

        public static List<string> UpdateIZAMs(this TBDDocument tBDDocument, IEnumerable<Space> spaces)
        {
            if (tBDDocument == null || spaces == null)
                return null;

            Building building = tBDDocument.Building;
            if (building == null)
                return null;

            building.RemoveIZAMs();

            RemoveSchedules(tBDDocument, "_IZAMSCHED");

            List<IZAM> iZAMs = Create.IZAMs(building, spaces);
            if (iZAMs == null)
                return new List<string>();

            return iZAMs.ConvertAll(x => x.name);
        }
    }
}