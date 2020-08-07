using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Panel ToSAM(this TAS3D.Element element)
        {
            if (element == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, element);

            PlanarBoundary3D planarBoundary3D = null;

            Panel panel = new Panel(new Construction(element.name), Query.PanelType(element.BEType), planarBoundary3D);
            panel.Add(parameterSet);

            return panel;
        }

        public static Panel ToSAM(this TAS3D.shade shade)
        {
            if (shade == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, shade);
            
            PlanarBoundary3D planarBoundary3D = null;

            Panel panel = new Panel(new Construction(shade.name), PanelType.Shade, planarBoundary3D);
            panel.Add(parameterSet);

            return panel;
        }

        public static Panel ToSAM(this SurfaceData surfaceData, IEnumerable<ResultType> resultTypes = null)
        {
            if (surfaceData == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, surfaceData);

            if (resultTypes != null)
            {
                foreach (ResultType resultType in resultTypes)
                {
                    List<double> values = surfaceData.AnnualSurfaceResult<double>(resultType);
                    if (values == null)
                        continue;

                    JArray jArray = new JArray();
                    values.ForEach(x => jArray.Add(x));

                    parameterSet.Add(resultType.Text(), jArray);
                }
            }

            PanelType panelType = Query.PanelType(surfaceData.BEType);

            PlanarBoundary3D planarBoundary3D = null;

            Panel panel = new Panel(new Construction(surfaceData.BEName), panelType, planarBoundary3D);
            panel.Add(parameterSet);

            return panel;
        }
    }
}
