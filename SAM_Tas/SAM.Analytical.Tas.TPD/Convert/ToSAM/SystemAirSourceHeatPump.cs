using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAirSourceHeatPump ToSAM(this AirSourceHeatPump airSourceHeatPump)
        {
            if (airSourceHeatPump == null)
            {
                return null;
            }

            dynamic @dynamic = airSourceHeatPump;

            SystemAirSourceHeatPump systemAirSourceHeatPump = new SystemAirSourceHeatPump(@dynamic.Name);
            systemAirSourceHeatPump.Description = dynamic.Description;
            Modify.SetReference(systemAirSourceHeatPump, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemAirSourceHeatPump result = Systems.Create.DisplayObject<DisplaySystemAirSourceHeatPump>(systemAirSourceHeatPump, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)airSourceHeatPump).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
