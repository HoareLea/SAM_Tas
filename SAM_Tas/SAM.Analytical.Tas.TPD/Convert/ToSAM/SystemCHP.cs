﻿using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCHP ToSAM(this CHP cHP)
        {
            if (cHP == null)
            {
                return null;
            }

            dynamic @dynamic = cHP;

            SystemCHP result = new SystemCHP(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.Setpoint = dynamic.Setpoint?.ToSAM();
            result.Efficiency = dynamic.Efficiency?.ToSAM();
            result.HeatPowerRatio = dynamic.HeatPowerRatio?.ToSAM();
            result.Duty = dynamic.Duty?.ToSAM();
            result.DesignTemperatureDiffrence = dynamic.DesignDeltaT;
            result.Capacity = dynamic.Capacity;
            result.DesignPressureDrop = dynamic.DesignPressureDrop;
            result.LossesInSizing = dynamic.LossesInSizing == 1;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemCHP displaySystemCHP = Systems.Create.DisplayObject<DisplaySystemCHP>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemCHP != null)
            {
                ITransform2D transform2D = ((IPlantComponent)cHP).Transform2D();
                if (transform2D != null)
                {
                    displaySystemCHP.Transform(transform2D);
                }

                result = displaySystemCHP;
            }

            return result;
        }
    }
}