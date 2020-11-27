namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool RemoveInternalCondition(this TBD.Building building, string name)
        {
            if (building == null)
                return false;

            int index = 0;
            TBD.InternalCondition internalCondition = building.GetIC(index);
            while (internalCondition != null)
            {
                if(internalCondition.name.Equals(name))
                {
                    building.RemoveIC(index);
                    return true;
                }

                index++;
                internalCondition = building.GetIC(index);
            }

            return false;
        }
    }
}