using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> AssignRooflightBuilidingElementType(this string path_TBD, IEnumerable<string> names, bool caseSensitive = true, bool trim = false)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<Guid> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = AssignRooflightBuilidingElementType(sAMTBDDocument, names, caseSensitive, trim);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<Guid> AssignRooflightBuilidingElementType(this SAMTBDDocument sAMTBDDocument, IEnumerable<string> names, bool caseSensitive = true, bool trim = false)
        {
            if (sAMTBDDocument == null)
                return null;

            return AssignRooflightBuilidingElementType(sAMTBDDocument.TBDDocument, names, caseSensitive, trim);
        }

        public static List<Guid> AssignRooflightBuilidingElementType(this TBD.TBDDocument tBDDocument, IEnumerable<string> names, bool caseSensitive = true, bool trim = false)
        {
            return AssignRooflightBuilidingElementType(tBDDocument?.Building, names, caseSensitive, trim);
        }

        public static List<Guid> AssignRooflightBuilidingElementType(this TBD.Building building, IEnumerable<string> names, bool caseSensitive = true, bool trim = false)
        {
            return AssignBuildingElementType(building, TBD.BuildingElementType.ROOFLIGHT, names, caseSensitive, trim);
        }
    }
}