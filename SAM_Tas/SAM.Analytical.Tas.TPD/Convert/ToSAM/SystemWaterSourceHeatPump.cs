using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterSourceHeatPump ToSAM(this HeatPump heatPump)
        {
            if (heatPump == null)
            {
                return null;
            }

            dynamic @dynamic = heatPump;

            SystemWaterSourceHeatPump systemWaterSourceHeatPump = new SystemWaterSourceHeatPump(@dynamic.Name);
            systemWaterSourceHeatPump.Description = dynamic.Description;
            Modify.SetReference(systemWaterSourceHeatPump, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemWaterSourceHeatPump result = Systems.Create.DisplayObject<DisplaySystemWaterSourceHeatPump>(systemWaterSourceHeatPump, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)heatPump).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
