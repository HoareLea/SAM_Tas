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

            SystemBoiler result = new SystemBoiler(@dynamic.Name)
            {
                DesignPressureDrop = @dynamic.DesignPressureDrop,
                Description = dynamic.Description,
                Setpoint = ((ProfileData)dynamic.Setpoint)?.ToSAM(),
                Efficiency = ((ProfileData)dynamic.Efficiency)?.ToSAM(),
                Duty = ((SizedVariable)dynamic.Duty)?.ToSAM(),
                DesignTemperatureDifference = dynamic.DesignDeltaT,
                Capacity = dynamic.Capacity,
                AncillaryLoad = ((ProfileData)dynamic.AncillaryLoad)?.ToSAM(),
                LossesInSizing = dynamic.LossesInSizing,
            };

            tpdBoilerPlantFlags tpdBoilerPlantFlags = (tpdBoilerPlantFlags)boilerPlant.Flags;
            result.IsDomesticHotWater = tpdBoilerPlantFlags.HasFlag(tpdBoilerPlantFlags.tpdBoilerPlantIsDHW);

            Modify.SetReference(result, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(boilerPlant as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                if(fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[1]?.Name);
                }
            }

            result.ScheduleName = ((dynamic)(boilerPlant))?.GetSchedule()?.Name;

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
