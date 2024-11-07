using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHorizontalExchanger ToSAM(this HorizontalGHE horizontalGHE)
        {
            if (horizontalGHE == null)
            {
                return null;
            }

            dynamic @dynamic = horizontalGHE;

            SystemHorizontalExchanger result = new SystemHorizontalExchanger(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemHorizontalExchanger displaySystemHorizontalExchanger = Systems.Create.DisplayObject<DisplaySystemHorizontalExchanger>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemHorizontalExchanger != null)
            {
                ITransform2D transform2D = ((IPlantComponent)horizontalGHE).Transform2D();
                if (transform2D != null)
                {
                    displaySystemHorizontalExchanger.Transform(transform2D);
                }

                result = displaySystemHorizontalExchanger;
            }

            return result;
        }
    }
}