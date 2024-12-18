using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMultiChiller ToSAM(this MultiChiller multiChiller)
        {
            if (multiChiller == null)
            {
                return null;
            }

            dynamic @dynamic = multiChiller;

            SystemMultiChiller result = new SystemMultiChiller(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.DesignPressureDrop = @dynamic.DesignPressureDrop;
            result.DesignTemperatureDiffrence = @dynamic.DesignDeltaT;
            result.Duty = @dynamic.Duty?.ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemMultiChiller displaySystemMultiChiller = Systems.Create.DisplayObject<DisplaySystemMultiChiller>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemMultiChiller != null)
            {
                ITransform2D transform2D = ((IPlantComponent)multiChiller).Transform2D();
                if (transform2D != null)
                {
                    displaySystemMultiChiller.Transform(transform2D);
                }

                result = displaySystemMultiChiller;
            }

            return result;
        }
    }
}
