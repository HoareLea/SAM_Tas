using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PlantController ToTPD(this IDisplaySystemController displaySystemController, PlantRoom plantRoom)
        {
            if (displaySystemController == null || plantRoom == null)
            {
                return null;
            }

            PlantController result = plantRoom.AddController();
            
            if(displaySystemController is SystemNormalController)
            {
                result.ControlType = tpdControlType.tpdControlNormal;
            }
            else if (displaySystemController is SystemOutdoorController)
            {
                result.ControlType = tpdControlType.tpdControlOutdoor;
            }
            else if (displaySystemController is SystemDifferenceController)
            {
                result.ControlType = tpdControlType.tpdControlDifference;
            }
            else if (displaySystemController is SystemMaxLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlMax;
            }
            else if (displaySystemController is SystemMinLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlMin;
            }
            else if (displaySystemController is SystemNotLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlNot;
            }
            else if (displaySystemController is SystemSigLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlSig;
            }
            else if (displaySystemController is SystemIfLogicalController)
            {
                result.ControlType = tpdControlType.tpdControlIf;
            }

            displaySystemController.SetLocation(result);

            return result;
        }
    }
}
