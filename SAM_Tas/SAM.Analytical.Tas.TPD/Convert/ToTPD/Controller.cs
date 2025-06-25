using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Geometry.Planar;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Controller ToTPD(this IDisplaySystemController displaySystemController, global::TPD.System system, Controller controller = null)
        {
            if (displaySystemController == null || system == null)
            {
                return null;
            }

            Controller result = controller;
            if(result == null)
            {
                result = system.AddController();
            }

            result.Description = ((dynamic)displaySystemController).Description;

            if(displaySystemController is SystemNormalController)
            {
                result.SensorType = ((SystemNormalController)displaySystemController).NormalControllerDataType.ToTPD();
                result.SensorPresetType = ((SystemNormalController)displaySystemController).NormalControllerLimit.ToTPD();
            }

            if (displaySystemController is SystemSetpointController)
            {
                ISetpoint setpoint = ((SystemSetpointController)displaySystemController).Setpoint;
                if (setpoint is ProfileSetpoint)
                {
                    ProfileSetpoint profileSetpoint = (ProfileSetpoint)setpoint;

                    ControllerProfileData controllerProfileData = result.GetProfile();
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

                    //throw new System.NotImplementedException();

                    if (rangeSetpoint.OutputRange != null)
                    {
                        result.Min = rangeSetpoint.OutputRange.Min;
                        result.Max = rangeSetpoint.OutputRange.Max;
                    }

                    if (result.SensorType == tpdSensorType.tpdMinFlowSensor)
                    {
                        result.Band = (rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min) / rangeSetpoint.InputRange.Max * 100;
                    }
                    else
                    {
                        result.Band = rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min;
                    }

                    if (rangeSetpoint.InputRange != null)
                    {
                        switch (rangeSetpoint.OutputGradient)
                        {
                            case Gradient.Positive:
                                result.Setpoint = rangeSetpoint.InputRange.Max;
                                result.Gradient = 1;
                                break;

                            case Gradient.Negative:
                                result.Setpoint = rangeSetpoint.InputRange.Min;
                                result.Gradient = -1;
                                break;
                        }
                    }
                }

                ISetback setback = ((SystemSetpointController)displaySystemController).Setback;
                if (setback != null)
                {
                    result.SetSchedule(setback.ScheduleName);

                    if (setback is SetpointSetback)
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
                                    result.SetbackGradient = point2Ds[1].Y > point2Ds[0].Y ? 1 : -1;
                                    if (result.SetbackGradient > 0)
                                    {
                                        result.SetbackSetpoint = point2Ds[1].X;
                                        result.SetbackBand = (point2Ds[0].X - point2Ds[1].X) * (-result.SetbackGradient);
                                    }
                                    else
                                    {
                                        result.SetbackSetpoint = point2Ds[0].X;
                                        result.SetbackBand = (point2Ds[1].X - point2Ds[0].X) * (-result.SetbackGradient);
                                    }

                                    result.SetbackMax = System.Math.Max(point2Ds[0].Y, point2Ds[1].Y);
                                    result.SetbackMin = System.Math.Min(point2Ds[0].Y, point2Ds[1].Y);
                                }
                            }
                        }
                        else if (setpoint is RangeSetpoint)
                        {
                            RangeSetpoint rangeSetpoint = (RangeSetpoint)setpoint;

                            if (rangeSetpoint.OutputRange != null)
                            {
                                result.SetbackMin = rangeSetpoint.OutputRange.Min;
                                result.SetbackMax = rangeSetpoint.OutputRange.Max;
                            }

                            if (result.SensorType == tpdSensorType.tpdMinFlowSensor)
                            {
                                if (rangeSetpoint.OutputGradient == Gradient.Positive)
                                {
                                    result.SetbackBand = (rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min) / rangeSetpoint.InputRange.Max * 100;
                                }
                                else
                                {
                                    result.SetbackBand = (rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min) / rangeSetpoint.InputRange.Min * 100;
                                }
                            }
                            else
                            {
                                result.SetbackBand = rangeSetpoint.InputRange.Max - rangeSetpoint.InputRange.Min;
                            }

                            if (rangeSetpoint.InputRange != null)
                            {
                                switch (rangeSetpoint.OutputGradient)
                                {
                                    case Gradient.Positive:
                                        result.SetbackSetpoint = rangeSetpoint.InputRange.Max;
                                        result.SetbackGradient = 1;
                                        break;

                                    case Gradient.Negative:
                                        result.SetbackSetpoint = rangeSetpoint.InputRange.Min;
                                        result.SetbackGradient = -1;
                                        break;
                                }
                            }
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

            if (controller == null)
            {
                displaySystemController.SetLocation(result);
            }

            if (displaySystemController is SAMObject)
            {
                dynamic @dynamic = (dynamic)result;

                SAMObject sAMObject = (SAMObject)displaySystemController;

                bool value = false;

                if (sAMObject.TryGetValue(SystemControllerParameter.LUAEnabled, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagLua;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagLua;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.DampenValue, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagDampenValue;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagDampenValue;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.DampenSignal, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagDampenSignal;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagDampenSignal;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.LuaHidden, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagLuaHidden;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagLuaHidden;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasControlType, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasControlType;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasControlType;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasControllerType, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasControllerType;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasControllerType;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSensorType, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSensorType;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSensorType;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSensorPresetType, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSensorPresetType;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSensorPresetType;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSensorArc1, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSensorArc1;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSensorArc1;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSensorArc2, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSensorArc2;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSensorArc2;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasProfile, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasProfile;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasProfile;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSetbackProfile, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSetbackProfile;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSetbackProfile;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSchedule, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSchedule;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSchedule;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSetpoint, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSetpoint;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSetpoint;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasBand, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasBand;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasBand;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasGradient, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasGradient;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasGradient;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasMin, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasMin;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasMin;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasMax, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasMax;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasMax;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSetbackSetpoint, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSetbackSetpoint;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSetbackSetpoint;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSetbackBand, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSetbackBand;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSetbackBand;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSetbackGradient, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSetbackGradient;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSetbackGradient;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSetbackMin, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSetbackMin;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSetbackMin;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.HasSetbackMax, out value) && value)
                {
                    @dynamic.Flags |= (int)tpdControllerFlags.tpdControllerFlagHasSetbackMax;
                }
                else
                {
                    @dynamic.Flags &= ~(int)tpdControllerFlags.tpdControllerFlagHasSetbackMax;
                }

                if (sAMObject.TryGetValue(SystemControllerParameter.LUACode, out string lUACode))
                {
                    @dynamic.Code = lUACode;
                }
            }

            return result;
        }
    }
}
