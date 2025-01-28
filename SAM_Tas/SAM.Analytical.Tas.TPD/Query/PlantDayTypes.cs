using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<PlantDayType> PlantDayTypes(this PlantController plantController) 
        { 
            if(plantController == null)
            {
                return null;
            }

            List<PlantDayType> result = new List<PlantDayType>();

            int count = plantController.GetDayTypeCount();
            for (int i = 1; i <= count; i++)
            {
                PlantDayType plantDayType = plantController.GetDayType(i);
                if (plantDayType == null)
                {
                    continue;
                }

                result.Add(plantDayType);
            }

            return result;
        }

        public static List<PlantDayType> PlantDayTypes(this Controller controller)
        {
            if (controller == null)
            {
                return null;
            }

            List<PlantDayType> result = new List<PlantDayType>();

            int count = controller.GetDayTypeCount();
            for (int i = 1; i <= count; i++)
            {
                PlantDayType plantDayType = controller.GetDayType(i);
                if (plantDayType == null)
                {
                    continue;
                }

                result.Add(plantDayType);
            }

            return result;
        }

        public static List<PlantDayType> PlantDayTypes(this PlantCalendar plantCalendar)
        {
            if (plantCalendar == null)
            {
                return null;
            }

            List<PlantDayType> result = new List<PlantDayType>();

            int count = plantCalendar.GetDayTypeCount();
            for (int i = 1; i <= count; i++)
            {
                PlantDayType plantDayType = plantCalendar.GetDayType(i);
                if (plantDayType == null)
                {
                    continue;
                }

                result.Add(plantDayType);
            }

            return result;
        }
    }
}