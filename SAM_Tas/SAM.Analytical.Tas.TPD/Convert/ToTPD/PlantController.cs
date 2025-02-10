﻿using SAM.Analytical.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PlantController ToTPD(this IDisplaySystemController displaySystemController, PlantRoom plantRoom)
        {
            if (displaySystemController == null || plantRoom == null)
            {
                return null;
            }

            PlantController result = plantRoom.AddController();
            

            if (displaySystemController is SystemOutdoorController)
            {
                result.ControlType = tpdControlType.tpdControlOutdoor;
            }
            else if (displaySystemController is SystemLiquidDifferenceController)
            {
                result.ControlType = tpdControlType.tpdControlDifference;
            }
            else if (displaySystemController is SystemMaxLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlMax;
            }
            else if (displaySystemController is SystemMinLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlMin;
            }
            else if (displaySystemController is SystemNotLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlNot;
            }
            else if (displaySystemController is SystemSigLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlSig;
            }
            else if (displaySystemController is SystemIfLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlIf;
            }
            else if (displaySystemController is SystemLiquidPassthroughController)
            {
                result.ControlType = tpdControlType.tpdControlPassThrough;
            }
            else if (displaySystemController is SystemLiquidNormalController)
            {
                result.ControlType = tpdControlType.tpdControlNormal;
            }

            HashSet<string> dayTypeNames = (displaySystemController as SystemController)?.DayTypeNames;
            if (dayTypeNames != null)
            {
                List<PlantDayType> plantDayTypes = Query.PlantDayTypes(plantRoom.GetEnergyCentre()?.GetCalendar());
                if (plantDayTypes != null)
                {
                    foreach (PlantDayType plantDayType in plantDayTypes)
                    {
                        if (!dayTypeNames.Contains(plantDayType.Name))
                        {
                            continue;
                        }

                        result.AddDayType(plantDayType);
                    }
                }
            }

            displaySystemController.SetLocation(result);

            return result;
        }
    }
}
