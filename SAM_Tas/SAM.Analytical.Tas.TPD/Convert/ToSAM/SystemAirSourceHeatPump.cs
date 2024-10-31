using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Core;

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

            SystemAirSourceHeatPump result = new SystemAirSourceHeatPump(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemAirSourceHeatPump displaySystemAirSourceHeatPump = Systems.Create.DisplayObject<DisplaySystemAirSourceHeatPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemAirSourceHeatPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)airSourceHeatPump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemAirSourceHeatPump.Transform(transform2D);
                }

                result = displaySystemAirSourceHeatPump; 
            }

            return result;
        }
    }
}
