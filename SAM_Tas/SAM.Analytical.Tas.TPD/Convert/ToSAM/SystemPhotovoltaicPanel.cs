using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPhotovoltaicPanel ToSAM(this PVPanel pVPanel)
        {
            if (pVPanel == null)
            {
                return null;
            }

            dynamic @dynamic = pVPanel;

            SystemPhotovoltaicPanel result = new SystemPhotovoltaicPanel(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemPhotovoltaicPanel displaySystemPhotovoltaicPanel = Systems.Create.DisplayObject<DisplaySystemPhotovoltaicPanel>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemPhotovoltaicPanel != null)
            {
                ITransform2D transform2D = ((IPlantComponent)pVPanel).Transform2D();
                if (transform2D != null)
                {
                    displaySystemPhotovoltaicPanel.Transform(transform2D);
                }

                result = displaySystemPhotovoltaicPanel;
            }

            return result;
        }
    }
}