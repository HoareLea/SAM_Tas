namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static global::TPD.MultiBoiler MultiBoiler(this global::TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetComponentCount(); i++)
            {
                global::TPD.MultiBoiler multiBoiler = plantRoom.GetComponent(i) as global::TPD.MultiBoiler;
                if(multiBoiler == null)
                {
                    continue;
                }

                if(name.Equals((multiBoiler as dynamic).Name))
                {
                    return multiBoiler;
                }
            }

            return null;
        }
    }
}