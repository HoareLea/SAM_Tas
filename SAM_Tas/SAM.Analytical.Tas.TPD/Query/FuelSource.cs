namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static global::TPD.FuelSource FuelSource(this global::TPD.EnergyCentre energyCentre, string name)
        {
            if (energyCentre is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= energyCentre.GetFuelSourceCount(); i++)
            {
                global::TPD.FuelSource fuelSource = energyCentre.GetFuelSource(i);
                if(fuelSource == null)
                {
                    continue;
                }

                if(name.Equals((fuelSource as dynamic).Name))
                {
                    return fuelSource;
                }
            }

            return null;
        }

        public static global::TPD.FuelSource FuelSource(this global::TPD.PlantRoom plantRoom, string name)
        {
            return FuelSource(plantRoom?.GetEnergyCentre(), name);
        }
    }
}