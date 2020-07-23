using SAM.Core;

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

            return panel;
        }
    }
}
