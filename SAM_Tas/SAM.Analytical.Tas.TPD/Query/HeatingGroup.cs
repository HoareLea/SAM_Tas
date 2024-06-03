namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static global::TPD.HeatingGroup HeatingGroup(this global::TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetHeatingGroupCount(); i++)
            {
                global::TPD.HeatingGroup heatingGroup = plantRoom.GetHeatingGroup(i);
                if(heatingGroup == null)
                {
                    continue;
                }

                if(name.Equals((heatingGroup as dynamic).Name))
                {
                    return heatingGroup;
                }
            }

            return null;
        }
    }
}