using System.Collections.Generic;

namespace SAM.Core.Tas
{
    public static partial class Modify
    {
        public static List<bool> UpdateSurfaceOutputSpecs(string path_TBD, IEnumerable<SurfaceOutputSpec> surfaceOutputSpecs)
        {
            if(string.IsNullOrWhiteSpace(path_TBD) || !System.IO.File.Exists(path_TBD) || surfaceOutputSpecs == null)
            {
                return null;
            }

            List<bool> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                

                result = UpdateSurfaceOutputSpecs(sAMTBDDocument.TBDDocument, surfaceOutputSpecs);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<bool> UpdateSurfaceOutputSpecs(this TBD.TBDDocument tBDDocument, IEnumerable<SurfaceOutputSpec> surfaceOutputSpecs)
        {
            if(tBDDocument == null || surfaceOutputSpecs == null)
            {
                return null;
            }

            TBD.Building building = tBDDocument.Building;
            if(building == null)
            {
                return null;
            }

            List<bool> result = new List<bool>();

            List<TBD.SurfaceOutputSpec> surfaceOutputSpecs_TBD = building?.SurfaceOutputSpecs();
            foreach(TBD.SurfaceOutputSpec surfaceOutputSpec_TBD in surfaceOutputSpecs_TBD)
            {
                tBDDocument.DeleteObjectByName(surfaceOutputSpec_TBD.name);
            }
            surfaceOutputSpecs_TBD.Clear();

            foreach (SurfaceOutputSpec surfaceOutputSpec in surfaceOutputSpecs)
            {
                if(surfaceOutputSpec == null)
                {
                    continue;
                }

                TBD.SurfaceOutputSpec surfaceOutputSpec_TBD = surfaceOutputSpecs_TBD.Find(x => x.name == surfaceOutputSpec.Name);
                if(surfaceOutputSpec_TBD == null)
                {
                    surfaceOutputSpec_TBD = building.AddSurfaceOutputSpec();

                    if (surfaceOutputSpec.Name != null)
                    {
                        surfaceOutputSpec_TBD.name = surfaceOutputSpec.Name;
                    }
                }

                surfaceOutputSpec_TBD.description = surfaceOutputSpec.Description;
                surfaceOutputSpec_TBD.apertureData = surfaceOutputSpec.ApertureData ? 1 : 0;
                surfaceOutputSpec_TBD.condensation = surfaceOutputSpec.Condensation ? 1 : 0;
                surfaceOutputSpec_TBD.convection = surfaceOutputSpec.Convection ? 1 : 0;
                surfaceOutputSpec_TBD.solarGain = surfaceOutputSpec.SolarGain ? 1 : 0;
                surfaceOutputSpec_TBD.conduction = surfaceOutputSpec.Conduction ? 1 : 0;
                surfaceOutputSpec_TBD.longWave = surfaceOutputSpec.LongWave ? 1 : 0;
                surfaceOutputSpec_TBD.dryBulbTemp = surfaceOutputSpec.Temperature ? 1 : 0;
            }

            return result;
        }
    }
}
