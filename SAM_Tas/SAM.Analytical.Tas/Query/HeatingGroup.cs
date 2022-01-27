namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TPD.HeatingGroup HeatingGroup(this TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetHeatingGroupCount(); i++)
            {
                TPD.HeatingGroup heatingGroup = plantRoom.GetHeatingGroup(i);
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