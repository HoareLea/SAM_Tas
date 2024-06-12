namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TPD.PlantSchedule PlantSchedule(this TPD.EnergyCentre energyCentre, string name)
        {
            if (energyCentre is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= energyCentre.GetScheduleCount(); i++)
            {
                TPD.PlantSchedule plantSchedule = energyCentre.GetSchedule(i);
                if(plantSchedule == null)
                {
                    continue;
                }

                if(name.Equals((plantSchedule as dynamic).Name))
                {
                    return plantSchedule;
                }
            }

            return null;
        }
    }
}