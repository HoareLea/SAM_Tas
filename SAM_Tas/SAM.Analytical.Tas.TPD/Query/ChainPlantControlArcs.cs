using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<ControlArc> ChainControlArcs(this Controller controller)
        {
            if (controller == null)
            {
                return null;
            }

            List<ControlArc> result = new List<ControlArc>();

            int count = controller.GetChainArcCount();
            for (int i = 1; i <= count; i++)
            {
                ControlArc controlArc = controller.GetChainArc(i);
                if (controlArc == null)
                {
                    continue;
                }

                result.Add(controlArc);
            }

            return result;
        }
    }
}