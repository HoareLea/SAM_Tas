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
            OutdoorControllerDataType outdoorControllerDataType = ((tpdSensorType)@dynamic.SensorType).ToSAM_OutdoorControllerDataType();

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

            SystemController systemController = null;

            switch(controller.ControlType)
            {
                case tpdControlType.tpdControlNormal:
                    systemController = new SystemNormalController(@dynamic.Name, normalControllerDataType, setpoint, normalControllerLimit) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlOutdoor:
                    systemController = new SystemOutdoorController(@dynamic.Name, outdoorControllerDataType, setpoint) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlDifference:
                    systemController = new SystemDifferenceController(@dynamic.Name, normalControllerDataType, setpoint, normalControllerLimit) { SensorReference = sensorReference };
                    break;

                case tpdControlType.tpdControlPassThrough:
                    systemController = new SystemPassthroughController(@dynamic.Name, normalControllerDataType) { SensorReference = sensorReference };
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

            IDisplaySystemController result = Systems.Create.DisplayObject<IDisplaySystemController>(systemController, location, Systems.Query.DefaultDisplaySystemManager());
            if(result == null)
            {
                return null;
            }

            return result;
        }
    }
}
