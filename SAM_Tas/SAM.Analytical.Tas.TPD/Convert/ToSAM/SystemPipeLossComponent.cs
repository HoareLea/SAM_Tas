using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPipeLossComponent ToSAM(this PipeLossComponent pipeLossComponent)
        {
            if (pipeLossComponent == null)
            {
                return null;
            }

            dynamic @dynamic = pipeLossComponent;

            SystemPipeLossComponent result = new SystemPipeLossComponent(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.DesignPressureDrop = dynamic.DesignPressureDrop;
            result.Capacity = dynamic.Capacity;
            result.Length = dynamic.length;
            result.InsidePipeDiameter = dynamic.PipeDiamIn;
            result.OutsidePipeDiameter = dynamic.PipeDiamOut;
            result.PipeConductivity = dynamic.PipeConductivity;
            result.InsulationThickness = dynamic.InsThickness;
            result.InsulationConductivity = dynamic.InsConductivity;
            result.AmbientTemperature = ((ProfileData)dynamic.AmbTemp)?.ToSAM();

            result.IsUnderground = dynamic.IsUnderground;
            result.GroundConductivity = dynamic.GrConductivity;
            result.GroundHeatCapacity = dynamic.GrHeatCapacity;
            result.GroundDensity = dynamic.GrDensity;
            result.GroundTemperature = dynamic.GrTemp;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemPipeLossComponent displaySystemPipeLossComponent = Systems.Create.DisplayObject<DisplaySystemPipeLossComponent>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemPipeLossComponent != null)
            {
                ITransform2D transform2D = ((IPlantComponent)pipeLossComponent).Transform2D();
                if (transform2D != null)
                {
                    displaySystemPipeLossComponent.Transform(transform2D);
                }

                result = displaySystemPipeLossComponent;
            }

            return result;
        }
    }
}
