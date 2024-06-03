namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static global::TPD.CoolingGroup CoolingGroup(this global::TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetCoolingGroupCount(); i++)
            {
                global::TPD.CoolingGroup coolingGroup = plantRoom.GetCoolingGroup(i);
                if(coolingGroup == null)
                {
                    continue;
                }

                if(name.Equals((coolingGroup as dynamic).Name))
                {
                    return coolingGroup;
                }
            }

            return null;
        }
    }
}