using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSurfaceWaterExchanger ToSAM(this SurfaceWaterExchanger surfaceWaterExchanger)
        {
            if (surfaceWaterExchanger == null)
            {
                return null;
            }

            dynamic @dynamic = surfaceWaterExchanger;

            SystemSurfaceWaterExchanger result = new SystemSurfaceWaterExchanger(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            result.Capacity = dynamic.Capacity;
            result.DesignPressureDrop = dynamic.DesignPressureDrop;
            result.Efficiency = ((ProfileData)dynamic.Efficiency)?.ToSAM();
            result.PondVolume = dynamic.PondVolume;
            result.PondSurfaceArea = dynamic.PondSurfaceArea;
            result.PondPerimeter = dynamic.PondPerimeter;
            result.GroundConductivity = dynamic.GroundConductivity;
            result.WaterTableDepth = dynamic.WaterTableDepth;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSurfaceWaterExchanger displaySystemSurfaceWaterExchanger = Systems.Create.DisplayObject<DisplaySystemSurfaceWaterExchanger>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemSurfaceWaterExchanger != null)
            {
                ITransform2D transform2D = ((IPlantComponent)surfaceWaterExchanger).Transform2D();
                if (transform2D != null)
                {
                    displaySystemSurfaceWaterExchanger.Transform(transform2D);
                }

                result = displaySystemSurfaceWaterExchanger;
            }

            return result;
        }
    }
}