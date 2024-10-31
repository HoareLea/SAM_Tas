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

            SystemWaterToWaterHeatPump result = new SystemWaterToWaterHeatPump(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemWaterToWaterHeatPump displaySystemWaterToWaterHeatPump = Systems.Create.DisplayObject<DisplaySystemWaterToWaterHeatPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemWaterToWaterHeatPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)waterToWaterHeatPump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemWaterToWaterHeatPump.Transform(transform2D);
                }

                result = displaySystemWaterToWaterHeatPump;
            }

            return result;
        }
    }
}