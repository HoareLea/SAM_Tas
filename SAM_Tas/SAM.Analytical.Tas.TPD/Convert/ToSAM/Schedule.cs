using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ISchedule ToSAM(this PlantSchedule plantSchedule)
        {
            if (plantSchedule == null)
            {
                return null;
            }

            ISchedule result = null;
            switch (plantSchedule.Type)
            {
                case tpdScheduleType.tpdScheduleYearly:
                    YearlySchedule yearlySchedule = new YearlySchedule(plantSchedule.Name);

                    result = yearlySchedule;
                    break;

                case tpdScheduleType.tpdScheduleFunction:
                    FunctionSchedule functionSchedule = new FunctionSchedule(plantSchedule.Name);
                    int functionLoads = plantSchedule.FunctionLoads;

                    if (functionLoads == 4 + 8 + 1024)
                    {
                        functionSchedule.Heating = true;
                        functionSchedule.Cooling = true;
                        functionSchedule.OccupancySensible = true;
                    }
                    else if(functionLoads == 4 + 1024)
                    {
                        functionSchedule.Heating = true;
                        functionSchedule.Cooling = false;
                        functionSchedule.OccupancySensible = true;
                    }
                    else if (functionLoads == 8 + 1024)
                    {
                        functionSchedule.Heating = false;
                        functionSchedule.Cooling = true;
                        functionSchedule.OccupancySensible = true;
                    }
                    else if (functionLoads == 4 + 8)
                    {
                        functionSchedule.Heating = true;
                        functionSchedule.Cooling = true;
                        functionSchedule.OccupancySensible = false;
                    }
                    else if (functionLoads == 4)
                    {
                        functionSchedule.Heating = true;
                        functionSchedule.Cooling = false;
                        functionSchedule.OccupancySensible = false;
                    }
                    else if (functionLoads == 8)
                    {
                        functionSchedule.Heating = false;
                        functionSchedule.Cooling = true;
                        functionSchedule.OccupancySensible = false;
                    }
                    else if (functionLoads == 1024)
                    {
                        functionSchedule.Heating = false;
                        functionSchedule.Cooling = false;
                        functionSchedule.OccupancySensible = true;
                    }

                    functionSchedule.ScheduleFunctionType = plantSchedule.FunctionType.ToSAM();

                    result = functionSchedule;
                    break;

                case tpdScheduleType.tpdScheduleHourly:
                    DailySchedule dailySchedule = new DailySchedule(plantSchedule.Name);

                    result = dailySchedule;
                    break;

            }

            if (result == null)
            {
                return null;
            }

            result.Description = plantSchedule.Description;

            return result;
        }
    }
}