using System.Collections.Generic;

namespace SAM.Core.Tas
{
    public static partial class Modify
    {
        public static bool AssignSurfaceOutputSpecs(string path_TBD, string name)
        {
            if(string.IsNullOrWhiteSpace(path_TBD) || !System.IO.File.Exists(path_TBD) || name == null)
            {
                return false;
            }

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                

                result = AssignSurfaceOutputSpecs(sAMTBDDocument.TBDDocument, name);
                if (result)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static bool AssignSurfaceOutputSpecs(this TBD.TBDDocument tBDDocument, string name)
        {
            if(tBDDocument == null)
            {
                return false;
            }

            TBD.Building building = tBDDocument.Building;
            if (building == null)
            {
                return false;
            }

            List<TBD.SurfaceOutputSpec> surfaceOutputSpecs_TBD = building?.SurfaceOutputSpecs();
            if(surfaceOutputSpecs_TBD == null || surfaceOutputSpecs_TBD.Count == 0)
            {
                return false;
            }

            TBD.SurfaceOutputSpec surfaceOutputSpec = surfaceOutputSpecs_TBD.Find(x => x.name == name);
            if(surfaceOutputSpec == null)
            {
                return false;
            }

            List<TBD.buildingElement> buildingElements = building.BuildingElements();
            if(buildingElements == null || buildingElements.Count == 0)
            {
                return false;
            }
            
            foreach(TBD.buildingElement buildingElement in buildingElements)
            {
                buildingElement.AssignSurfaceOutputSpec(surfaceOutputSpec);
            }

            return true;
        }
    }
}
