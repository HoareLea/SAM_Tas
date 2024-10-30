using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterToWaterHeatPump ToSAM(this WaterToWaterHeatPump waterToWaterHeatPump)
        {
            if (waterToWaterHeatPump == null)
            {
                return null;
            }

            dynamic @dynamic = waterToWaterHeatPump;

            SystemWaterToWaterHeatPump systemWaterToWaterHeatPump = new SystemWaterToWaterHeatPump(@dynamic.Name);
            systemWaterToWaterHeatPump.Description = dynamic.Description;
            Modify.SetReference(systemWaterToWaterHeatPump, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemWaterToWaterHeatPump result = Systems.Create.DisplayObject<DisplaySystemWaterToWaterHeatPump>(systemWaterToWaterHeatPump, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)waterToWaterHeatPump).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}