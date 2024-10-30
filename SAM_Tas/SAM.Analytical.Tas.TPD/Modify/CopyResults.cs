using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static void CopyResults(this EnergyCentre energyCentre, SystemEnergyCentre systemEnergyCentre, int startHour, int endHour)
        {
            if(energyCentre == null || systemEnergyCentre == null)
            {
                return;
            }

            List<PlantRoom> plantRooms = energyCentre.PlantRooms();
            if (plantRooms != null)
            {
                List<SystemPlantRoom> systemPlantRooms = systemEnergyCentre.GetSystemPlantRooms();
                if(systemPlantRooms != null)
                {
                    foreach (PlantRoom plantRoom in plantRooms)
                    {
                        SystemPlantRoom systemPlantRoom = systemPlantRooms.Find(x => x.Name == plantRoom.Name);
                        if(systemPlantRoom == null)
                        {
                            continue;
                        }

                        CopyResults(plantRoom, systemPlantRoom, startHour, endHour);
                        systemEnergyCentre.Add(systemPlantRoom);
                    }
                }
            }

            double totalConsumption = 0;
            double CO2Emission = 0;
            double cost = 0;
            double unmetHours = 0;

            WrResultSet wrResultSet = (WrResultSet)energyCentre.GetResultSet(tpdResultsPeriod.tpdResultsPeriodAnnual, 0, 0, 0, null);

            int count;

            count = wrResultSet.GetVectorSize(tpdResultVectorType.tpdConsumption);
            for (int j = 1; j <= count; j++)
            {
                WrResultItem wrResultItem = (WrResultItem)wrResultSet.GetResultItem(tpdResultVectorType.tpdConsumption, j);
                if (wrResultItem != null)
                {
                    Array array = (Array)wrResultItem.GetValues();
                    if (array != null && array.Length != 0)
                    {
                        totalConsumption += (double)array.GetValue(0);
                    }
                }
            }

            count = wrResultSet.GetVectorSize(tpdResultVectorType.tpdCost);
            for (int j = 1; j <= count; j++)
            {
                WrResultItem wrResultItem = (WrResultItem)wrResultSet.GetResultItem(tpdResultVectorType.tpdCost, j);
                if (wrResultItem != null)
                {
                    Array array = (Array)wrResultItem.GetValues();
                    if (array != null && array.Length != 0)
                    {
                        cost += (double)array.GetValue(0);
                    }
                }
            }

            count = wrResultSet.GetVectorSize(tpdResultVectorType.tpdCo2);
            for (int j = 1; j <= count; j++)
            {
                WrResultItem wrResultItem = (WrResultItem)wrResultSet.GetResultItem(tpdResultVectorType.tpdCo2, j);
                if (wrResultItem != null)
                {
                    Array array = (Array)wrResultItem.GetValues();
                    if (array != null && array.Length != 0)
                    {
                        CO2Emission += (double)array.GetValue(0);
                    }
                }
            }

            count = wrResultSet.GetVectorSize(tpdResultVectorType.tpdUnmetHours);
            for (int j = 1; j <= count; j++)
            {
                WrResultItem wrResultItem = (WrResultItem)wrResultSet.GetResultItem(tpdResultVectorType.tpdUnmetHours, j);
                if (wrResultItem != null)
                {
                    Array array = (Array)wrResultItem.GetValues();
                    if (array != null && array.Length != 0)
                    {
                        unmetHours += (double)array.GetValue(0);
                    }
                }
            }

            wrResultSet.Dispose();

            systemEnergyCentre.SetValue(Systems.SystemEnergyCentreParameter.AnnualTotalConsumption, totalConsumption);
            systemEnergyCentre.SetValue(Systems.SystemEnergyCentreParameter.AnnualCO2Emission, CO2Emission);
            systemEnergyCentre.SetValue(Systems.SystemEnergyCentreParameter.AnnualCost, cost);
            systemEnergyCentre.SetValue(Systems.SystemEnergyCentreParameter.AnnualUnmetHours, unmetHours);
        }

        public static void CopyResults(this PlantRoom plantRoom, SystemPlantRoom systemPlantRoom, int startHour, int endHour)
        {
            if(plantRoom == null || systemPlantRoom == null)
            {
                return;
            }

            List<global::TPD.System> systems = plantRoom?.Systems();
            if (systems == null || systems.Count == 0)
            {
                return;
            }

            List<AirSystem> airSystems = systemPlantRoom.GetSystems<AirSystem>();
            if (airSystems == null || airSystems.Count == 0)
            {
                return;
            }

            foreach(global::TPD.System system in systems)
            {
                AirSystem airSystem = airSystems.Find(x => x.Name == system.Name);
                if(airSystem == null)
                {
                    continue; 
                }

                List<global::TPD.SystemComponent> systemComponents = system.SystemComponents<global::TPD.SystemComponent>(true);
                if(systemComponents == null || systemComponents.Count == 0)
                {
                    continue; 
                }

                List<Core.Systems.ISystemComponent> systemComponents_AirSystem = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(airSystem);

                foreach(global::TPD.SystemComponent systemComponent in systemComponents)
                {
                    string name = ((dynamic)systemComponent).Name;

                    if (systemComponent is SystemZone)
                    {
                        SystemSpace systemSpace = systemComponents_AirSystem.Find(x => x is SystemSpace && x.Reference() == systemComponent.Reference()) as SystemSpace;
                        if(systemSpace == null)
                        {
                            systemSpace = systemComponents_AirSystem.Find(x => x is SystemSpace && ((SystemSpace)x).Name == name) as SystemSpace;
                        }

                        if (systemSpace != null)
                        {
                            SystemZone systemZone = (SystemZone)(systemComponent as dynamic);

                            SystemSpaceResult systemSpaceResult = Convert.ToSAM_SpaceSystemResult(systemZone, systemPlantRoom, startHour, endHour);
                            if (systemSpaceResult != null)
                            {
                                systemPlantRoom.Add(systemSpaceResult);
                                systemPlantRoom.Connect(systemSpaceResult, systemSpace);
                            }

                            List<ZoneComponent> zoneComponents = systemZone.ZoneComponents<ZoneComponent>();
                            if(zoneComponents != null && zoneComponents.Count != 0)
                            {
                                List<ISystemSpaceComponent> systemSpaceComponents = systemPlantRoom.GetSystemSpaceComponents<ISystemSpaceComponent>(systemSpace);
                                if(systemSpaceComponents != null && systemSpaceComponents.Count != 0)
                                {
                                    foreach (ZoneComponent zoneComponent in zoneComponents)
                                    {
                                        ISystemComponentResult systemComponentResult = zoneComponent?.ToSAM_SystemComponentResult(startHour, endHour);
                                        if(systemComponentResult == null)
                                        {
                                            continue;
                                        }

                                        systemPlantRoom.Add(systemComponentResult);
                                        
                                        ISystemSpaceComponent systemSpaceComponent = systemSpaceComponents.Find(x => x.Reference() == systemComponentResult.Reference);
                                        if(systemSpaceComponent != null)
                                        {
                                            systemPlantRoom.Connect(systemSpaceComponent, systemSpace);
                                        }
                                    }
                                }
                            }

  
                        }
                    }
                    else if (systemComponent is ComponentGroup)
                    {
                        AirSystemGroup airSystemGroup = systemComponents_AirSystem.Find(x => x is AirSystemGroup && x.Reference() == systemComponent.Reference()) as AirSystemGroup;
                        if (airSystemGroup == null)
                        {
                            airSystemGroup = systemComponents_AirSystem.Find(x => x is AirSystemGroup && ((AirSystemGroup)x).Name == name) as AirSystemGroup;
                        }

                        if (airSystemGroup != null)
                        {
                            List<global::TPD.SystemComponent> systemComponent_ComponentGroup = Query.SystemComponents<global::TPD.SystemComponent>((ComponentGroup)systemComponent, true);
                        }
                    }
                    else if (systemComponent is Junction)
                    {
                        SystemAirJunction systemAirJunction = systemComponents_AirSystem.Find(x => x is SystemAirJunction && x.Reference() == systemComponent.Reference()) as SystemAirJunction;
                        if (systemAirJunction == null)
                        {
                            systemAirJunction = systemComponents_AirSystem.Find(x => x is SystemAirJunction && ((SystemAirJunction)x).Name == name) as SystemAirJunction;
                        }

                        if (systemAirJunction != null)
                        {
                            SystemAirJunctionResult systemAirJunctionResult = Convert.ToSAM_SystemAirJunctionResult((Junction)(systemComponent as dynamic), startHour, endHour);
                            if(systemAirJunctionResult != null)
                            {
                                systemPlantRoom.Add(systemAirJunctionResult);
                                systemPlantRoom.Connect(systemAirJunctionResult, systemAirJunction);
                            }
                        }
                    }
                    else if (systemComponent is Exchanger)
                    {
                        SystemExchanger systemExchanger = systemComponents_AirSystem.Find(x => x is SystemExchanger && x.Reference() == systemComponent.Reference()) as SystemExchanger;
                        if(systemExchanger == null)
                        {
                            systemExchanger = systemComponents_AirSystem.Find(x => x is SystemExchanger && ((SystemExchanger)x).Name == name) as SystemExchanger;
                        }

                        if (systemExchanger != null)
                        {
                            SystemExchangerResult systemExchangerResult = Convert.ToSAM_SystemExchangerResult((Exchanger)(systemComponent as dynamic), startHour, endHour);
                            if (systemExchangerResult != null)
                            {
                                systemPlantRoom.Add(systemExchangerResult);
                                systemPlantRoom.Connect(systemExchangerResult, systemExchanger);
                            }
                        }
                    }
                    else if (systemComponent is DesiccantWheel)
                    {
                        SystemDesiccantWheel systemDesiccantWheel = systemComponents_AirSystem.Find(x => x is SystemDesiccantWheel && x.Reference() == systemComponent.Reference()) as SystemDesiccantWheel;
                        if(systemDesiccantWheel != null)
                        {
                            systemDesiccantWheel = systemComponents_AirSystem.Find(x => x is SystemDesiccantWheel && ((SystemDesiccantWheel)x).Name == name) as SystemDesiccantWheel;
                        }

                        if (systemDesiccantWheel != null)
                        {
                            SystemDesiccantWheelResult systemDesiccantWheelResult = Convert.ToSAM_SystemDesiccantWheelResult((DesiccantWheel)(systemComponent as dynamic), startHour, endHour);
                            if (systemDesiccantWheelResult != null)
                            {
                                systemPlantRoom.Add(systemDesiccantWheelResult);
                                systemPlantRoom.Connect(systemDesiccantWheelResult, systemDesiccantWheel);
                            }
                        }
                    }
                    else if (systemComponent is global::TPD.Fan)
                    {
                        SystemFan systemFan = systemComponents_AirSystem.Find(x => x is SystemFan && x.Reference() == systemComponent.Reference()) as SystemFan;
                        if(systemFan == null)
                        {
                            systemFan = systemComponents_AirSystem.Find(x => x is SystemFan && ((SystemFan)x).Name == name) as SystemFan;
                        }
                        
                        if (systemFan != null)
                        {
                            SystemFanResult systemFanResult = Convert.ToSAM_SystemFanResult((global::TPD.Fan)(systemComponent as dynamic), startHour, endHour);
                            if (systemFanResult != null)
                            {
                                systemPlantRoom.Add(systemFanResult);
                                systemPlantRoom.Connect(systemFanResult, systemFan);
                            }
                        }
                    }
                    else if (systemComponent is global::TPD.HeatingCoil)
                    {
                        SystemHeatingCoil systemHeatingCoil = systemComponents_AirSystem.Find(x => x is SystemHeatingCoil && x.Reference() == systemComponent.Reference()) as SystemHeatingCoil;
                        if(systemHeatingCoil == null)
                        {
                            systemHeatingCoil = systemComponents_AirSystem.Find(x => x is SystemHeatingCoil && ((SystemHeatingCoil)x).Name == name) as SystemHeatingCoil;
                        }

                        if (systemHeatingCoil != null)
                        {
                            SystemHeatingCoilResult systemHeatingCoilResult = Convert.ToSAM_SystemHeatingCoilResult((global::TPD.HeatingCoil)(systemComponent as dynamic), startHour, endHour);
                            if (systemHeatingCoilResult != null)
                            {
                                systemPlantRoom.Add(systemHeatingCoilResult);
                                systemPlantRoom.Connect(systemHeatingCoilResult, systemHeatingCoil);
                            }
                        }
                    }
                    else if (systemComponent is global::TPD.CoolingCoil)
                    {
                        SystemCoolingCoil systemCoolingCoil = systemComponents_AirSystem.Find(x => x is SystemCoolingCoil && x.Reference() == systemComponent.Reference()) as SystemCoolingCoil;
                        if(systemCoolingCoil == null)
                        {
                            systemCoolingCoil = systemComponents_AirSystem.Find(x => x is SystemCoolingCoil && ((SystemCoolingCoil)x).Name == name) as SystemCoolingCoil;
                        }

                        if (systemCoolingCoil != null)
                        {
                            SystemCoolingCoilResult systemCoolingCoilResult = Convert.ToSAM_SystemCoolingCoilResult((global::TPD.CoolingCoil)(systemComponent as dynamic), startHour, endHour);
                            if (systemCoolingCoilResult != null)
                            {
                                systemPlantRoom.Add(systemCoolingCoilResult);
                                systemPlantRoom.Connect(systemCoolingCoilResult, systemCoolingCoil);
                            }
                        }
                    }
                    else if (systemComponent is Damper)
                    {

                    }
                    else if (systemComponent is Optimiser)
                    {
                        Optimiser optimiser = (Optimiser)(systemComponent as dynamic);
                        switch (optimiser.Flags)
                        {
                            case 1:
                                SystemEconomiser systemEconomiser = systemComponents_AirSystem.Find(x => x is SystemEconomiser && x.Reference() == systemComponent.Reference()) as SystemEconomiser;
                                if(systemEconomiser == null)
                                {
                                    systemEconomiser = systemComponents_AirSystem.Find(x => x is SystemEconomiser && ((SystemEconomiser)x).Name == name) as SystemEconomiser;
                                }

                                if (systemEconomiser != null)
                                {
                                    SystemEconomiserResult systemEconomiserResult = Convert.ToSAM_SystemEconomiserResult((Optimiser)(systemComponent as dynamic), startHour, endHour);
                                    if (systemEconomiserResult != null)
                                    {
                                        systemPlantRoom.Add(systemEconomiserResult);
                                        systemPlantRoom.Connect(systemEconomiserResult, systemEconomiser);
                                    }
                                }
                                break;

                            case 0:
                                SystemMixingBox systemMixingBox = systemComponents_AirSystem.Find(x => x is SystemMixingBox && x.Reference() == systemComponent.Reference()) as SystemMixingBox;
                                if(systemMixingBox == null)
                                {
                                    systemMixingBox = systemComponents_AirSystem.Find(x => x is SystemMixingBox && ((SystemMixingBox)x).Name == name) as SystemMixingBox;
                                }

                                if (systemMixingBox != null)
                                {
                                    SystemMixingBoxResult systemMixingBoxResult = Convert.ToSAM_SystemMixingBoxResult((Optimiser)(systemComponent as dynamic), startHour, endHour);
                                    if (systemMixingBoxResult != null)
                                    {
                                        systemPlantRoom.Add(systemMixingBoxResult);
                                        systemPlantRoom.Connect(systemMixingBoxResult, systemMixingBox);
                                    }
                                }
                                break;
                        }
                    }
                    else if (systemComponent is SteamHumidifier)
                    {
                        SystemSteamHumidifier systemSteamHumidifier = systemComponents_AirSystem.Find(x => x is SystemSteamHumidifier && x.Reference() == systemComponent.Reference()) as SystemSteamHumidifier;
                        if(systemSteamHumidifier == null)
                        {
                            systemSteamHumidifier = systemComponents_AirSystem.Find(x => x is SystemSteamHumidifier && ((SystemSteamHumidifier)x).Name == name) as SystemSteamHumidifier;
                        }

                        if (systemSteamHumidifier != null)
                        {
                            SystemSteamHumidifierResult systemSteamHumidifierResult = Convert.ToSAM_SystemSteamHumidifierResult((SteamHumidifier)(systemComponent as dynamic), startHour, endHour);
                            if (systemSteamHumidifierResult != null)
                            {
                                systemPlantRoom.Add(systemSteamHumidifierResult);
                                systemPlantRoom.Connect(systemSteamHumidifierResult, systemSteamHumidifier);
                            }
                        }
                    }
                    else if (systemComponent is SprayHumidifier)
                    {
                        SystemDirectEvaporativeCooler systemDirectEvaporativeCooler = systemComponents_AirSystem.Find(x => x is SystemDirectEvaporativeCooler && x.Reference() == systemComponent.Reference()) as SystemDirectEvaporativeCooler;
                        if(systemDirectEvaporativeCooler == null)
                        {
                            systemDirectEvaporativeCooler = systemComponents_AirSystem.Find(x => x is SystemDirectEvaporativeCooler && ((SystemCoolingCoil)x).Name == name) as SystemDirectEvaporativeCooler;
                        }

                        if (systemDirectEvaporativeCooler != null)
                        {
                            SystemDirectEvaporativeCoolerResult systemDirectEvaporativeCoolerResult = Convert.ToSAM_SystemDirectEvaporativeCoolerResult((SprayHumidifier)(systemComponent as dynamic), startHour, endHour);
                            if (systemDirectEvaporativeCoolerResult != null)
                            {
                                systemPlantRoom.Add(systemDirectEvaporativeCoolerResult);
                                systemPlantRoom.Connect(systemDirectEvaporativeCoolerResult, systemDirectEvaporativeCooler);
                            }
                        }
                        
                    }
                    else if (systemComponent is DXCoil)
                    {
                        SystemDXCoil systemDXCoil = systemComponents_AirSystem.Find(x => x is SystemDXCoil && x.Reference() == systemComponent.Reference()) as SystemDXCoil;
                        if(systemDXCoil == null)
                        {
                            systemDXCoil = systemComponents_AirSystem.Find(x => x is SystemDXCoil && ((SystemDXCoil)x).Name == name) as SystemDXCoil;
                        }

                        if (systemDXCoil != null)
                        {
                            SystemDXCoilResult systemDXCoilResult = Convert.ToSAM_SystemDXCoilResult((DXCoil)(systemComponent as dynamic), startHour, endHour);
                            if (systemDXCoilResult != null)
                            {
                                systemPlantRoom.Add(systemDXCoilResult);
                                systemPlantRoom.Connect(systemDXCoilResult, systemDXCoil);
                            }
                        }
                    }
                }


            }

        }

    }
}