using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool SetControlType(this Controller controller, IDisplaySystemController displaySystemController)
        {
            if (controller == null || displaySystemController == null)
            {
                return false;
            }

            if (displaySystemController is SystemOutdoorController)
            {
                controller.ControlType = tpdControlType.tpdControlOutdoor;
            }
            else if (displaySystemController is SystemDifferenceController)
            {
                controller.ControlType = tpdControlType.tpdControlDifference;
            }
            else if (displaySystemController is SystemMaxLogicalController)
            {
                controller.ControlType = tpdControlType.tpdControlMax;
            }
            else if (displaySystemController is SystemMinLogicalController)
            {
                controller.ControlType = tpdControlType.tpdControlMin;
            }
            else if (displaySystemController is SystemNotLogicalController)
            {
                controller.ControlType = tpdControlType.tpdControlNot;
            }
            else if (displaySystemController is SystemSigLogicalController)
            {
                controller.ControlType = tpdControlType.tpdControlSig;
            }
            else if (displaySystemController is SystemIfLogicalController)
            {
                controller.ControlType = tpdControlType.tpdControlIf;
            }
            else if (displaySystemController is SystemPassthroughController)
            {
                controller.ControlType = tpdControlType.tpdControlPassThrough;
            }
            else if (displaySystemController is SystemNormalController)
            {
                controller.ControlType = tpdControlType.tpdControlNormal;
            }
            else
            {
                return false;
            }

            return true;
        }

        public static bool SetControlType(this PlantController plantController, IDisplaySystemController displaySystemController)
        {
            if (plantController == null || displaySystemController == null)
            {
                return false;
            }

            if (displaySystemController is SystemOutdoorController)
            {
                plantController.ControlType = tpdControlType.tpdControlOutdoor;
            }
            else if (displaySystemController is SystemDifferenceController)
            {
                plantController.ControlType = tpdControlType.tpdControlDifference;
            }
            else if (displaySystemController is SystemMaxLogicalController)
            {
                plantController.ControlType = tpdControlType.tpdControlMax;
            }
            else if (displaySystemController is SystemMinLogicalController)
            {
                plantController.ControlType = tpdControlType.tpdControlMin;
            }
            else if (displaySystemController is SystemNotLogicalController)
            {
                plantController.ControlType = tpdControlType.tpdControlNot;
            }
            else if (displaySystemController is SystemSigLogicalController)
            {
                plantController.ControlType = tpdControlType.tpdControlSig;
            }
            else if (displaySystemController is SystemIfLogicalController)
            {
                plantController.ControlType = tpdControlType.tpdControlIf;
            }
            else if (displaySystemController is SystemPassthroughController)
            {
                plantController.ControlType = tpdControlType.tpdControlPassThrough;
            }
            else if (displaySystemController is SystemNormalController)
            {
                plantController.ControlType = tpdControlType.tpdControlNormal;
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}