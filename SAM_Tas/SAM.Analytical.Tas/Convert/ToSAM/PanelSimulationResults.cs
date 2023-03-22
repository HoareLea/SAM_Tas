using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<SurfaceSimulationResult> ToSAM_SurfaceSimulationResults(this ZoneData zoneData, int index)
        {
            List<SurfaceData> surfaceDatas = zoneData?.SurfaceDatas();
            if(surfaceDatas == null)
            {
                return null;
            }

            List<SurfaceSimulationResult> result = new List<SurfaceSimulationResult>();
            foreach(SurfaceData surfaceData in surfaceDatas)
            {
                Core.Tas.ZoneSurfaceReference zoneSurfaceReference = new Core.Tas.ZoneSurfaceReference(surfaceData.surfaceNumber, zoneData.zoneGUID);

                SurfaceSimulationResult surfaceSimulationResult = surfaceData?.ToSAM(index);
                if (surfaceSimulationResult == null)
                {
                    continue;
                }

                surfaceSimulationResult.SetValue(SurfaceSimulationResultParameter.ZoneSurfaceReference, zoneSurfaceReference);
                
                result.Add(surfaceSimulationResult);
            }

            return result;
        }
    }
}
