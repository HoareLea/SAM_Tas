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

            SystemWaterSourceHeatPump result = new SystemWaterSourceHeatPump(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemWaterSourceHeatPump displaySystemWaterSourceHeatPump = Systems.Create.DisplayObject<DisplaySystemWaterSourceHeatPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemWaterSourceHeatPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)heatPump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemWaterSourceHeatPump.Transform(transform2D);
                }

                result = displaySystemWaterSourceHeatPump;

            }

            return result;
        }
    }
}
