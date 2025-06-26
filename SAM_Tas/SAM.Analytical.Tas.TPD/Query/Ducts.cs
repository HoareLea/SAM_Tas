using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.Duct> Ducts(global::TPD.ISystemComponent systemComponent, Direction direction)
        {
            if(systemComponent == null)
            {
                return null;
            }

            int portCount = direction == Direction.Out ? (systemComponent as dynamic).GetOutputPortCount() : (systemComponent as dynamic).GetInputPortCount();
            if(portCount < 1)
            {
                return null;
            }

            List<global::TPD.Duct> result = new List<global::TPD.Duct>();
            for(int i = 1; i <= portCount; i ++)
            {
                int ductCount = direction == Direction.Out ? (systemComponent as dynamic).GetOutputDuctCount(i) : (systemComponent as dynamic).GetInputDuctCount(i);
                if(ductCount < 1)
                {
                    continue;
                }

                for (int j = 1; j <= ductCount; j++)
                {
                    global::TPD.Duct duct = direction == Direction.Out ? (systemComponent as dynamic).GetOutputDuct(i, j) : (systemComponent as dynamic).GetInputDuct(i, j);

                    if(duct == null)
                    {
                        continue;
                    }

                    result.Add(duct);
                }

            }

            return result;
        }

        public static List<global::TPD.Duct> Ducts(global::TPD.System system)
        {
            if (system == null)
            {
                return null;
            }

            int ductCount = system.GetDuctCount();
            if (ductCount < 1)
            {
                return null;
            }

            List<global::TPD.Duct> result = new List<global::TPD.Duct>();
            for (int i = 1; i <= ductCount; i++)
            {
                global::TPD.Duct duct = system.GetDuct(i);
                if (duct == null)
                {
                    continue;
                }

                result.Add(duct);
            }

            return result;
        }

        public static List<global::TPD.Duct> Ducts(global::TPD.ComponentGroup componentGroup)
        {
            if (componentGroup == null)
            {
                return null;
            }

            int ductCount = componentGroup.GetDuctCount();
            if (ductCount < 1)
            {
                return null;
            }

            List<global::TPD.Duct> result = new List<global::TPD.Duct>();
            for (int i = 1; i <= ductCount; i++)
            {
                global::TPD.Duct duct = componentGroup.GetDuct(i);
                if (duct == null)
                {
                    continue;
                }

                result.Add(duct);
            }

            return result;
        }

        public static List<global::TPD.Duct> Ducts(global::TPD.ISystemComponent systemComponent_1, Direction direction, global::TPD.ISystemComponent systemComponent_2)
        {
            if(systemComponent_1 == null || direction == Direction.Undefined || systemComponent_2 == null)
            {
                return null;
            }

            List<global::TPD.Duct> result = Ducts(systemComponent_1, direction);
            if(result == null || result.Count == 0)
            {
                return null;
            }

            string guid_1 = null;

            dynamic componentGroup_1 = systemComponent_1 is global::TPD.ComponentGroup ? systemComponent_1 : (systemComponent_1 as dynamic).GetGroup();
            if (componentGroup_1 != null)
            {
                guid_1 = componentGroup_1.Guid;
            }

            string guid_2 = null;
            dynamic componentGroup_2 = systemComponent_2 is global::TPD.ComponentGroup ? systemComponent_2 : (systemComponent_2 as dynamic).GetGroup();
            if (componentGroup_2 != null)
            {
                guid_2 = componentGroup_2.Guid;
            }

            int count = result.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                global::TPD.Duct duct = result[i];

                global::TPD.ISystemComponent systemComponent = direction == Direction.Out ? duct.GetDownstreamComponent() : duct.GetUpstreamComponent();
                if(systemComponent == null)
                {
                    result.RemoveAt(i);
                    continue;
                }

                if((systemComponent as dynamic).GUID != (systemComponent_2 as dynamic).GUID)
                {
                    if (guid_1 != guid_2)
                    {
                        List<global::TPD.Duct> ducts_Temp = Ducts(systemComponent, direction, systemComponent_2);
                        if (ducts_Temp != null && ducts_Temp.Count != 0)
                        {
                            result.AddRange(ducts_Temp);
                            continue;
                        }
                    }

                    if(systemComponent is global::TPD.ComponentGroup)
                    {
                        if((systemComponent as dynamic).GUID == guid_1 || (systemComponent as dynamic).GUID == guid_2)
                        {
                            continue;
                        }
                    }

                    result.RemoveAt(i);
                    continue;
                }
            }

            return result;
        }
    }
}