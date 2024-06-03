namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static global::TPD.DesignConditionLoad DesignConditionLoad(this global::TPD.EnergyCentre energyCentre, string name)
        {
            if (energyCentre is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
            {
                global::TPD.DesignConditionLoad designConditionLoad = energyCentre.GetDesignCondition(i);
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