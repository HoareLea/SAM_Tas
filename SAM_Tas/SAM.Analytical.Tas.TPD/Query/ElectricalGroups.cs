using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.ElectricalGroup> ElectricalGroups(this global::TPD.PlantRoom plantRoom)
        {
            if (plantRoom is null)
            {
                return null;
            }

            List<global::TPD.ElectricalGroup> result = new List<global::TPD.ElectricalGroup>();
            for (int i = 1; i <= plantRoom.GetElectricalGroupCount(); i++)
            {
                global::TPD.ElectricalGroup electricalGroup = plantRoom.GetElectricalGroup(i);
                if(electricalGroup == null)
                {
                    continue;
                }

                result.Add(electricalGroup);
            }

            return result;
        }
    }
}