using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Attributes;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.Systems;
using System.Collections.Generic;
using System.Linq;
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
                controller.ControlType = tpdControlType.tpdControlIf;
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
    }
}