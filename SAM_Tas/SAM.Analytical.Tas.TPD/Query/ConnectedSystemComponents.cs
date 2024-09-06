using SAM.Core;
using SAM.Core.Systems;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.SystemComponent> ConnectedSystemComponents(this global::TPD.SystemComponent systemComponent, Direction direction)
        {
            List<global::TPD.Duct> ducts = Ducts(systemComponent, direction);
            if(ducts == null)
            {
                return null;
            }

            List<global::TPD.SystemComponent> result = new List<global::TPD.SystemComponent>();
            foreach (global::TPD.Duct duct in ducts)
            {
                global::TPD.SystemComponent systemComponent_Temp = null;

                systemComponent_Temp = duct.GetDownstreamComponent();
                if(systemComponent_Temp != null && systemComponent_Temp != systemComponent && !result.Contains(systemComponent_Temp))
                {
                    result.Add(systemComponent_Temp);
                }

                systemComponent_Temp = duct.GetUpstreamComponent();
                if (systemComponent_Temp != null && systemComponent_Temp != systemComponent && !result.Contains(systemComponent_Temp))
                {
                    result.Add(systemComponent_Temp);
                }
            }

            return result;
        }

        public static List<ISystemComponent> ConnectedSystemComponents<T>(this SystemPlantRoom systemPlantRoom, SystemGroup<T> systemGroup, ISystemComponent systemComponent) where T : Core.Systems.ISystem
        {
            if(systemPlantRoom == null || systemGroup == null || systemComponent == null)
            {
                return null;
            }

            List<ISystemComponent> systemComponents = systemPlantRoom.GetRelatedObjects<ISystemComponent>(systemGroup);
            if(systemComponents == null)
            {
                return null;
            }

            if(systemComponents.Find(x => (x as dynamic).Guid == (systemComponent as dynamic).Guid) == null)
            {
                return null;
            }

            List<Core.Systems.ISystem> systems = systemPlantRoom.GetRelatedObjects<Core.Systems.ISystem>(systemGroup);
            if(systems == null || systems.Count == 0)
            {
                return null;
            }

            return Core.Systems.Query.ConnectedSystemComponents(systemPlantRoom, systems[0], systemGroup, systemComponent);
        }
    }
}