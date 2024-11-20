using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.HeatingGroup> HeatingGroups(this global::TPD.PlantRoom plantRoom)
        {
            if (plantRoom is null)
            {
                return null;
            }

            List<global::TPD.HeatingGroup> result = new List<global::TPD.HeatingGroup>();
            for (int i = 1; i <= plantRoom.GetHeatingGroupCount(); i++)
            {
                global::TPD.HeatingGroup heatingGroup = plantRoom.GetHeatingGroup(i);
                if(heatingGroup == null)
                {
                    continue;
                }

                result.Add(heatingGroup);
            }

            return result;
        }
    }
}