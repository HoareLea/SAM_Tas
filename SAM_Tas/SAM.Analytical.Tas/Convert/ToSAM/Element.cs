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
            parameterSet.Add("IsUsed", element.isUsed == 1);

            PlanarBoundary3D planarBoundary3D = null;

            Panel panel = new Panel(new Construction(element.name), Query.PanelType(element.BEType), planarBoundary3D);

            return panel;
        }
    }
}
