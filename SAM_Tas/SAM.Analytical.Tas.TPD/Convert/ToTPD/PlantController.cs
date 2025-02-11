using SAM.Analytical.Systems;
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
