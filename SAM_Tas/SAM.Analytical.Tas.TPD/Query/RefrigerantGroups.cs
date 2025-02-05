using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.RefrigerantGroup> RefrigerantGroups(this global::TPD.PlantRoom plantRoom)
        {
            if (plantRoom is null)
            {
                return null;
            }

            List<global::TPD.RefrigerantGroup> result = new List<global::TPD.RefrigerantGroup>();
            for (int i = 1; i <= plantRoom.GetRefrigerantGroupCount(); i++)
            {
                global::TPD.RefrigerantGroup refrigerantGroup = plantRoom.GetRefrigerantGroup(i);
                if(refrigerantGroup == null)
                {
                    continue;
                }

                result.Add(refrigerantGroup);
            }

            return result;
        }
    }
}