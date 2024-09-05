using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Core;

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

            SystemController systemController = null;

            switch(controller.ControlType)
            {
                case tpdControlType.tpdControlNormal:
                    systemController = new SystemNormalController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlOutdoor:
                    systemController = new SystemOutdoorController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlDifference:
                    systemController = new SystemDifferenceController(@dynamic.Name);
                    break;

                case tpdControlType.tpdControlPassThrough:
                    systemController = new SystemPassthroughController(@dynamic.Name);
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
