using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMultiChiller ToSAM(this MultiChiller multiChiller)
        {
            if (multiChiller == null)
            {
                return null;
            }

            dynamic @dynamic = multiChiller;

            SystemMultiChiller result = new SystemMultiChiller(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.DesignPressureDrop = @dynamic.DesignPressureDrop;
            result.DesignTemperatureDifference = @dynamic.DesignDeltaT;
            result.Duty = ((SizedVariable)@dynamic.Duty)?.ToSAM();
            result.Setpoint = ((ProfileData)@dynamic.Setpoint)?.ToSAM();
            result.Capacity = @dynamic.Capacity;
            result.Sequence = ((tpdBoilerSequence)@dynamic.Sequence).ToSAM();

            List<FuelSource> fuelSources = Query.FuelSources(multiChiller as PlantComponent);

            for (int i = 1; i <= multiChiller.Multiplicity; i++)
            {
                SystemMultiChillerItem systemMultiChillerItem = new SystemMultiChillerItem();
                systemMultiChillerItem.Efficiency = multiChiller.GetChillerEfficiency(i)?.ToSAM();
                systemMultiChillerItem.CondenserFanLoad = multiChiller.GetChillerCondenserFanLoad(i)?.ToSAM();
                systemMultiChillerItem.Percentage = multiChiller.GetChillerPercentage(i);
                systemMultiChillerItem.Threshold = multiChiller.GetChillerThreshold(i);
                systemMultiChillerItem.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[(i - 1) * 2]?.Name);
                systemMultiChillerItem.SetValue(Core.Systems.SystemObjectParameter.FanEnergySourceName, fuelSources[((i - 1) * 2) + 1]?.Name);

                result.Add(systemMultiChillerItem);
            }

            result.ScheduleName = ((dynamic)multiChiller )?.GetSchedule()?.Name;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemMultiChiller displaySystemMultiChiller = Systems.Create.DisplayObject<DisplaySystemMultiChiller>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemMultiChiller != null)
            {
                ITransform2D transform2D = ((IPlantComponent)multiChiller).Transform2D();
                if (transform2D != null)
                {
                    displaySystemMultiChiller.Transform(transform2D);
                }

                result = displaySystemMultiChiller;
            }

            return result;
        }
    }
}
