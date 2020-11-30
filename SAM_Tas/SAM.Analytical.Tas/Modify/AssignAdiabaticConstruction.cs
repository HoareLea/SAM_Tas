using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> AssignAdiabaticConstruction(this string path_TBD, string constructionName_Adiabatic, IEnumerable<string> constructionNames_Sufixes, bool caseSensitive = true, bool trim = false)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<Guid> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = AssignAdiabaticConstruction(sAMTBDDocument, constructionName_Adiabatic, constructionNames_Sufixes, caseSensitive, trim);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<Guid> AssignAdiabaticConstruction(this SAMTBDDocument sAMTBDDocument, string constructionName_Adiabatic, IEnumerable<string> constructionNames_Sufixes, bool caseSensitive = true, bool trim = false)
        {
            if (sAMTBDDocument == null)
                return null;

            return AssignAdiabaticConstruction(sAMTBDDocument.TBDDocument, constructionName_Adiabatic, constructionNames_Sufixes, caseSensitive, trim);
        }

        public static List<Guid> AssignAdiabaticConstruction(this TBD.TBDDocument tBDDocument, string constructionName_Adiabatic, IEnumerable<string> constructionNames_Sufixes, bool caseSensitive = true, bool trim = false)
        {
            return AssignAdiabaticConstruction(tBDDocument?.Building, constructionName_Adiabatic, constructionNames_Sufixes, caseSensitive, trim);
        }

        public static List<Guid> AssignAdiabaticConstruction(this TBD.Building building, string constructionName_Adiabatic, IEnumerable<string> constructionNames_Sufixes, bool caseSensitive = true, bool trim = false)
        {
            if (building == null || string.IsNullOrWhiteSpace(constructionName_Adiabatic) || constructionNames_Sufixes == null)
                return null;

            List<Guid> result = new List<Guid>();

            if (constructionNames_Sufixes.Count() == 0)
                return result;

            List<TBD.buildingElement> buildingElements = building.BuildingElements();
            if (buildingElements == null || buildingElements.Count == 0)
                return result;

            TBD.Construction construction_Adiabatic = building.Construction(constructionName_Adiabatic, caseSensitive, trim);
            if (construction_Adiabatic == null)
                return result;

            List<string> constructionNames_Sufixes_Temp = new List<string>(constructionNames_Sufixes);

            constructionNames_Sufixes_Temp.RemoveAll(x => string.IsNullOrEmpty(x));

            if (trim)
                constructionNames_Sufixes_Temp = constructionNames_Sufixes_Temp.ConvertAll(x => x.Trim());

            if (!caseSensitive)
                constructionNames_Sufixes_Temp = constructionNames_Sufixes_Temp.ConvertAll(x => x.ToUpper());

            foreach (TBD.buildingElement buildingElement in buildingElements)
            {
                string name = buildingElement?.name;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (trim)
                    name = name.Trim();

                if (!caseSensitive)
                    name = name.ToUpper();

                foreach(string constructionName_Sufix in constructionNames_Sufixes_Temp)
                {
                    if(name.EndsWith(constructionName_Sufix))
                    {
                        buildingElement.AssignConstruction(construction_Adiabatic);
                        result.Add(Guid.Parse(buildingElement.GUID));
                        break;
                    }
                }
            }
            return result;
        }
    }
}