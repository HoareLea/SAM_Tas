namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TPD.DHWGroup DHWGroup(this TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetDHWGroupCount(); i++)
            {
                TPD.DHWGroup dHWGroup = plantRoom.GetDHWGroup(i);
                if(dHWGroup == null)
                {
                    continue;
                }

                if(name.Equals((dHWGroup as dynamic).Name))
                {
                    return dHWGroup;
                }
            }

            return null;
        }
    }
}