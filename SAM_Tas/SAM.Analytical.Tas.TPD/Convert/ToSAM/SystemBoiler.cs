using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemBoiler ToSAM(this BoilerPlant boilerPlant)
        {
            if (boilerPlant == null)
            {
                return null;
            }

            dynamic @dynamic = boilerPlant;

            SystemBoiler result = new SystemBoiler(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            int count = @dynamic.GetFuelSourceCount();
            FuelSource fuelSource = null;
            if(count > 0)
            {
                for (int i = 1; i <= count; i++)
                {
                    fuelSource = @dynamic.GetFuelSource(i);
                }
            }

            result.DesignPressureDrop = @dynamic.DesignPressureDrop;
            result.DesignTemperatureDiffrence = @dynamic.DesignDeltaT;
            result.Description = dynamic.Description;
            result.Setpoint = boilerPlant.Setpoint?.ToSAM();
            result.Efficiency = boilerPlant?.Efficiency?.ToSAM();
            result.Duty = boilerPlant.Duty?.ToSAM();
            result.DesignTemperatureDiffrence = boilerPlant.DesignDeltaT;
            result.Capacity = boilerPlant.Capacity;
            result.DesignPressureDrop = boilerPlant.DesignPressureDrop;
            result.AncillaryLoad = boilerPlant.AncillaryLoad?.ToSAM();
            result.LossesInSizing = boilerPlant.LossesInSizing == 1;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemBoiler displaySystemBoiler = Systems.Create.DisplayObject<DisplaySystemBoiler>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemBoiler != null)
            {
                ITransform2D transform2D = ((IPlantComponent)boilerPlant).Transform2D();
                if (transform2D != null)
                {
                    displaySystemBoiler.Transform(transform2D);
                }

                result = displaySystemBoiler;
            }

            return result;
        }
    }
}
