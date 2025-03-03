using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.DesignConditionLoad> DesignConditionLoads(this global::TPD.SizedFlowVariable sizedFlowVariable)
        {
            if (sizedFlowVariable == null)
            {
                return null;
            }

            List<global::TPD.DesignConditionLoad> result = new List<global::TPD.DesignConditionLoad>();
            for (int i = 1; i <= sizedFlowVariable.GetDesignConditionCount(); i++)
            {
                global::TPD.DesignConditionLoad designConditionLoad = sizedFlowVariable.GetDesignCondition(i);
                if (designConditionLoad == null)
                {
                    continue;
                }

                result.Add(designConditionLoad);
            }

            return result;
        }

        public static List<global::TPD.DesignConditionLoad> DesignConditionLoads(this global::TPD.SizedVariable sizedVariable)
        {
            if (sizedVariable == null)
            {
                return null;
            }

            List<global::TPD.DesignConditionLoad> result = new List<global::TPD.DesignConditionLoad>();
            for (int i = 1; i <= sizedVariable.GetDesignConditionCount(); i++)
            {
                global::TPD.DesignConditionLoad designConditionLoad = sizedVariable.GetDesignCondition(i);
                if(designConditionLoad == null)
                {
                    continue;
                }

                result.Add(designConditionLoad);
            }

            return result;
        }

        public static List<global::TPD.DesignConditionLoad> DesignConditionLoads(this global::TPD.EnergyCentre energyCentre)
        {
            if (energyCentre is null)
            {
                return null;
            }

            List<global::TPD.DesignConditionLoad> result = new List<global::TPD.DesignConditionLoad>();
            for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
            {
                global::TPD.DesignConditionLoad designConditionLoad = energyCentre.GetDesignCondition(i);
                if (designConditionLoad == null)
                {
                    continue;
                }

                result.Add(designConditionLoad);
            }

            return result;
        }
    }
}