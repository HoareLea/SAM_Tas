namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static global::TPD.PlantRoom PlantRoom(this global::TPD.EnergyCentre energyCentre, string name)
        {
            if (energyCentre is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= energyCentre.GetPlantRoomCount(); i++)
            {
                global::TPD.PlantRoom plantRoom = energyCentre.GetPlantRoom(i);
                if (plantRoom == null)
                {
                    continue;
                }

                if (name.Equals((plantRoom as dynamic).Name))
                {
                    return plantRoom;
                }
            }

            return null;
        }
    }
}