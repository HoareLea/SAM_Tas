using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<T> PlantComponents<T>(this IPlantRoom plantRoom, bool includeNested = false) where T : PlantComponent
        { 
            if(plantRoom == null)
            {
                return null;
            }

            int index = 1;

            List<T> result = new List<T>();

            int count = plantRoom.GetComponentCount();
            for (int i = 1; i <= count; i++)
            {
                PlantComponent systemComponent = plantRoom.GetComponent(i);
                if (systemComponent is T)
                {
                    result.Add((T)systemComponent);
                }

                if (includeNested && systemComponent is ComponentGroup)
                {
                    List<T> ts = PlantComponents<T>((ComponentGroup)systemComponent, includeNested);
                    if (ts != null)
                    {
                        result.AddRange(ts);
                    }
                }
            }

            return result;

        }

        public static List<T> PlantComponents<T>(ComponentGroup componentGroup, bool includeNested = false) where T : PlantComponent
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

                if(includeNested  && systemComponent is ComponentGroup)
                {
                    List<T> ts = PlantComponents<T>((ComponentGroup)systemComponent, includeNested);
                    if(ts != null)
                    {
                        result.AddRange(ts);
                    }
                }
            }

            return result;
        }
    }
}