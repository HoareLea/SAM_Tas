using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

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

            List<FuelSource> fuelSources = Query.FuelSources(boilerPlant as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0].Name);
                if(fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[1].Name);
                }
            }


            result.DesignPressureDrop = @dynamic.DesignPressureDrop;
            result.Description = dynamic.Description;
            result.Setpoint = ((ProfileData)dynamic.Setpoint)?.ToSAM();
            result.Efficiency = boilerPlant?.Efficiency?.ToSAM();
            result.Duty = ((SizedVariable)dynamic.Duty)?.ToSAM();
            result.DesignTemperatureDifference = dynamic.DesignDeltaT;
            result.Capacity = dynamic.Capacity;
            result.DesignPressureDrop = dynamic.DesignPressureDrop;
            result.AncillaryLoad = dynamic.AncillaryLoad?.ToSAM();
            result.LossesInSizing = dynamic.LossesInSizing;

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
