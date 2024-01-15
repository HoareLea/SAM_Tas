using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static void Connect(this SystemPlantRoom systemPlantRoom, global::TPD.System system)
        {
            if (systemPlantRoom == null || system == null)
            {
                return;
            }

            string reference_System = (system as dynamic).GUID;

            AirSystem airSystem = systemPlantRoom.Find<AirSystem>(x => x?.Reference() == reference_System);
            if(airSystem == null)
            {
                return;
            }

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(system, true);
            if(systemComponents == null || systemComponents.Count == 0)
            {
                return;
            }

            foreach(global::TPD.SystemComponent systemComponent in systemComponents)
            {
                if(systemComponent == null)
                {
                    continue;
                }

                string reference_1 = (systemComponent as dynamic).GUID;

                Core.Systems.ISystemComponent systemComponent_1 = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_1);

                foreach (Core.Direction direction in new Core.Direction[] { Direction.In, Direction.Out})
                {
                    List<Duct> ducts = Query.Ducts(systemComponent, direction);
                    if (ducts != null)
                    {
                        foreach (Duct duct in ducts)
                        {
                            string reference_2 = (direction == Direction.In ? duct.GetUpstreamComponent() as dynamic : duct.GetDownstreamComponent() as dynamic)?.GUID;

                            Core.Systems.ISystemComponent systemComponent_2 = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                            if (systemComponent_2 == null)
                            {
                                continue;
                            }

                            int index_1 = Core.Systems.Query.FindIndex(systemPlantRoom, systemComponent_1, new SystemType(airSystem), ConnectorStatus.Unconnected, direction);
                            if(index_1 == -1)
                            {
                                continue;
                            }

                            int index_2 = Core.Systems.Query.FindIndex(systemPlantRoom, systemComponent_2, new SystemType(airSystem), ConnectorStatus.Unconnected, direction.Opposite());
                            if(index_2 == -1)
                            {
                                continue;
                            }

                            //TODO: Match In/Out direction of components

                            systemPlantRoom.Connect(systemComponent_1, systemComponent_2, airSystem, index_1, index_2);
                        }
                    }
                }
            }

        }
    }
}