namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static global::TPD.System System(this global::TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetSystemCount(); i++)
            {
                global::TPD.System system = plantRoom.GetSystem(i);
                if(system == null)
                {
                    continue;
                }

                if(name.Equals((system as dynamic).Name))
                {
                    return system;
                }
            }

            return null;
        }
    }
}