using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<ControlArc> ControlArcs(this Controller controller) 
        { 
            if(controller == null)
            {
                return null;
            }

            List<ControlArc> result = new List<ControlArc>();

            int count = controller.GetControlArcCount();
            for (int i = 1; i <= count; i++)
            {
                ControlArc controlArc = controller.GetControlArc(i);
                if (controlArc == null)
                {
                    continue;
                }

                result.Add(controlArc);
            }

            return result;
        }

        public static List<ControlArc> ControlArcs(this SystemComponent systemComponent)
        {
            if (systemComponent == null)
            {
                return null;
            }

            dynamic @dynamic = systemComponent;

            List<ControlArc> result = new List<ControlArc>();

            int portCount = @dynamic.GetControlPortCount();
            for (int i = 1; i <= portCount; i++)
            {
                int arcCount = @dynamic.GetControlArcCount(i);
                for (int j = 1; j <= arcCount; j++)
                {
                    ControlArc controlArc = dynamic.GetControlArc(i, j);
                    if (controlArc == null)
                    {
                        continue;
                    }

                    result.Add(controlArc);
                }
            }

            return result;
        }

        public static List<ControlArc> ControlArcs(this Controller controller, ComponentGroup componentGroup, bool control = true, bool chain = true)
        {
            if(controller == null || (!control && !chain))
            {
                return null;
            }

            List<ControlArc> result = new List<ControlArc>();

            if(chain)
            {
                List<ControlArc> controlArcs = controller.ChainControlArcs();
                if (controlArcs != null)
                {
                    foreach (ControlArc controlArc_Temp in controlArcs)
                    {
                        ComponentGroup componentGroup_Temp = controlArc_Temp.GetGroup();
                        if (componentGroup_Temp != null && componentGroup_Temp.Reference() == componentGroup.Reference())
                        {
                            result.Add(controlArc_Temp);
                        }
                    }
                }
            }

            if(control)
            {
                List<ControlArc> controlArcs = controller.ControlArcs();
                if (controlArcs != null)
                {
                    foreach (ControlArc controlArc_Temp in controlArcs)
                    {
                        ComponentGroup componentGroup_Temp = controlArc_Temp.GetGroup();
                        if (componentGroup_Temp != null && componentGroup_Temp.Reference() == componentGroup.Reference())
                        {
                            result.Add(controlArc_Temp);
                        }
                    }
                }
            }



            return result;
        }
    }
}