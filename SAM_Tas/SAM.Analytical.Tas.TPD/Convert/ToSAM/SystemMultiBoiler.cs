using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMultiBoiler ToSAM(this MultiBoiler multiBoiler)
        {
            if (multiBoiler == null)
            {
                return null;
            }

            dynamic @dynamic = multiBoiler;

            SystemMultiBoiler result = new SystemMultiBoiler(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemMultiBoiler displaySystemMultiBoiler = Systems.Create.DisplayObject<DisplaySystemMultiBoiler>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemMultiBoiler != null)
            {
                ITransform2D transform2D = ((IPlantComponent)multiBoiler).Transform2D();
                if (transform2D != null)
                {
                    displaySystemMultiBoiler.Transform(transform2D);
                }

                result = displaySystemMultiBoiler;
            }

            return result;
        }
    }
}
