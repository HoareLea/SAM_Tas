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

            Panel panel = Analytical.Create.Panel(new Construction(element.name), Query.PanelType(element.BEType), planarBoundary3D);
            panel.Add(parameterSet);

            return panel;
        }

        public static Panel ToSAM(this TAS3D.shade shade)
        {
            if (shade == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, shade);
            
            PlanarBoundary3D planarBoundary3D = null;

            Panel panel = Analytical.Create.Panel(new Construction(shade.name), PanelType.Shade, planarBoundary3D);
            panel.Add(parameterSet);

            return panel;
        }

        public static Panel ToSAM(this SurfaceData surfaceData, IEnumerable<PanelDataType> panelDataTypes = null)
        {
            if (surfaceData == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, surfaceData);

            if (panelDataTypes != null)
            {
                foreach (PanelDataType panelDataType in panelDataTypes)
                {
                    List<double> values = surfaceData.AnnualSurfaceResult<double>(panelDataType);
                    if (values == null)
                        continue;

                    JArray jArray = new JArray();
                    values.ForEach(x => jArray.Add(x));

                    parameterSet.Add(panelDataType.Text(), jArray);
                }
            }

            PanelType panelType = Query.PanelType(surfaceData.BEType);

            Panel panel = Analytical.Create.Panel(new Construction(surfaceData.BEName), panelType);
            panel.Add(parameterSet);

            return panel;
        }
    }
}
