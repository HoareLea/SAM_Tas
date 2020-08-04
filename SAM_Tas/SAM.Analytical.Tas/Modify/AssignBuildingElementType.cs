using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> AssignBuildingElementType(this string path_TBD, TBD.BuildingElementType buildingElementType, IEnumerable<string> names, bool caseSensitive = true, bool trim = false)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<Guid> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = AssignBuildingElementType(sAMTBDDocument, buildingElementType, names, caseSensitive, trim);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<Guid> AssignBuildingElementType(this SAMTBDDocument sAMTBDDocument, TBD.BuildingElementType buildingElementType, IEnumerable<string> names, bool caseSensitive = true, bool trim = false)
        {
            if (sAMTBDDocument == null)
                return null;

            return AssignBuildingElementType(sAMTBDDocument.TBDDocument, buildingElementType, names, caseSensitive, trim);
        }

        public static List<Guid> AssignBuildingElementType(this TBD.TBDDocument tBDDocument, TBD.BuildingElementType buildingElementType, IEnumerable<string> names, bool caseSensitive = true, bool trim = false)
        {
            return AssignBuildingElementType(tBDDocument?.Building, buildingElementType, names, caseSensitive, trim);
        }

        public static List<Guid> AssignBuildingElementType(this TBD.Building building, TBD.BuildingElementType buildingElementType, IEnumerable<string> names, bool caseSensitive = true, bool trim = false)
        {
            if (building == null || names == null)
                return null;

            List<Guid> result = new List<Guid>();

            if (names.Count() == 0)
                return result;

            List<TBD.buildingElement> buildingElements = building.BuildingElements();
            if (buildingElements == null || buildingElements.Count == 0)
                return result;

            List<string> names_Temp = new List<string>(names);

            names_Temp.RemoveAll(x => string.IsNullOrEmpty(x));

            if (trim)
                names_Temp = names_Temp.ConvertAll(x => x.Trim());

            if (!caseSensitive)
                names_Temp = names_Temp.ConvertAll(x => x.ToUpper());

            foreach (TBD.buildingElement buildingElement in buildingElements)
            {
                string name = buildingElement?.name;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (trim)
                    name = name.Trim();

                if (!caseSensitive)
                    name = name.ToUpper();

                if (!names_Temp.Contains(name))
                    continue;

                buildingElement.BEType = (int)buildingElementType;
                result.Add(Guid.Parse(buildingElement.GUID));
            }
            return result;
        }
    }
}