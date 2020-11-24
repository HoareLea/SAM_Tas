using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.InternalCondition AddInternalCondition(this TBD.Building building, Space space, ProfileLibrary profileLibrary)
        {
            if (building == null || space == null || profileLibrary == null)
                return null;

            TBD.InternalCondition internalCondition = building.AddIC(null);
            if (internalCondition == null)
                return null;

            UpdateInternalCondition(internalCondition, space, profileLibrary);

            return internalCondition; 
        }
    }
}