using SAM.Analytical.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Controller ToTPD(this IDisplaySystemController displaySystemController, global::TPD.System system)
        {
            if (displaySystemController == null || system == null)
            {
                return null;
            }

            Controller result = system.AddController();

            HashSet<string> dayTypeNames = (displaySystemController as SystemController)?.DayTypeNames;
            if(dayTypeNames != null)
            {
                List<PlantDayType> plantDayTypes = Query.PlantDayTypes(system.GetPlantRoom()?.GetEnergyCentre()?.GetCalendar());
                if (plantDayTypes != null)
                {
                    foreach(PlantDayType plantDayType in plantDayTypes)
                    {
                        if(!dayTypeNames.Contains(plantDayType.Name))
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
