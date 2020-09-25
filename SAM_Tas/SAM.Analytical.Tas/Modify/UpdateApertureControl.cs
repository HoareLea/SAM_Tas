using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> UpdateApertureControl(this string path_TBD, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<Guid> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateApertureControl(sAMTBDDocument, apertureConstructions);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<Guid> UpdateApertureControl(this SAMTBDDocument sAMTBDDocument, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if (sAMTBDDocument == null)
                return null;

            return UpdateApertureControl(sAMTBDDocument.TBDDocument, apertureConstructions);
        }

        public static List<Guid> UpdateApertureControl(this TBDDocument tBDDocument, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if (tBDDocument == null || apertureConstructions == null)
                return null;

            Building builidng = tBDDocument.Building;
            if (builidng == null)
                return null;

            builidng.RemoveApertureTypes();
            tBDDocument.RemoveSchedules("_APSCHED");

            List<Guid> result = new List<Guid>();

            List<buildingElement> buildingElements = builidng.BuildingElements();
            if (buildingElements == null)
                return result;

            List<dayType> dayTypes = builidng.DayTypes();
            dayTypes.RemoveAll(x => x.name.Equals("CDD") || x.name.Equals("HDD"));

            foreach (ApertureConstruction apertureConstruction in apertureConstructions)
            {
                string paneApertureConstructionName = apertureConstruction.PaneApertureConstructionUniqueName();
                
                buildingElement buildingElement = buildingElements.Find(x => x.name == paneApertureConstructionName);
                if (buildingElement == null)
                    continue;

                if (builidng.AssignApertureTypes(buildingElement, dayTypes, apertureConstruction))
                {
                    Guid guid;
                    if (Guid.TryParse(buildingElement.GUID, out guid))
                        result.Add(guid);
                }
            }

            return result;
        }
    }
}