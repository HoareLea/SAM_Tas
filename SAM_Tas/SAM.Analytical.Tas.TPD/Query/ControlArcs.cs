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

            int index = 1;

            List<ControlArc> result = new List<ControlArc>();

            int count = controller.GetControlArcCount();
            for (int i = 1; i <= count; i++)
            {
                ControlArc controlArc = controller.GetControlArc(i);

                result.Add(controlArc);
            }

            return result;
        }
    }
}