using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Core;
using System.Collections.Generic;

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

            ISetpoint setpoint = null;
            
            ControllerProfileData ControllerProfileData_Setpoint = controller.GetProfile();

            List<ControllerProfilePoint> controllerProfilePoints_Setpoint = ControllerProfileData_Setpoint?.ControllerProfilePoints();
            if(controllerProfilePoints_Setpoint != null && controllerProfilePoints_Setpoint.Count > 1)
            {
                ProfileSetpoint profileSetpoint = new ProfileSetpoint();
                foreach(ControllerProfilePoint controllerProfilePoint in controllerProfilePoints_Setpoint)
                {
                    profileSetpoint.Add(controllerProfilePoint.x, controllerProfilePoint.y);
                }

                setpoint = profileSetpoint;
            }

            if(setpoint == null)
            {
                RangeSetpoint rangeSetpoint = new RangeSetpoint();
                rangeSetpoint.InputRange = new Range<double>(controller.Setpoint - (controller.Gradient * controller.Band));
                rangeSetpoint.OutputRange = new Range<double>(controller.Min, controller.Max);
                setpoint = rangeSetpoint;
            }

            NormalControllerDataType normalControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM();
            NormalControllerLimit normalControllerLimit = (controller.SensorPresetType).ToSAM();

            string scheduleName = controller.GetSchedule()?.Name;

            ISetback setback = null;

            ControllerProfileData ControllerProfileData_Setback = controller.GetSetbackProfile();

            List<ControllerProfilePoint> controllerProfilePoints_Setback = ControllerProfileData_Setback?.ControllerProfilePoints();
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
                rangeSetpoint.InputRange = new Range<double>(controller.SetbackSetpoint - (controller.SetbackGradient * controller.SetbackBand));
                rangeSetpoint.OutputRange = new Range<double>(controller.SetbackMin, controller.SetbackMax);
                setback = new SetpointSetback(scheduleName, rangeSetpoint);
            }

            SystemController result = null;

            switch(controller.ControlType)
            {
                case tpdControlType.tpdControlNormal:
                    result = new SystemNormalController(@dynamic.Name, normalControllerDataType, setpoint, normalControllerLimit) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlOutdoor:
                    OutdoorControllerDataType outdoorControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM_OutdoorControllerDataType();
                    result = new SystemOutdoorController(@dynamic.Name, outdoorControllerDataType, setpoint) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlDifference:
                    result = new SystemDifferenceController(@dynamic.Name, normalControllerDataType, setpoint, normalControllerLimit) { SensorReference = sensorReference };
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

            ISetpoint setpoint = null;

            ControllerProfileData ControllerProfileData_Setpoint = plantController.GetProfile();

            List<ControllerProfilePoint> controllerProfilePoints_Setpoint = ControllerProfileData_Setpoint?.ControllerProfilePoints();
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

            NormalControllerDataType normalControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM();
            NormalControllerLimit normalControllerLimit = (plantController.SensorPresetType).ToSAM();

            string scheduleName = plantController.GetSchedule()?.Name;

            ISetback setback = null;

            ControllerProfileData ControllerProfileData_Setback = plantController.GetSetbackProfile();

            List<ControllerProfilePoint> controllerProfilePoints_Setback = ControllerProfileData_Setback?.ControllerProfilePoints();
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
                    result = new SystemNormalController(@dynamic.Name, normalControllerDataType, setpoint, normalControllerLimit) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlOutdoor:
                    OutdoorControllerDataType outdoorControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM_OutdoorControllerDataType();
                    result = new SystemOutdoorController(@dynamic.Name, outdoorControllerDataType, setpoint) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlDifference:
                    result = new SystemDifferenceController(@dynamic.Name, normalControllerDataType, setpoint, normalControllerLimit) { SensorReference = sensorReference };
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
