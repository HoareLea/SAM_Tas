namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TPD.MultiBoiler MultiBoiler(this TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetComponentCount(); i++)
            {
                TPD.MultiBoiler multiBoiler = plantRoom.GetComponent(i) as TPD.MultiBoiler;
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