using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Systems;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<ISystemConnection> Connect(this SystemPlantRoom systemPlantRoom, global::TPD.System system)
        {
            if (systemPlantRoom == null || system == null)
            {
                return null;
            }

            string reference_System = (system as dynamic).GUID;

            AirSystem airSystem = systemPlantRoom.Find<AirSystem>(x => x?.Reference() == reference_System);
            if(airSystem == null)
            {
                return null;
            }

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(system, true);
            if(systemComponents == null || systemComponents.Count == 0)
            {
                return null;
            }

            List<ISystemConnection> result = new List<ISystemConnection>();
            foreach (global::TPD.SystemComponent systemComponent in systemComponents)
            {
                if(systemComponent == null)
                {
                    continue;
                }

                string reference_1 = (systemComponent as dynamic).GUID;

                Core.Systems.ISystemComponent systemComponent_1 = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_1);

                foreach (Direction direction in new Direction[] { Direction.In })
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

                            List<Point2D> point2Ds = Query.Point2Ds(duct);

                            ISystemConnection systemConnection = Connect(systemPlantRoom, systemComponent_1, systemComponent_2, airSystem, direction, point2Ds);
                            if(systemConnection != null)
                            {
                                result.Add(systemConnection);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static ISystemConnection Connect(this SystemPlantRoom systemPlantRoom, Core.Systems.ISystemComponent systemComponent_1, Core.Systems.ISystemComponent systemComponent_2, Core.Systems.ISystem system, Direction direction, IEnumerable<Point2D> point2Ds = null)
        {
            if(systemPlantRoom == null || systemComponent_1 == null || systemComponent_2 == null || system == null)
            {
                return null;
            }

            if (!Geometry.Systems.Query.TryGetIndexes(systemPlantRoom, systemComponent_1, systemComponent_2, out int index_1, out int index_2, new SystemType(system), direction))
            {
                return null;
            }

            if (!(systemComponent_1 is IDisplaySystemObject<SystemGeometryInstance>) || !(systemComponent_2 is IDisplaySystemObject<SystemGeometryInstance>))
            {
                if(systemPlantRoom.Connect(systemComponent_1, systemComponent_2, out ISystemConnection systemConnection, system, index_1, index_2))
                {
                    return systemConnection;
                }

                return null;
            }

            Point2D point2D_1 = (systemComponent_1 as dynamic).SystemGeometry.GetPoint2D(index_1);
            if (point2D_1 == null)
            {
                return null;
            }

            Point2D point2D_2 = (systemComponent_2 as dynamic).SystemGeometry.GetPoint2D(index_2);
            if (point2D_2 == null)
            {
                return null;
            }

            List<Point2D> point2Ds_Temp = point2Ds == null ? null : new List<Point2D>(point2Ds);
            if (point2Ds_Temp == null || point2Ds_Temp.Count == 0)
            {
                point2Ds_Temp = new List<Point2D>() { point2D_1, point2D_2 };
            }
            else
            {
                if (point2D_1.Distance(point2Ds_Temp.First()) + point2D_2.Distance(point2Ds_Temp.Last()) < point2D_1.Distance(point2Ds_Temp.Last()) + point2D_2.Distance(point2Ds_Temp.First()))
                {
                    point2Ds_Temp.Insert(0, point2D_1);
                    point2Ds_Temp.Add(point2D_2);
                }
                else
                {
                    point2Ds_Temp.Insert(0, point2D_2);
                    point2Ds_Temp.Add(point2D_1);
                }

            }

            if (point2Ds_Temp == null || point2Ds_Temp.Count < 2)
            {
                return null;
            }

            DisplaySystemConnection result = new DisplaySystemConnection(new SystemConnection(new SystemType(system), systemComponent_1, index_1, systemComponent_2, index_2), point2Ds_Temp?.ToArray());

            return systemPlantRoom.Connect(result, system) ? result : null;
        }

        public static List<ISystemConnection> Connect(this SystemPlantRoom systemPlantRoom, ComponentGroup componentGroup)
        {
            if(systemPlantRoom == null || componentGroup == null)
            {
                return null;
            }

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(componentGroup);
            if(systemComponents == null)
            {
                return null;
            }

            List<ISystemConnection> result = new List<ISystemConnection>();

            //Connect componentGroup with the rest of the system
            foreach (global::TPD.SystemComponent systemComponent_Temp in systemComponents)
            {
                if (!(systemComponent_Temp is Junction))
                {
                    continue;
                }

                AirSystem airSystem = systemPlantRoom.System<AirSystem>(((systemComponent_Temp as dynamic)?.GetSystem() as dynamic)?.GUID as string);

                List<global::TPD.SystemComponent> systemComponents_In = Query.ConnectedSystemComponents(systemComponent_Temp, Direction.In);
                if (systemComponents_In == null || systemComponents_In.Count == 0)
                {
                    continue;
                }

                systemComponents_In.RemoveAll(x => (x as dynamic).Guid == (componentGroup as dynamic).Guid);

                List<global::TPD.SystemComponent> systemComponents_Out = Query.ConnectedSystemComponents(systemComponent_Temp, Direction.Out);
                if (systemComponents_Out == null || systemComponents_Out.Count == 0)
                {
                    continue;
                }

                systemComponents_Out.RemoveAll(x => (x as dynamic).Guid == (componentGroup as dynamic).Guid);

                foreach (global::TPD.SystemComponent systemComponent_In in systemComponents_In)
                {
                    Core.Systems.ISystemComponent systemComponent_In_SAM = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x?.Reference() == (systemComponent_In as dynamic).GUID);
                    if (systemComponent_In_SAM == null)
                    {
                        continue;
                    }

                    foreach (global::TPD.SystemComponent systemComponent_Out in systemComponents_Out)
                    {
                        Core.Systems.ISystemComponent systemComponent_Out_SAM = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x?.Reference() == (systemComponent_Out as dynamic).GUID);
                        if (systemComponent_Out_SAM == null)
                        {
                            continue;
                        }

                        ISystemConnection systemConnection = Connect(systemPlantRoom, systemComponent_In_SAM, systemComponent_Out_SAM, airSystem, Direction.Out);
                        if(systemConnection != null)
                        {
                            result.Add(systemConnection);
                        }
                    }
                }

            }

            return result;
        }

    }
}