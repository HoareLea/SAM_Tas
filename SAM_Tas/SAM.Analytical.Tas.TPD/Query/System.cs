using SAM.Core.Systems;
using System;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static T System<T>(this SystemPlantRoom systemPlantRoom, string reference) where T : ISystem
        {
            if(systemPlantRoom == null || string.IsNullOrWhiteSpace(reference))
            {
                return default;
            }

            Func<T, bool> func = new Func<T, bool>((T systemComponent_Temp) =>
            {
                if(systemComponent_Temp == null)
                {
                    return false;
                }

                return reference.Equals(Reference(systemComponent_Temp));
            });

            return systemPlantRoom.GetSystem(func);
        }
    }
}