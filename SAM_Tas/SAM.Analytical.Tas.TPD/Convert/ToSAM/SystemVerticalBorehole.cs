using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemVerticalBorehole ToSAM(this GroundSource groundSource)
        {
            if (groundSource == null)
            {
                return null;
            }

            dynamic @dynamic = groundSource;

            SystemVerticalBorehole result = new SystemVerticalBorehole(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemVerticalBorehole displaySystemVerticalBorehole = Systems.Create.DisplayObject<DisplaySystemVerticalBorehole>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemVerticalBorehole != null)
            {
                ITransform2D transform2D = ((IPlantComponent)groundSource).Transform2D();
                if (transform2D != null)
                {
                    displaySystemVerticalBorehole.Transform(transform2D);
                }

                result = displaySystemVerticalBorehole;
            }

            return result;
        }
    }
}