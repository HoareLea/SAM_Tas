namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TPD.DesignConditionLoad DesignConditionLoad(this TPD.EnergyCentre energyCentre, string name)
        {
            if (energyCentre is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
            {
                TPD.DesignConditionLoad designConditionLoad = energyCentre.GetDesignCondition(i);
                if(designConditionLoad == null)
                {
                    continue;
                }

                if(name.Equals((designConditionLoad as dynamic).Name))
                {
                    return designConditionLoad;
                }
            }

            return null;
        }
    }
}