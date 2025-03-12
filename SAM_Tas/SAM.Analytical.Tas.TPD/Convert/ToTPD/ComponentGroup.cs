//using SAM.Analytical.Systems;
//using SAM.Core.Systems;
//using SAM.Geometry.Systems;
//using System;
//using System.Collections.Generic;
//using TPD;

//namespace SAM.Analytical.Tas.TPD
//{
//    public static partial class Convert
//    {
//        public static ComponentGroup ToTPD(this DisplayAirSystemGroup displayAirSystemGroup, SystemPlantRoom systemPlantRoom, EnergyCentre energyCentre, global::TPD.System system, Controller[] controllers, DHWGroup dHWGroup, ElectricalGroup electricalGroup_SmallPower, ElectricalGroup electricalGroup_Lighting)
//        {
//            return ToTPD(displayAirSystemGroup, systemPlantRoom, energyCentre, system, controllers, dHWGroup, electricalGroup_SmallPower, electricalGroup_Lighting, out Dictionary<Guid, global::TPD.ISystemComponent> dictionary);
//        }

//        public static ComponentGroup ToTPD(this DisplayAirSystemGroup displayAirSystemGroup, SystemPlantRoom systemPlantRoom, EnergyCentre energyCentre, global::TPD.System system, Controller[] controllers, DHWGroup dHWGroup, ElectricalGroup electricalGroup_SmallPower, ElectricalGroup electricalGroup_Lighting, out Dictionary<Guid, global::TPD.ISystemComponent> dictionary)
//        {
//            dictionary = null;

//            if (displayAirSystemGroup == null || system == null || systemPlantRoom == null)
//            {
//                return null;
//            }

//            List<DisplaySystemSpace> displaySystemSpaces = systemPlantRoom.GetRelatedObjects<DisplaySystemSpace>(displayAirSystemGroup);
//            if(displaySystemSpaces == null || displaySystemSpaces.Count == 0)
//            {
//                return null;
//            }

//            List<ZoneLoad> zoneLoads = Query.ZoneLoads(energyCentre.GetTSDData(1), displaySystemSpaces);
//            if(zoneLoads == null || zoneLoads.Count == 0)
//            {
//                return null;
//            }

//            List<Core.Systems.SystemComponent> systemComponents = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(displayAirSystemGroup);
//            if(systemComponents == null || systemComponents.Count == 0)
//            {
//                return null; 
//            }
            
//            List<IDisplaySystemObject> displaySystemObjects = systemComponents.FindAll(x => x is IDisplaySystemObject).ConvertAll(x => (IDisplaySystemObject)x);
//            if(displaySystemObjects == null || displaySystemObjects.Count == 0)
//            {
//                return null;
//            }

//            dictionary = new Dictionary<Guid, global::TPD.ISystemComponent>();

//            List<global::TPD.SystemComponent> systemComponents_TPD = new List<global::TPD.SystemComponent>();
//            while(displaySystemObjects.Count > 0)
//            {
//                List<IDisplaySystemObject> displaySystemObjects_Type = displaySystemObjects.FindAll(x => x?.GetType() == displaySystemObjects[0]?.GetType());
//                displaySystemObjects.RemoveAll(x => displaySystemObjects_Type.Contains(x));

//                if(displaySystemObjects_Type.Count == 0)
//                {
//                    continue;
//                }

//                dynamic dynamic = Convert.ToTPD(displaySystemObjects_Type[0] as dynamic, system);
//                if(dynamic is global::TPD.SystemComponent)
//                {
//                    global::TPD.SystemComponent systemComponent_Temp = (global::TPD.SystemComponent)dynamic;
//                    foreach(IDisplaySystemObject displaySystemObject_Type in displaySystemObjects_Type)
//                    {
//                        dictionary[displaySystemObject_Type.Guid] = systemComponent_Temp;
//                    }

//                    systemComponents_TPD.Add(systemComponent_Temp);
//                }
//            }

//            Create.Ducts(systemPlantRoom, system, dictionary, out Dictionary<Guid, Duct> dictionary_Ducts);

//            ComponentGroup componentGroup = system.AddGroup(systemComponents_TPD.ToArray(), controllers);
//            componentGroup.SetMultiplicity(zoneLoads.Count);

//            List<global::TPD.SystemComponent> systemComponents_ComponentGroup = Query.SystemComponents<global::TPD.SystemComponent>(componentGroup);

//            List<SystemZone> systemZones = systemComponents_ComponentGroup.FindAll(x => x is SystemZone).ConvertAll(x => (SystemZone)x);
//            if (systemZones != null && systemZones.Count == zoneLoads.Count)
//            {
//                for (int i = 0; i < zoneLoads.Count; i++)
//                {
//                    dynamic systemZone_Group = systemZones[i];
//                    systemZone_Group.AddZoneLoad(zoneLoads[i]);
                    
//                    if(dHWGroup != null)
//                    {
//                        systemZone_Group.SetDHWGroup(dHWGroup);
//                    }
                    
//                    if(electricalGroup_SmallPower != null)
//                    {
//                        systemZone_Group.SetElectricalGroup1(electricalGroup_SmallPower);
//                    }
                    
//                    if(electricalGroup_Lighting != null)
//                    {
//                        systemZone_Group.SetElectricalGroup2(electricalGroup_Lighting);
//                    }

//                    systemZone_Group.FlowRate.Type = tpdSizedVariable.tpdSizedVariableSize;
//                    systemZone_Group.FlowRate.Method = tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
//                    //systemZone_Group.FlowRate.Value = 100;
//                    for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
//                    {
//                        systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
//                    }

//                    //systemZone_Group.DisplacementVent = displacementVent ? 1 : 0;

//                    systemZone_Group.FreshAir.Type = tpdSizedVariable.tpdSizedVariableSize;
//                    systemZone_Group.FreshAir.Method = tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
//                    //systemZone_Group.FreshAir.Value = 100;
//                    for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
//                    {
//                        systemZone_Group.FreshAir.AddDesignCondition(energyCentre.GetDesignCondition(j));
//                    }
//                }
//            }

//            return componentGroup;
           
//        }
//    }
//}
