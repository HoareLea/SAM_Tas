﻿using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemLiquidExchanger ToSAM(this HeatExchanger heatExchanger)
        {
            if (heatExchanger == null)
            {
                return null;
            }

            dynamic @dynamic = heatExchanger;

            SystemLiquidExchanger result = new SystemLiquidExchanger(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.DesignPressureDrop1 = @dynamic.DesignPressureDrop1;
            result.DesignPressureDrop2 = @dynamic.DesignPressureDrop2;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemLiquidExchanger displaySystemLiquidExchanger = Systems.Create.DisplayObject<DisplaySystemLiquidExchanger>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemLiquidExchanger != null)
            {
                ITransform2D transform2D = ((IPlantComponent)heatExchanger).Transform2D();
                if (transform2D != null)
                {
                    displaySystemLiquidExchanger.Transform(transform2D);
                }

                result = displaySystemLiquidExchanger;
            }

            return result;
        }
    }
}

