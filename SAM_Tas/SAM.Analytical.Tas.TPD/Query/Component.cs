namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static T Component<T>(this global::TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return default;
            }

            for (int i = 1; i <= plantRoom.GetComponentCount(); i++)
            {
                global::TPD.PlantComponent plantComponent = plantRoom.GetComponent(i);
                if(!(plantComponent is T))
                {
                    continue;
                }

                if(name.Equals((plantComponent as dynamic).Name))
                {
                    return (T)plantComponent;
                }
            }

            return default;
        }
    }
}