using SAM.Core;
using SAM.Core.Systems;
using System;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static T SystemComponent<T>(this SystemPlantRoom systemPlantRoom, string reference) where T : Core.Systems.ISystemComponent
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

            return systemPlantRoom.GetSystemComponent(func);
        }

        public static Controller Controller(this SystemPlantRoom systemPlantRoom, string reference)
        {
            if(systemPlantRoom == null || string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            PathReference pathReference = Core.Convert.ComplexReference(reference) as PathReference;
            if(pathReference == null)
            {
                return null;
            }

            if(pathReference.Count() < 2)
            {
                return null;
            }

            throw new NotImplementedException();

            ObjectReference objectReference = pathReference.ElementAt(0);
            if(objectReference.TypeName == null)
            {
                return null;
            }

            return null;
        }
    }
}