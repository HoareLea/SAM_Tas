using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<SurfaceData> SurfaceDatas(this ZoneData zoneData)
        {
            if (zoneData == null)
                return null;

            List<SurfaceData> result = new List<SurfaceData>();

            int index = 1;
            SurfaceData surfaceData = zoneData.GetSurfaceData(index);
            while (surfaceData != null)
            {
                result.Add(surfaceData);
                index++;
                surfaceData = zoneData.GetSurfaceData(index);
            }
            return result;
        }
    }
}