using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.DHWGroup> DHWGroups(this global::TPD.PlantRoom plantRoom)
        {
            if (plantRoom is null)
            {
                return null;
            }

            List<global::TPD.DHWGroup> result = new List<global::TPD.DHWGroup>();
            for (int i = 1; i <= plantRoom.GetDHWGroupCount(); i++)
            {
                global::TPD.DHWGroup dHWGroup = plantRoom.GetDHWGroup(i);
                if(dHWGroup == null)
                {
                    continue;
                }

                result.Add(dHWGroup);
            }

            return result;
        }
    }
}