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

            SystemComponent systemComponent = system.GetComponent(index);
            while(systemComponent != null)
            {
                if (systemComponent is T)
                {
                    result.Add((T)systemComponent);
                }
                index++;
                systemComponent = system.GetComponent(index);
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

            SystemComponent systemComponent = componentGroup.GetComponent(index);
            while (systemComponent != null)
            {
                if (systemComponent is T)
                {
                    result.Add((T)systemComponent);
                }
                index++;
                systemComponent = componentGroup.GetComponent(index);
            }

            return result;
        }
    }
}