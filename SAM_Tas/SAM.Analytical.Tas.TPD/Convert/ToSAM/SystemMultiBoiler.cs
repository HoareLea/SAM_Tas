using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMultiBoiler ToSAM(this MultiBoiler multiBoiler)
        {
            if (multiBoiler == null)
            {
                return null;
            }

            dynamic @dynamic = multiBoiler;

            SystemMultiBoiler result = new SystemMultiBoiler(@dynamic.Name)
            {
                Description = dynamic.Description,
                Setpoint = ((ProfileData)dynamic.Setpoint)?.ToSAM(),
                Duty = ((SizedVariable)dynamic.Duty)?.ToSAM(),
                Capacity = dynamic.Capacity,
                DesignPressureDrop = dynamic.DesignPressureDrop,
                DesignTemperatureDifference = multiBoiler.DesignDeltaT,
                LossesInSizing = dynamic.LossesInSizing,
                Sequence = ((tpdBoilerSequence)dynamic.Sequence).ToSAM()
            };

            tpdMultiBoilerFlags tpdMultiBoilerFlags = (tpdMultiBoilerFlags)multiBoiler.Flags;
            result.IsDomesticHotWater = tpdMultiBoilerFlags.HasFlag(tpdMultiBoilerFlags.tpdMultiBoilerIsDHW);

            Modify.SetReference(result, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(multiBoiler as PlantComponent);

            for (int i = 1; i <= multiBoiler.Multiplicity; i++)
            {
                SystemMultiBoilerItem systemMultiBoilerItem = new SystemMultiBoilerItem();
                systemMultiBoilerItem.Efficiency = multiBoiler.GetBoilerEfficiency(i)?.ToSAM();
                systemMultiBoilerItem.Percentage = multiBoiler.GetBoilerPercentage(i);
                systemMultiBoilerItem.Threshold = multiBoiler.GetBoilerThreshold(i);
                systemMultiBoilerItem.AncillaryLoad = multiBoiler.GetBoilerAncillaryLoad(i)?.ToSAM();
                systemMultiBoilerItem.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[(i - 1) * 2]?.Name);
                systemMultiBoilerItem.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[((i - 1) * 2) + 1]?.Name);
                result.Add(systemMultiBoilerItem);
            }

            result.ScheduleName = ((dynamic)multiBoiler )?.GetSchedule()?.Name;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemMultiBoiler displaySystemMultiBoiler = Systems.Create.DisplayObject<DisplaySystemMultiBoiler>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemMultiBoiler != null)
            {
                ITransform2D transform2D = ((IPlantComponent)multiBoiler).Transform2D();
                if (transform2D != null)
                {
                    displaySystemMultiBoiler.Transform(transform2D);
                }

                result = displaySystemMultiBoiler;
            }

            return result;
        }
    }
}
