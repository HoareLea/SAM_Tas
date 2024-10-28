using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemChiller ToSAM(this Chiller chiller)
        {
            if (chiller == null)
            {
                return null;
            }

            bool directAbsorptionChiller = chiller.IsDirectAbsChiller == 1;
            
            dynamic @dynamic = chiller;

            SystemChiller systemChiller = null;
            if (directAbsorptionChiller)
            {
                systemChiller = new SystemAirSourceDirectAbsorptionChiller(@dynamic.Name);
            }
            else
            {
                systemChiller = new SystemAirSourceChiller(@dynamic.Name);
            }

            systemChiller.Description = dynamic.Description;
            Modify.SetReference(systemChiller, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            IDisplaySystemObject result = null;
            if (directAbsorptionChiller)
            {
                result = Systems.Create.DisplayObject<DisplaySystemAirSourceDirectAbsorptionChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }
            else
            {
                result = Systems.Create.DisplayObject<DisplaySystemAirSourceChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }

            ITransform2D transform2D = ((IPlantComponent)chiller).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result as SystemChiller;
        }
    }
}
