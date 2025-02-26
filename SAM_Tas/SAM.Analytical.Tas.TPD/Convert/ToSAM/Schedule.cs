using TPD;
using SAM.Analytical.Systems;
using System.Collections.Generic;

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

                    for (int i = 0; i < 8760; i++)
                    {
                        yearlySchedule[i] = plantSchedule.GetYearlyValue(i + 1);
                    }

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
                    int count = plantSchedule.GetScheduleDayCount();
                    for (int i = 0; i < count; i++)
                    {
                        PlantScheduleDay plantScheduleDay = plantSchedule.GetScheduleDay(i + 1);
                        if(plantScheduleDay == null)
                        {
                            continue;
                        }

                        string name = plantScheduleDay.GetDayType()?.Name;

                        List<double> values = new List<double>();
                        for(int hour = 1; hour <= 24; hour++)
                        {
                            values.Add(plantScheduleDay.GetHourlyValue(hour));
                        }

                        ScheduleDay scheduleDay = new ScheduleDay(name, values);
                        dailySchedule.Add(scheduleDay);
                    }

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