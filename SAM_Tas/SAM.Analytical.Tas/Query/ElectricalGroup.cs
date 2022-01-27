namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TPD.ElectricalGroup ElectricalGroup(this TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetElectricalGroupCount(); i++)
            {
                TPD.ElectricalGroup electricalGroup = plantRoom.GetElectricalGroup(i);
                if(electricalGroup == null)
                {
                    continue;
                }

                if(name.Equals((electricalGroup as dynamic).Name))
                {
                    return electricalGroup;
                }
            }

            return null;
        }
    }
}