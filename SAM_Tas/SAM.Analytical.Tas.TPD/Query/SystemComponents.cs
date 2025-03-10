using SAM.Core;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<T> SystemComponents<T>(this global::TPD.System system, bool includeNested = false) where T : SystemComponent
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

                if (includeNested && systemComponent is ComponentGroup)
                {
                    List<T> ts = SystemComponents<T>((ComponentGroup)systemComponent, includeNested);
                    if (ts != null)
                    {
                        result.AddRange(ts);
                    }
                }
            }

            return result;

        }

        public static List<T> SystemComponents<T>(ComponentGroup componentGroup, bool includeNested = false) where T : SystemComponent
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
                    List<T> ts = SystemComponents<T>((ComponentGroup)systemComponent, includeNested);
                    if(ts != null)
                    {
                        result.AddRange(ts);
                    }
                }
            }

            return result;
        }

        public static List<T> SystemComponents<T>(ComponentGroup componentGroup, bool includeNested, bool includeConnectionJunctionBoxes) where T : SystemComponent
        {
            List<T> result = SystemComponents<T>(componentGroup, includeNested);
            if (includeConnectionJunctionBoxes)
            {
                return result;
            }

            for (int i = result.Count - 1; i >= 0; i--)
            {
                SystemComponent systemComponent = result[i];

                if (!(systemComponent is Junction))
                {
                    continue;
                }

                List<Duct> ducts_Temp = new List<Duct>();
                Ducts(systemComponent, Direction.Out)?.ForEach(x => ducts_Temp.Add(x));
                Ducts(systemComponent, Direction.In)?.ForEach(x => ducts_Temp.Add(x));

                if (ducts_Temp != null && ducts_Temp.Count != 0)
                {
                    foreach (Duct duct in ducts_Temp)
                    {
                        SystemComponent systemComponent_Temp = null;

                        systemComponent_Temp = duct.GetDownstreamComponent();
                        if (systemComponent_Temp != null && result.Find(x => ((dynamic)x).GUID == ((dynamic)systemComponent_Temp).GUID) == null)
                        {
                            result.RemoveAt(0);
                            break;
                        }

                        systemComponent_Temp = duct.GetUpstreamComponent();
                        if (systemComponent_Temp != null && result.Find(x => ((dynamic)x).GUID == ((dynamic)systemComponent_Temp).GUID) == null)
                        {
                            result.RemoveAt(0);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public static List<T> SystemComponents<T>(PlantGroup plantGroup, bool includeNested = false) where T : SystemComponent
        {
            if (plantGroup == null)
            {
                return null;
            }

            int index = 1;

            List<T> result = new List<T>();

            dynamic @dynamic = (dynamic)plantGroup;

            int count = @dynamic.GetComponentCount();
            for (int i = 1; i <= count; i++)
            {
                SystemComponent systemComponent = @dynamic.GetComponent(i);
                if (systemComponent is T)
                {
                    result.Add((T)systemComponent);
                }

                if (includeNested && systemComponent is PlantGroup)
                {
                    List<T> ts = SystemComponents<T>((PlantGroup)systemComponent, includeNested);
                    if (ts != null)
                    {
                        result.AddRange(ts);
                    }
                }
            }

            return result;
        }
    }
}