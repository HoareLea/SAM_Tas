﻿using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPump ToSAM(this Pump pump)
        {
            if (pump == null)
            {
                return null;
            }

            dynamic @dynamic = pump;

            SystemPump result = new SystemPump(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(pump as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName, fuelSources[0]?.Name);
            }

            result.Description = dynamic.Description;
            result.OverallEfficiency = pump.OverallEfficiency?.ToSAM();
            result.Pressure = pump.Pressure;
            result.DesignFrowRate = pump.DesignFlowRate;
            result.Capacity = pump.Capacity;
            result.PartLoad = pump.PartLoad?.ToSAM();
            result.FanControlType = pump.ControlType.ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemPump displaySystemPump = Systems.Create.DisplayObject<DisplaySystemPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)pump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemPump.Transform(transform2D);
                }

                result = displaySystemPump;
            }

            return result;
        }
    }
}