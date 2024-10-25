using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHeatPump ToSAM(this HeatPump heatPump)
        {
            if (heatPump == null)
            {
                return null;
            }

            dynamic @dynamic = heatPump;

            SystemHeatPump systemHeatPump = new SystemHeatPump(@dynamic.Name);
            systemHeatPump.Description = dynamic.Description;
            Modify.SetReference(systemHeatPump, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemHeatPump result = Systems.Create.DisplayObject<DisplaySystemHeatPump>(systemHeatPump, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)heatPump).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}