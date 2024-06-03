namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static global::TPD.ElectricalGroup ElectricalGroup(this global::TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetElectricalGroupCount(); i++)
            {
                global::TPD.ElectricalGroup electricalGroup = plantRoom.GetElectricalGroup(i);
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