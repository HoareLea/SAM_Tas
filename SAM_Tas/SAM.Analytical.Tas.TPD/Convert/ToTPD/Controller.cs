using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
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
            result.Description = ((dynamic)displaySystemController).Description;

            if(displaySystemController is SystemNormalController)
            {
                result.SensorType = ((SystemNormalController)displaySystemController).NormalControllerDataType.ToTPD();
            }

            if (displaySystemController is SystemSetpointController)
            {
                ISetpoint setpoint = ((SystemSetpointController)displaySystemController).Setpoint;
                if(setpoint is ProfileSetpoint)
                {
                    ProfileSetpoint profileSetpoint = (ProfileSetpoint)setpoint;

                    ControllerProfileData controllerProfileData = result.GetProfile();
                    controllerProfileData.Clear();

                    List<Point2D> point2Ds = profileSetpoint.Point2Ds;
                    if(point2Ds != null)
                    {
                        foreach (Point2D point2D in point2Ds)
                        {
                            controllerProfileData.AddPoint(point2D.X, point2D.Y);
                        }

                        if(point2Ds.Count == 2)
                        {
                            result.Gradient = point2Ds[1].Y > point2Ds[0].Y ? 1 : -1;
                            if(result.Gradient > 0)
                            {
                                result.Setpoint = point2Ds[1].X;
                                result.Band = (point2Ds[0].X - point2Ds[1].X) * (-result.Gradient);
                            }
                            else
                            {
                                result.Setpoint = point2Ds[0].X;
                                result.Band = (point2Ds[1].X - point2Ds[0].X) * (-result.Gradient);
                            }

                            result.Max = System.Math.Max(point2Ds[0].Y, point2Ds[1].Y);
                            result.Min = System.Math.Min(point2Ds[0].Y, point2Ds[1].Y);
                        }
                    }
                }
                else if(setpoint is RangeSetpoint)
                {
                    RangeSetpoint rangeSetpoint = (RangeSetpoint)setpoint;

                    throw new System.NotImplementedException();

                    //if(rangeSetpoint.OutputRange != null)
                    //{
                    //    result.Min = rangeSetpoint.OutputRange.Min;
                    //    result.Max = rangeSetpoint.OutputRange.Max;
                    //}

                    //if (rangeSetpoint.InputRange != null)
                    //{
                    //    switch (rangeSetpoint.InputGradient)
                    //    {
                    //        case Gradient.Positive:
                    //            result.Setpoint = rangeSetpoint.InputRange.Min;
                    //            result.Gradient = 1;
                    //            result.Band = rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min;
                    //            break;

                    //        case Gradient.Negative:
                    //            result.Setpoint = rangeSetpoint.InputRange.Max;
                    //            result.Gradient = -1;
                    //            result.Band = rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min;
                    //            break;
                    //    }
                    //}
                }

                ISetback setback = ((SystemSetpointController)displaySystemController).Setback;
                if(setback != null)
                {
                    result.SetSchedule(setback.ScheduleName);

                    if(setback is SetpointSetback)
                    {
                        SetpointSetback setpointSetback = (SetpointSetback)setback;

                        setpoint = setpointSetback.Setpoint;

                        if (setpoint is ProfileSetpoint)
                        {
                            ProfileSetpoint profileSetpoint = (ProfileSetpoint)setpoint;

                            ControllerProfileData controllerProfileData = result.GetSetbackProfile();
                            controllerProfileData.Clear();

                            List<Point2D> point2Ds = profileSetpoint.Point2Ds;
                            if (point2Ds != null)
                            {
                                foreach (Point2D point2D in point2Ds)
                                {
                                    controllerProfileData.AddPoint(point2D.X, point2D.Y);
                                }

                                if (point2Ds.Count == 2)
                                {
                                    result.Gradient = point2Ds[1].Y > point2Ds[0].Y ? 1 : -1;
                                    if (result.Gradient > 0)
                                    {
                                        result.Setpoint = point2Ds[1].X;
                                        result.Band = (point2Ds[0].X - point2Ds[1].X) * (-result.Gradient);
                                    }
                                    else
                                    {
                                        result.Setpoint = point2Ds[0].X;
                                        result.Band = (point2Ds[1].X - point2Ds[0].X) * (-result.Gradient);
                                    }

                                    result.Max = System.Math.Max(point2Ds[0].Y, point2Ds[1].Y);
                                    result.Min = System.Math.Min(point2Ds[0].Y, point2Ds[1].Y);
                                }
                            }
                        }
                        else if (setpoint is RangeSetpoint)
                        {
                            RangeSetpoint rangeSetpoint = (RangeSetpoint)setpoint;

                            throw new System.NotImplementedException();

                            //if (rangeSetpoint.OutputRange != null)
                            //{
                            //    result.SetbackMin = rangeSetpoint.OutputRange.Min;
                            //    result.SetbackMax = rangeSetpoint.OutputRange.Max;
                            //}

                            //if (rangeSetpoint.InputRange != null)
                            //{
                            //    switch (rangeSetpoint.InputGradient)
                            //    {
                            //        case Gradient.Positive:
                            //            result.SetbackSetpoint = rangeSetpoint.InputRange.Min;
                            //            result.SetbackGradient = 1;
                            //            result.SetbackBand = rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min;
                            //            break;

                            //        case Gradient.Negative:
                            //            result.SetbackSetpoint = rangeSetpoint.InputRange.Max;
                            //            result.SetbackGradient = -1;
                            //            result.SetbackBand = rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min;
                            //            break;
                            //    }
                            //}
                        }
                    }
                }
            }

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
