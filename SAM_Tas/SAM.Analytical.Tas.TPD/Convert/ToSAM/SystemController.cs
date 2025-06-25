using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Core;
using System.Collections.Generic;
using SAM.Core.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ISystemController ToSAM(this Controller controller)
        {
            if (controller == null)
            {
                return null;
            }

            dynamic @dynamic = controller;

            string sensorReference = controller.SensorArc1?.Reference();
            string secondarySensorReference = controller.SensorArc2?.Reference();

            tpdSensorType tpdSensorType = (tpdSensorType)@dynamic.SensorType;

            NormalControllerDataType normalControllerDataType;
            try
            {
                normalControllerDataType = tpdSensorType.ToSAM_NormalControllerDataType();
            }
            catch
            {
                return null;
            }

            NormalControllerLimit normalControllerLimit = (controller.SensorPresetType).ToSAM();


            ISetpoint setpoint = null;

            if(normalControllerDataType != NormalControllerDataType.MinFlow &&
                normalControllerDataType != NormalControllerDataType.ThermostatTemperature &&
                normalControllerDataType != NormalControllerDataType.HumidistatRelativeHumidity)
            {
                ControllerProfileData controllerProfileData_Setpoint = controller.GetProfile();

                List<ControllerProfilePoint> controllerProfilePoints_Setpoint = controllerProfileData_Setpoint?.ControllerProfilePoints();
                if (controllerProfilePoints_Setpoint != null && controllerProfilePoints_Setpoint.Count > 1)
                {
                    ProfileSetpoint profileSetpoint = new ProfileSetpoint();
                    foreach (ControllerProfilePoint controllerProfilePoint in controllerProfilePoints_Setpoint)
                    {
                        profileSetpoint.Add(controllerProfilePoint.x, controllerProfilePoint.y);
                    }

                    setpoint = profileSetpoint;
                }
            }

            if(setpoint == null)
            {
                RangeSetpoint rangeSetpoint = new RangeSetpoint();
                if(controller.Gradient < 0)
                {
                    rangeSetpoint.OutputGradient = Gradient.Negative;
                }

                if(normalControllerDataType == NormalControllerDataType.MinFlow)
                {
                    rangeSetpoint.InputRange = new Range<double>(controller.Setpoint, controller.Setpoint - (controller.Gradient * (controller.Band / 100) * controller.Setpoint));
                }
                else
                {
                    rangeSetpoint.InputRange = new Range<double>(controller.Setpoint, controller.Setpoint - (controller.Gradient * controller.Band));
                }

                rangeSetpoint.OutputRange = new Range<double>(controller.Min, controller.Max);
                setpoint = rangeSetpoint;
            }



            string scheduleName = controller.GetSchedule()?.Name;

            ISetback setback = null;

            if (normalControllerDataType != NormalControllerDataType.MinFlow &&
                normalControllerDataType != NormalControllerDataType.ThermostatTemperature &&
                normalControllerDataType != NormalControllerDataType.HumidistatRelativeHumidity)
            {
                ControllerProfileData controllerProfileData_Setback = controller.GetSetbackProfile();

                List<ControllerProfilePoint> controllerProfilePoints_Setback = controllerProfileData_Setback?.ControllerProfilePoints();
                if (controllerProfilePoints_Setback != null && controllerProfilePoints_Setback.Count > 1)
                {
                    ProfileSetpoint profileSetpoint = new ProfileSetpoint();
                    foreach (ControllerProfilePoint controllerProfilePoint in controllerProfilePoints_Setback)
                    {
                        profileSetpoint.Add(controllerProfilePoint.x, controllerProfilePoint.y);
                    }

                    setback = new SetpointSetback(scheduleName, profileSetpoint);
                }
            }

            if (setback == null)
            {
                RangeSetpoint rangeSetpoint = new RangeSetpoint();

                if (normalControllerDataType == NormalControllerDataType.MinFlow)
                {
                    rangeSetpoint.InputRange = new Range<double>(controller.SetbackSetpoint, controller.SetbackSetpoint - (controller.SetbackGradient * (controller.SetbackBand / 100) * controller.SetbackSetpoint));
                }
                else
                {
                    rangeSetpoint.InputRange = new Range<double>(controller.SetbackSetpoint, controller.SetbackSetpoint - (controller.SetbackGradient * controller.SetbackBand));
                }

                rangeSetpoint.OutputRange = new Range<double>(controller.SetbackMin, controller.SetbackMax);
                rangeSetpoint.OutputGradient = controller.SetbackGradient > 0 ? Gradient.Positive : Gradient.Negative;
                setback = new SetpointSetback(scheduleName, rangeSetpoint);
            }

            SystemController systemController = null;

            switch(controller.ControlType)
            {
                case tpdControlType.tpdControlNormal:
                    systemController = new SystemNormalController(@dynamic.Name, normalControllerDataType, setpoint, setback, normalControllerLimit) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlOutdoor:
                    OutdoorControllerDataType outdoorControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM_OutdoorControllerDataType();
                    systemController = new SystemOutdoorController(@dynamic.Name, outdoorControllerDataType, setpoint, setback) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlDifference:
                    systemController = new SystemDifferenceController(@dynamic.Name, sensorReference, secondarySensorReference, normalControllerDataType, setpoint, setback, normalControllerLimit);
                    break;

                case tpdControlType.tpdControlPassThrough:
                    systemController = new SystemPassthroughController(@dynamic.Name, sensorReference, setpoint, setback, normalControllerDataType);
                    break;

                case tpdControlType.tpdControlNot:
                    systemController = new SystemNotLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlMin:
                    systemController = new SystemMinLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlMax:
                    systemController = new SystemMaxLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlSig:
                    systemController = new SystemSigLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlIf:
                    systemController = new SystemIfLogicalController(@dynamic.Name);
                    break;

                //case tpdControlType.tpdControlGroup:
                //    systemController = new SystemNormalController(@dynamic.Name);
                //    break;
            }

            if(systemController == null)
            {
                return null;
            }

            IReference reference = Create.Reference(controller);
            if (reference != null)
            {
                Modify.SetReference(systemController, reference.ToString());
            }

            systemController.Description = dynamic.Description;
            //Modify.SetReference(systemController, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            List<PlantDayType> plantDayTypes = controller.PlantDayTypes();
            if(plantDayTypes != null && plantDayTypes.Count != 0)
            {
                systemController.DayTypeNames = new HashSet<string>(plantDayTypes.ConvertAll(x => x.Name));
            }

            ISystemController result = systemController;

            IDisplaySystemController displaySystemController = Systems.Create.DisplayObject<IDisplaySystemController>(systemController, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemController != null)
            {
                result = displaySystemController;
            }

            if (result is SAMObject)
            {
                SAMObject sAMObject = (SAMObject)result;

                string lUACode = (string)@dynamic.Code;
                if (!string.IsNullOrWhiteSpace(lUACode))
                {
                    sAMObject.SetValue(SystemControllerParameter.LUACode, lUACode);
                }

                tpdControllerFlags tpdControllerFlags = (tpdControllerFlags)@dynamic.Flags;

                sAMObject.SetValue(SystemControllerParameter.LUAEnabled, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagLua));
                sAMObject.SetValue(SystemControllerParameter.DampenValue, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagDampenValue));
                sAMObject.SetValue(SystemControllerParameter.DampenSignal, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagDampenSignal));
                sAMObject.SetValue(SystemControllerParameter.LuaHidden, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagLuaHidden));
                sAMObject.SetValue(SystemControllerParameter.HasControlType, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasControlType));
                sAMObject.SetValue(SystemControllerParameter.HasControllerType, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasControllerType));
                sAMObject.SetValue(SystemControllerParameter.HasSensorType, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSensorType));
                sAMObject.SetValue(SystemControllerParameter.HasSensorPresetType, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSensorPresetType));
                sAMObject.SetValue(SystemControllerParameter.HasSensorArc1, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSensorArc1));
                sAMObject.SetValue(SystemControllerParameter.HasSensorArc2, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSensorArc2));
                sAMObject.SetValue(SystemControllerParameter.HasProfile, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasProfile));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackProfile, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackProfile));
                sAMObject.SetValue(SystemControllerParameter.HasSchedule, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSchedule));
                sAMObject.SetValue(SystemControllerParameter.HasSetpoint, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetpoint));
                sAMObject.SetValue(SystemControllerParameter.HasBand, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasBand));
                sAMObject.SetValue(SystemControllerParameter.HasGradient, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasGradient));
                sAMObject.SetValue(SystemControllerParameter.HasMin, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasMin));
                sAMObject.SetValue(SystemControllerParameter.HasMax, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasMax));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackSetpoint, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackSetpoint));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackBand, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackBand));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackGradient, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackGradient));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackMin, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackMin));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackMax, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackMax));
            }

            return result;
        }

        public static ISystemController ToSAM(this PlantController plantController)
        {
            if (plantController == null)
            {
                return null;
            }

            dynamic @dynamic = plantController;

            string sensorReference = plantController.SensorArc1?.Reference();
            string secondarySensorReference = plantController.SensorArc2?.Reference();

            tpdSensorType tpdSensorType = (tpdSensorType)@dynamic.SensorType;

            NormalControllerDataType normalControllerDataType;
            try
            {
                normalControllerDataType = tpdSensorType.ToSAM_NormalControllerDataType();
            }
            catch
            {
                return null;
            }

            NormalControllerLimit normalControllerLimit = (plantController.SensorPresetType).ToSAM();


            ISetpoint setpoint = null;

            if (normalControllerDataType != NormalControllerDataType.MinFlow &&
                normalControllerDataType != NormalControllerDataType.ThermostatTemperature &&
                normalControllerDataType != NormalControllerDataType.HumidistatRelativeHumidity)
            {
                ControllerProfileData controllerProfileData_Setpoint = plantController.GetProfile();

                List<ControllerProfilePoint> controllerProfilePoints_Setpoint = controllerProfileData_Setpoint?.ControllerProfilePoints();
                if (controllerProfilePoints_Setpoint != null && controllerProfilePoints_Setpoint.Count > 1)
                {
                    ProfileSetpoint profileSetpoint = new ProfileSetpoint();
                    foreach (ControllerProfilePoint controllerProfilePoint in controllerProfilePoints_Setpoint)
                    {
                        profileSetpoint.Add(controllerProfilePoint.x, controllerProfilePoint.y);
                    }

                    setpoint = profileSetpoint;
                }
            }

            if (setpoint == null)
            {
                RangeSetpoint rangeSetpoint = new RangeSetpoint();
                if (plantController.Gradient < 0)
                {
                    rangeSetpoint.OutputGradient = Gradient.Negative;
                }

                if (normalControllerDataType == NormalControllerDataType.MinFlow)
                {
                    rangeSetpoint.InputRange = new Range<double>(plantController.Setpoint, plantController.Setpoint - (plantController.Gradient * (plantController.Band / 100) * plantController.Setpoint));
                }
                else
                {
                    rangeSetpoint.InputRange = new Range<double>(plantController.Setpoint, plantController.Setpoint - (plantController.Gradient * plantController.Band));
                }

                rangeSetpoint.OutputRange = new Range<double>(plantController.Min, plantController.Max);
                setpoint = rangeSetpoint;
            }



            string scheduleName = plantController.GetSchedule()?.Name;

            ISetback setback = null;

            if (normalControllerDataType != NormalControllerDataType.MinFlow &&
                normalControllerDataType != NormalControllerDataType.ThermostatTemperature &&
                normalControllerDataType != NormalControllerDataType.HumidistatRelativeHumidity)
            {
                ControllerProfileData controllerProfileData_Setback = plantController.GetSetbackProfile();

                List<ControllerProfilePoint> controllerProfilePoints_Setback = controllerProfileData_Setback?.ControllerProfilePoints();
                if (controllerProfilePoints_Setback != null && controllerProfilePoints_Setback.Count > 1)
                {
                    ProfileSetpoint profileSetpoint = new ProfileSetpoint();
                    foreach (ControllerProfilePoint controllerProfilePoint in controllerProfilePoints_Setback)
                    {
                        profileSetpoint.Add(controllerProfilePoint.x, controllerProfilePoint.y);
                    }

                    setback = new SetpointSetback(scheduleName, profileSetpoint);
                }
            }

            if (setback == null)
            {
                RangeSetpoint rangeSetpoint = new RangeSetpoint();

                if (normalControllerDataType == NormalControllerDataType.MinFlow)
                {
                    rangeSetpoint.InputRange = new Range<double>(plantController.SetbackSetpoint, plantController.SetbackSetpoint - (plantController.SetbackGradient * (plantController.SetbackBand / 100) * plantController.SetbackSetpoint));
                }
                else
                {
                    rangeSetpoint.InputRange = new Range<double>(plantController.SetbackSetpoint, plantController.SetbackSetpoint - (plantController.SetbackGradient * plantController.SetbackBand));
                }

                rangeSetpoint.OutputRange = new Range<double>(plantController.SetbackMin, plantController.SetbackMax);
                rangeSetpoint.OutputGradient = plantController.SetbackGradient > 0 ? Gradient.Positive : Gradient.Negative;
                setback = new SetpointSetback(scheduleName, rangeSetpoint);
            }

            LiquidNormalControllerDataType liquidNormalControllerDataType = LiquidNormalControllerDataType.Flow;
            switch(normalControllerDataType)
            {
                case NormalControllerDataType.Flow:
                    liquidNormalControllerDataType = LiquidNormalControllerDataType.Flow;
                    break;

                case NormalControllerDataType.Pressure:
                    liquidNormalControllerDataType = LiquidNormalControllerDataType.Pressure;
                    break;

                case NormalControllerDataType.Load:
                    liquidNormalControllerDataType = LiquidNormalControllerDataType.Load;
                    break;

                case NormalControllerDataType.DryBulbTemperature:
                    liquidNormalControllerDataType = LiquidNormalControllerDataType.Temperature;
                    break;
            }

            SystemController systemController = null;

            switch (plantController.ControlType)
            {
                case tpdControlType.tpdControlNormal:
                    systemController = new SystemLiquidNormalController(@dynamic.Name, sensorReference, liquidNormalControllerDataType, setpoint, setback);
                    break;

                case tpdControlType.tpdControlOutdoor:
                    OutdoorControllerDataType outdoorControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM_OutdoorControllerDataType();
                    systemController = new SystemOutdoorController(@dynamic.Name, outdoorControllerDataType, setpoint, setback) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlDifference:
                    systemController = new SystemLiquidDifferenceController(@dynamic.Name, sensorReference, secondarySensorReference, liquidNormalControllerDataType, setpoint, setback);
                    break;

                case tpdControlType.tpdControlPassThrough:
                    systemController = new SystemLiquidPassthroughController(@dynamic.Name, liquidNormalControllerDataType) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlNot:
                    systemController = new SystemNotLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlMin:
                    systemController = new SystemMinLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlMax:
                    systemController = new SystemMaxLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlSig:
                    systemController = new SystemSigLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlIf:
                    systemController = new SystemIfLogicalController(@dynamic.Name);
                    break;

                    //case tpdControlType.tpdControlGroup:
                    //    systemController = new SystemNormalController(@dynamic.Name);
                    //    break;
            }

            if (systemController == null)
            {
                return null;
            }

            IReference reference = Create.Reference(plantController);
            if (reference != null)
            {
                Modify.SetReference(systemController, reference.ToString());
            }

            systemController.Description = dynamic.Description;
            //Modify.SetReference(systemController, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            List<PlantDayType> plantDayTypes = plantController.PlantDayTypes();
            if (plantDayTypes != null && plantDayTypes.Count != 0)
            {
                systemController.DayTypeNames = new HashSet<string>(plantDayTypes.ConvertAll(x => x.Name));
            }

            ISystemController result = systemController;

            IDisplaySystemController displaySystemController = Systems.Create.DisplayObject<IDisplaySystemController>(systemController, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemController != null)
            {
                result = displaySystemController;
            }

            if(result is SAMObject)
            {
                SAMObject sAMObject = (SAMObject)result;

                string lUACode= (string)@dynamic.Code;
                if(!string.IsNullOrWhiteSpace(lUACode))
                {
                    sAMObject.SetValue(SystemControllerParameter.LUACode, lUACode);
                }

                tpdControllerFlags tpdControllerFlags = (tpdControllerFlags)@dynamic.Flags;

                sAMObject.SetValue(SystemControllerParameter.LUAEnabled, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagLua));
                sAMObject.SetValue(SystemControllerParameter.DampenValue, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagDampenValue));
                sAMObject.SetValue(SystemControllerParameter.DampenSignal, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagDampenSignal));
                sAMObject.SetValue(SystemControllerParameter.LuaHidden, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagLuaHidden));
                sAMObject.SetValue(SystemControllerParameter.HasControlType, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasControlType));
                sAMObject.SetValue(SystemControllerParameter.HasControllerType, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasControllerType));
                sAMObject.SetValue(SystemControllerParameter.HasSensorType, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSensorType));
                sAMObject.SetValue(SystemControllerParameter.HasSensorPresetType, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSensorPresetType));
                sAMObject.SetValue(SystemControllerParameter.HasSensorArc1, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSensorArc1));
                sAMObject.SetValue(SystemControllerParameter.HasSensorArc2, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSensorArc2));
                sAMObject.SetValue(SystemControllerParameter.HasProfile, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasProfile));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackProfile, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackProfile));
                sAMObject.SetValue(SystemControllerParameter.HasSchedule, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSchedule));
                sAMObject.SetValue(SystemControllerParameter.HasSetpoint, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetpoint));
                sAMObject.SetValue(SystemControllerParameter.HasBand, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasBand));
                sAMObject.SetValue(SystemControllerParameter.HasGradient, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasGradient));
                sAMObject.SetValue(SystemControllerParameter.HasMin, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasMin));
                sAMObject.SetValue(SystemControllerParameter.HasMax, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasMax)); 
                sAMObject.SetValue(SystemControllerParameter.HasSetbackSetpoint, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackSetpoint));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackBand, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackBand));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackGradient, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackGradient));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackMin, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackMin));
                sAMObject.SetValue(SystemControllerParameter.HasSetbackMax, tpdControllerFlags.HasFlag(tpdControllerFlags.tpdControllerFlagHasSetbackMax));
            }

            return result;
        }
    }
}
