using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSolarPanel ToSAM(this SolarPanel solarPanel)
        {
            if (solarPanel == null)
            {
                return null;
            }

            dynamic @dynamic = solarPanel;

            SystemSolarPanel result = new SystemSolarPanel(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSolarPanel displaySystemSolarPanel = Systems.Create.DisplayObject<DisplaySystemSolarPanel>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemSolarPanel != null)
            {
                ITransform2D transform2D = ((IPlantComponent)solarPanel).Transform2D();
                if (transform2D != null)
                {
                    displaySystemSolarPanel.Transform(transform2D);
                }

                result = displaySystemSolarPanel;
            }

            return result;
        }
    }
}