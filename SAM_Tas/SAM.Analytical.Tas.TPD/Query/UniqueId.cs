using System;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static string UniqueId(this EquipmentType equipmentType, Enum @enum)
        {
            return string.Format("{0} {1}", equipmentType.ToString(), @enum.ToString());
        }
    }
}