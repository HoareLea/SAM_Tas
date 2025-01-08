﻿using TPD;
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

            SystemMultiBoiler result = new SystemMultiBoiler(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.Setpoint = multiBoiler.Setpoint?.ToSAM();
            result.Duty = multiBoiler.Duty?.ToSAM();
            result.DesignTemperatureDifference = multiBoiler.DesignDeltaT;
            result.LossesInSizing = multiBoiler.LossesInSizing == 1;
            result.Sequence = multiBoiler.Sequence.ToSAM();

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
