using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Core;
using System.Collections.Generic;
using System;

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

            SystemController result = null;

            switch(controller.ControlType)
            {
                case tpdControlType.tpdControlNormal:
                    result = new SystemNormalController(@dynamic.Name, normalControllerDataType, setpoint, setback, normalControllerLimit) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlOutdoor:
                    OutdoorControllerDataType outdoorControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM_OutdoorControllerDataType();
                    result = new SystemOutdoorController(@dynamic.Name, outdoorControllerDataType, setpoint, setback) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlDifference:
                    result = new SystemDifferenceController(@dynamic.Name, sensorReference, secondarySensorReference, normalControllerDataType, setpoint, setback, normalControllerLimit);
                    break;

                case tpdControlType.tpdControlPassThrough:
                    result = new SystemPassthroughController(@dynamic.Name, normalControllerDataType) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlNot:
                    result = new SystemNotLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlMin:
                    result = new SystemMinLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlMax:
                    result = new SystemMaxLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlSig:
                    result = new SystemSigLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlIf:
                    result = new SystemIfLogicalController(@dynamic.Name);
                    break;

                //case tpdControlType.tpdControlGroup:
                //    systemController = new SystemNormalController(@dynamic.Name);
                //    break;
            }

            if(result == null)
            {
                return null;
            }

            IReference reference = Create.Reference(controller);
            if (reference != null)
            {
                Modify.SetReference(result, reference.ToString());
            }

            result.Description = dynamic.Description;
            //Modify.SetReference(systemController, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            List<PlantDayType> plantDayTypes = controller.PlantDayTypes();
            if(plantDayTypes != null && plantDayTypes.Count != 0)
            {
                result.DayTypeNames = new HashSet<string>(plantDayTypes.ConvertAll(x => x.Name));
            }

            IDisplaySystemController displaySystemController = Systems.Create.DisplayObject<IDisplaySystemController>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemController != null)
            {
                return displaySystemController;
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

            ISetpoint setpoint = null;

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

            if (setpoint == null)
            {
                RangeSetpoint rangeSetpoint = new RangeSetpoint();
                rangeSetpoint.InputRange = new Range<double>(plantController.Setpoint - (plantController.Gradient * plantController.Band));
                rangeSetpoint.OutputRange = new Range<double>(plantController.Min, plantController.Max);
                setpoint = rangeSetpoint;
            }

            LiquidNormalControllerDataType liquidNormalControllerDataType;

            try
            {
                liquidNormalControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM_LiquidNormalControllerDataType();
            }
            catch(Exception exception)
            {
                return null;
            }

            string scheduleName = plantController.GetSchedule()?.Name;

            ISetback setback = null;

            ControllerProfileData controllerProfileData_Setback = plantController.GetSetbackProfile();

            List<ControllerProfilePoint> controllerProfilePoints_Setback = controllerProfileData_Setback?.ControllerProfilePoints();
            if (controllerProfilePoints_Setpoint != null && controllerProfilePoints_Setpoint.Count > 1)
            {
                ProfileSetpoint profileSetpoint = new ProfileSetpoint();
                foreach (ControllerProfilePoint controllerProfilePoint in controllerProfilePoints_Setpoint)
                {
                    profileSetpoint.Add(controllerProfilePoint.x, controllerProfilePoint.y);
                }

                setback = new SetpointSetback(scheduleName, profileSetpoint);
            }

            if (setback == null)
            {
                RangeSetpoint rangeSetpoint = new RangeSetpoint();
                rangeSetpoint.InputRange = new Range<double>(plantController.SetbackSetpoint - (plantController.SetbackGradient * plantController.SetbackBand));
                rangeSetpoint.OutputRange = new Range<double>(plantController.SetbackMin, plantController.SetbackMax);
                setback = new SetpointSetback(scheduleName, rangeSetpoint);
            }

            SystemController result = null;

            switch (plantController.ControlType)
            {
                case tpdControlType.tpdControlNormal:
                    result = new SystemLiquidNormalController(@dynamic.Name, liquidNormalControllerDataType, setpoint, setback) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlOutdoor:
                    OutdoorControllerDataType outdoorControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM_OutdoorControllerDataType();
                    result = new SystemOutdoorController(@dynamic.Name, outdoorControllerDataType, setpoint, setback) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlDifference:
                    result = new SystemLiquidDifferenceController(@dynamic.Name, sensorReference, secondarySensorReference, liquidNormalControllerDataType, setpoint, setback);
                    break;

                case tpdControlType.tpdControlPassThrough:
                    result = new SystemLiquidPassthroughController(@dynamic.Name, liquidNormalControllerDataType) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlNot:
                    result = new SystemNotLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlMin:
                    result = new SystemMinLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlMax:
                    result = new SystemMaxLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlSig:
                    result = new SystemSigLogicalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlIf:
                    result = new SystemIfLogicalController(@dynamic.Name);
                    break;

                    //case tpdControlType.tpdControlGroup:
                    //    systemController = new SystemNormalController(@dynamic.Name);
                    //    break;
            }

            if (result == null)
            {
                return null;
            }

            IReference reference = Create.Reference(plantController);
            if (reference != null)
            {
                Modify.SetReference(result, reference.ToString());
            }

            result.Description = dynamic.Description;
            //Modify.SetReference(systemController, @dynamic.GUID);

            List<PlantDayType> plantDayTypes = plantController.PlantDayTypes();
            if (plantDayTypes != null && plantDayTypes.Count != 0)
            {
                result.DayTypeNames = new HashSet<string>(plantDayTypes.ConvertAll(x => x.Name));
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            IDisplaySystemController displaySystemController = Systems.Create.DisplayObject<IDisplaySystemController>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemController != null)
            {
                return displaySystemController;
            }

            return result;
        }
    }
}
