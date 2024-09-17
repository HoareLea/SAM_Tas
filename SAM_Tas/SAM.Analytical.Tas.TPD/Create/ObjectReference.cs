using TPD;
using SAM.Core;
using System;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static ObjectReference ObjectReference(this ISystemComponent systemComponent)
        {
            if(systemComponent == null)
            {
                return null;
            }

            Type type = null;
            if(systemComponent is Damper)
            {
                type = typeof(Damper);
            }

            if(type == null)
            {
                throw new NotImplementedException();
            }

            return new ObjectReference(type, systemComponent.GUID);
        }

        public static ObjectReference ObjectReference(this global::TPD.ISystem system)
        {
            if (system == null)
            {
                return null;
            }

            return new ObjectReference(typeof(global::TPD.ISystem), system.GUID);
        }

    }
}
