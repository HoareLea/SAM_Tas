using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<T> SystemComponents<T>(this global::TPD.System system) where T : SystemComponent
        { 
            if(system == null)
            {
                return null;
            }

            int index = 1;

            List<T> result = new List<T>();

            int count = system.GetComponentCount();
            for (int i = 1; i <= count; i++)
            {
                SystemComponent systemComponent = system.GetComponent(i);
                if (systemComponent is T)
                {
                    result.Add((T)systemComponent);
                }
            }

            return result;

        }

        public static List<T> SystemComponents<T>(ComponentGroup componentGroup) where T : SystemComponent
        {
            if(componentGroup == null)
            {
                return null;
            }

            int index = 1;

            List<T> result = new List<T>();

            int count = componentGroup.GetComponentCount();
            for (int i = 1; i <= count; i++)
            {
                SystemComponent systemComponent = componentGroup.GetComponent(i);
                if (systemComponent is T)
                {
                    result.Add((T)systemComponent);
                }
            }

            return result;
        }
    }
}