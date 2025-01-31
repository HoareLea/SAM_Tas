using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.FuelGroup> FuelGroups(this global::TPD.PlantRoom plantRoom)
        {
            if (plantRoom is null)
            {
                return null;
            }

            List<global::TPD.FuelGroup> result = new List<global::TPD.FuelGroup>();
            for (int i = 1; i <= plantRoom.GetFuelGroupCount(); i++)
            {
                global::TPD.FuelGroup fuelGroup = plantRoom.GetFuelGroup(i);
                if(fuelGroup == null)
                {
                    continue;
                }

                result.Add(fuelGroup);
            }

            return result;
        }
    }
}