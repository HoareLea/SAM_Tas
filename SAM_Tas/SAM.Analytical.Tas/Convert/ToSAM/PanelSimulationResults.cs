using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<PanelSimulationResult> ToSAM_PanelSimulationResults(this ZoneData zoneData, int index)
        {
            List<SurfaceData> surfaceDatas = zoneData?.SurfaceDatas();
            if(surfaceDatas == null)
            {
                return null;
            }

            List<PanelSimulationResult> result = new List<PanelSimulationResult>();
            foreach(SurfaceData surfaceData in surfaceDatas)
            {
                PanelSimulationResult panelSimulationResult = surfaceData?.ToSAM(index);
                if (panelSimulationResult == null)
                {
                    continue;
                }

                panelSimulationResult.SetValue(PanelSimulationResultParameter.ZoneName, zoneData.name);

                result.Add(panelSimulationResult);
            }

            return result;
        }
    }
}
