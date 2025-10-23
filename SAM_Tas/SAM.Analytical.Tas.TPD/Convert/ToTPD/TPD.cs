using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static bool ToTPD(this SystemEnergyCentre systemEnergyCentre, string path_TPD, string path_TSD, SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = null)
        {
            if (systemEnergyCentre == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path_TPD) || string.IsNullOrWhiteSpace(path_TSD))
            {
                return false;
            }

            if (!System.IO.File.Exists(path_TSD))
            {
                return false;
            }

            if (System.IO.File.Exists(path_TPD))
            {
                System.IO.File.Delete(path_TPD);
            }

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {
                TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                if (tPDDoc == null)
                {
                    return false;
                }

                EnergyCentre energyCentre = tPDDoc.EnergyCentre;
                energyCentre.AddTSDData(path_TSD, 1);

                TSDData tSDData = energyCentre.GetTSDData(1);

                ToTPD(systemEnergyCentre, tPDDoc);

                if (systemEnergyCentreConversionSettings == null)
                {
                    systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings();
                }

                if (systemEnergyCentreConversionSettings.Simulate)
                {
                    foreach(PlantRoom plantRoom in energyCentre.PlantRooms())
                    {
                        plantRoom.SimulateEx(systemEnergyCentreConversionSettings.StartHour + 1, systemEnergyCentreConversionSettings.EndHour + 1, 0, energyCentre.ExternalPollutant.Value, 10.0, (int)tpdSimulationData.tpdSimulationDataLoad + (int)tpdSimulationData.tpdSimulationDataPipe + (int)tpdSimulationData.tpdSimulationDataDuct + (int)tpdSimulationData.tpdSimulationDataSimEvents + (int)tpdSimulationData.tpdSimulationDataCont, 1, 0);
                    }

                    if (systemEnergyCentreConversionSettings.IncludeComponentResults)
                    {
                        Modify.CopyResults(energyCentre, systemEnergyCentre, systemEnergyCentreConversionSettings.StartHour + 1, systemEnergyCentreConversionSettings.EndHour + 1);
                    }
                }

                tPDDoc.Save();
            }

            return true;
        }

        public static bool ToTPD(this SystemEnergyCentre systemEnergyCentre, TPDDoc tPDDoc)
        {
            EnergyCentre energyCentre = tPDDoc.EnergyCentre;

            AnalyticalSystemsProperties analyticalSystemsProperties = systemEnergyCentre.GetValue<AnalyticalSystemsProperties>(SystemEnergyCentreParameter.AnalyticalSystemsProperties);
            if (analyticalSystemsProperties != null)
            {
                List<ISchedule> schedules = analyticalSystemsProperties.Schedules;
                if (schedules != null)
                {
                    foreach (ISchedule schedule in schedules)
                    {
                        PlantSchedule plantSchedule = energyCentre.Add(schedule);
                    }
                }

                List<FluidType> fluidTypes = analyticalSystemsProperties.FluidTypes;
                if (fluidTypes != null)
                {
                    foreach (FluidType fluidType in fluidTypes)
                    {
                        fluid fluid = energyCentre.Add(fluidType);
                    }
                }

                List<DesignCondition> designConditions = analyticalSystemsProperties.DesignConditions;
                if (designConditions != null)
                {
                    foreach (DesignCondition designCondition in designConditions)
                    {
                        DesignConditionLoad designConditionLoad = energyCentre.Add(designCondition);
                    }
                }
            }

            List<SystemEnergySource> systemEnergySources = systemEnergyCentre.GetSystemEnergySources();
            if (systemEnergySources != null && systemEnergySources.Count != 0)
            {
                foreach (SystemEnergySource systemEnergySource in systemEnergySources)
                {
                    FuelSource fuelSource = systemEnergySource.ToTPD(energyCentre);
                }
            }

            List<SystemPlantRoom> systemPlantRooms = systemEnergyCentre.GetSystemPlantRooms();
            if (systemPlantRooms != null && systemPlantRooms.Count != 0)
            {
                foreach (SystemPlantRoom systemPlantRoom in systemPlantRooms)
                {
                    PlantRoom plantRoom = energyCentre.PlantRoom(systemPlantRoom.Name);
                    if (plantRoom == null)
                    {
                        plantRoom = energyCentre.AddPlantRoom();
                        plantRoom.Name = systemPlantRoom.Name;
                    }

                    List<Core.Systems.SystemLabel> systemLabels = systemPlantRoom.GetSystemObjects<Core.Systems.SystemLabel>();

                    List<LiquidSystem> liquidSystems = systemPlantRoom.GetSystems<LiquidSystem>();
                    if (liquidSystems != null && liquidSystems.Count != 0)
                    {
                        foreach (LiquidSystem liquidSystem in liquidSystems)
                        {
                            if (liquidSystem.GetType() != typeof(LiquidSystem))
                            {
                                continue;
                            }

                            Dictionary<Guid, PlantComponent> dictionary_SystemComponents_TPD = new Dictionary<Guid, PlantComponent>();
                            Dictionary<Guid, Core.Systems.ISystemComponent> dictionary_SystemComponents_SAM = new Dictionary<Guid, Core.Systems.ISystemComponent>();

                            Dictionary<Guid, PlantController> dictionary_PlantController = new Dictionary<Guid, PlantController>();

                            List<Core.Systems.ISystemComponent> systemComponents = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(liquidSystem, ConnectorStatus.Undefined, Direction.Out);
                            if (systemComponents == null || systemComponents.Count == 0)
                            {
                                systemComponents = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(liquidSystem);
                            }

                            List<ISystemCollection> systemCollections = systemPlantRoom.GetSystemComponents<ISystemCollection>(liquidSystem);
                            if (systemCollections != null)
                            {
                                foreach (ISystemCollection systemCollection in systemCollections)
                                {
                                    if (systemCollection == null)
                                    {
                                        continue;
                                    }

                                    PlantComponent plantComponent_TPD = ToTPD(systemCollection as dynamic, plantRoom) as PlantComponent;

                                    dictionary_SystemComponents_TPD[(systemCollection as dynamic).Guid] = plantComponent_TPD;
                                    systemCollection.SetReference(Query.Reference(plantComponent_TPD));
                                    systemPlantRoom.Add(systemCollection);

                                    if (systemComponents != null)
                                    {
                                        systemComponents.RemoveAll(x => x is ISystemCollection && ((dynamic)x).Guid == ((dynamic)x).Guid);
                                    }
                                }
                            }

                            if (systemComponents == null || systemComponents.Count == 0)
                            {
                                continue;
                            }

                            foreach (Core.Systems.ISystemComponent systemComponents_Temp in systemComponents)
                            {
                                dictionary_SystemComponents_SAM[systemPlantRoom.GetGuid(systemComponents_Temp)] = systemComponents_Temp;
                            }

                            foreach (Core.Systems.SystemComponent systemComponent_Temp in dictionary_SystemComponents_SAM.Values)
                            {
                                PlantComponent plantComponent_TPD = null;

                                if (systemComponent_Temp is DisplaySystemLiquidJunction)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemLiquidJunction)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplayElectricalSystemCollection)
                                {
                                    plantComponent_TPD = ToTPD((DisplayElectricalSystemCollection)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplayFuelSystemCollection)
                                {
                                    plantComponent_TPD = ToTPD((DisplayFuelSystemCollection)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplayCoolingSystemCollection)
                                {
                                    plantComponent_TPD = ToTPD((DisplayCoolingSystemCollection)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplayDomesticHotWaterSystemCollection)
                                {
                                    plantComponent_TPD = ToTPD((DisplayDomesticHotWaterSystemCollection)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplayHeatingSystemCollection)
                                {
                                    plantComponent_TPD = ToTPD((DisplayHeatingSystemCollection)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplayRefrigerantSystemCollection)
                                {
                                    plantComponent_TPD = ToTPD((DisplayRefrigerantSystemCollection)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemAbsorptionChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemAbsorptionChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemWaterSourceAbsorptionChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemWaterSourceAbsorptionChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemAirSourceChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemAirSourceChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemAirSourceDirectAbsorptionChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemAirSourceDirectAbsorptionChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemAirSourceHeatPump)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemAirSourceHeatPump)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemBoiler)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemBoiler)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemCHP)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemCHP)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemCoolingTower)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemCoolingTower)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemDryCooler)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemDryCooler)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemHorizontalExchanger)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemHorizontalExchanger)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemIceStorageChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemIceStorageChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemWaterSourceIceStorageChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemWaterSourceIceStorageChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemLiquidExchanger)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemLiquidExchanger)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemMultiChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemMultiChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemMultiBoiler)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemMultiBoiler)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemPipeLossComponent)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemPipeLossComponent)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemPump)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemPump)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemSlinkyCoil)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemSlinkyCoil)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemSolarPanel)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemSolarPanel)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemSurfaceWaterExchanger)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemSurfaceWaterExchanger)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemTank)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemTank)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemValve)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemValve)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemVerticalBorehole)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemVerticalBorehole)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemWaterSourceChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemWaterSourceChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemWaterSourceDirectAbsorptionChiller)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemWaterSourceDirectAbsorptionChiller)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemWaterSourceHeatPump)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemWaterSourceHeatPump)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemWaterToWaterHeatPump)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemWaterToWaterHeatPump)systemComponent_Temp, plantRoom) as PlantComponent;
                                }
                                else if (systemComponent_Temp is DisplaySystemFourPipeHeatPump)
                                {
                                    plantComponent_TPD = ToTPD((DisplaySystemFourPipeHeatPump)systemComponent_Temp, plantRoom) as PlantComponent;
                                }

                                if (plantComponent_TPD == null)
                                {
                                    continue;
                                }

                                dictionary_SystemComponents_TPD[systemComponent_Temp.Guid] = plantComponent_TPD;
                                systemComponent_Temp.SetReference(Query.Reference(plantComponent_TPD));
                                systemPlantRoom.Add(systemComponent_Temp);
                            }

                            Create.Pipes(systemPlantRoom, plantRoom, dictionary_SystemComponents_TPD, out Dictionary<Guid, Pipe> dictionary_Pipes);
                            dictionary_PlantController = Create.PlantControllers(systemPlantRoom, plantRoom, liquidSystem, dictionary_SystemComponents_TPD, dictionary_Pipes);

                            if(systemLabels != null)
                            {
                                foreach (Core.Systems.SystemLabel systemLabel in systemLabels)
                                {
                                    PlantComponent plantComponent = null;
                                    PlantController plantController = null;

                                    ISystemJSAMObject systemJSAMObject = systemPlantRoom.GetRelatedObjects<ISystemJSAMObject>(systemLabel)?.FirstOrDefault();
                                    if (systemJSAMObject != null)
                                    {
                                        if (systemJSAMObject is ISystemConnection && dictionary_Pipes.TryGetValue(((ISystemConnection)systemJSAMObject).Guid, out Pipe pipe) && pipe != null)
                                        {
                                            systemLabel.ToTPD(pipe);
                                        }
                                        else if (systemJSAMObject is ISystemController && dictionary_PlantController.TryGetValue((systemJSAMObject as dynamic).Guid, out plantController) && plantController != null)
                                        {
                                            systemLabel.ToTPD(plantController);
                                        }
                                        else if (systemJSAMObject is Core.Systems.ISystemComponent && dictionary_SystemComponents_TPD.TryGetValue((systemJSAMObject as dynamic).Guid, out plantComponent) && plantComponent != null)
                                        {
                                            systemLabel.ToTPD(plantComponent);
                                        }
                                    }
                                }
                            }


                        }
                    }

                    List<Core.Systems.ISystemComponent> systemComponents_All = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>();
                    if (systemComponents_All != null)
                    {
                        foreach (Core.Systems.ISystemComponent systemComponent_Temp in systemComponents_All)
                        {
                            List<Core.Systems.ISystem> systems = systemPlantRoom.GetRelatedObjects<Core.Systems.ISystem>(systemComponent_Temp);
                            if (systems != null && systems.Count != 0)
                            {
                                continue;
                            }

                            PlantComponent plantComponent_TPD = null;

                            if (systemComponent_Temp is DisplaySystemWindTurbine)
                            {
                                plantComponent_TPD = ToTPD((DisplaySystemWindTurbine)systemComponent_Temp, plantRoom) as PlantComponent;
                            }
                            else if (systemComponent_Temp is DisplaySystemPhotovoltaicPanel)
                            {
                                plantComponent_TPD = ToTPD((DisplaySystemPhotovoltaicPanel)systemComponent_Temp, plantRoom) as PlantComponent;
                            }

                            if (plantComponent_TPD == null)
                            {
                                continue;
                            }

                            systemComponent_Temp.SetReference(Query.Reference(plantComponent_TPD));
                            systemPlantRoom.Add(systemComponent_Temp);
                        }
                    }

                    List<AirSystem> airSystems = systemPlantRoom.GetSystems<AirSystem>();
                    if (airSystems != null && airSystems.Count != 0)
                    {
                        foreach (AirSystem airSystem in airSystems)
                        {
                            Dictionary<Guid, HashSet<int>> dictionary_AirSystemGroup = new Dictionary<Guid, HashSet<int>>();

                            Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponent = new Dictionary<Guid, global::TPD.ISystemComponent>();

                            Dictionary<Guid, Controller> dictionary_Controller = new Dictionary<Guid, Controller>();

                            global::TPD.System system = airSystem.ToTPD(plantRoom);
                            if (system == null)
                            {
                                continue;
                            }

                            Modify.SetReference(airSystem, system.Reference());
                            systemPlantRoom.Add(airSystem);

                            List<Core.Systems.SystemComponent> systemComponents_AirSystem = systemPlantRoom.GetSystemComponents<Core.Systems.SystemComponent>(airSystem);
                            if (systemComponents_AirSystem != null && systemComponents_AirSystem.Count != 0)
                            {
                                systemComponents_AirSystem.RemoveAll(x => x is ISystemController || x is ISystemConnection);

                                foreach (Core.Systems.SystemComponent systemComponent_Temp in systemComponents_AirSystem)
                                {
                                    AirSystemGroup airSystemGroup = systemPlantRoom.GetRelatedObjects<AirSystemGroup>(systemComponent_Temp)?.FirstOrDefault();
                                    if (airSystemGroup != null)
                                    {
                                        if (!dictionary_AirSystemGroup.TryGetValue(airSystemGroup.Guid, out HashSet<int> groupIndexes))
                                        {
                                            groupIndexes = new HashSet<int>();
                                            dictionary_AirSystemGroup[airSystemGroup.Guid] = groupIndexes;
                                        }

                                        if (((dynamic)systemComponent_Temp).TryGetValue(AirSystemComponentParameter.GroupIndex, out int groupIndex))
                                        {
                                            if (groupIndexes.Contains(groupIndex))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                groupIndexes.Add(groupIndex);
                                            }
                                        }
                                    }

                                    global::TPD.ISystemComponent systemComponent_TPD = null;

                                    if (systemComponent_Temp is DisplaySystemAirJunction)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemAirJunction)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemCoolingCoil)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemCoolingCoil)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemHeatingCoil)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemHeatingCoil)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemExchanger)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemExchanger)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemFan)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemFan)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemDamper)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemDamper)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemSpace)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemSpace)systemComponent_Temp, systemPlantRoom, system, null, airSystemGroup == null) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemEconomiser)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemEconomiser)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemSprayHumidifier)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemSprayHumidifier)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemSteamHumidifier)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemSteamHumidifier)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemDesiccantWheel)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemDesiccantWheel)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemDXCoil)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemDXCoil)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemMixingBox)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemMixingBox)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemLoadComponent)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemLoadComponent)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Temp is DisplaySystemDirectEvaporativeCooler)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemDirectEvaporativeCooler)systemComponent_Temp, system) as global::TPD.ISystemComponent;
                                    }

                                    if (systemComponent_TPD == null)
                                    {
                                        continue;
                                    }

                                    dictionary_SystemComponent[systemComponent_Temp.Guid] = systemComponent_TPD;
                                    systemComponent_Temp.SetReference(Query.Reference(systemComponent_TPD));
                                    systemPlantRoom.Add(systemComponent_Temp);
                                }

                                Create.Ducts(systemPlantRoom, system, dictionary_SystemComponent, out Dictionary<Guid, Duct> dictionary_Ducts);
                                dictionary_Controller = Create.Controllers(systemPlantRoom, system, airSystem, dictionary_SystemComponent, dictionary_Ducts, false);

                                if(systemLabels != null)
                                {
                                    foreach (Core.Systems.SystemLabel systemLabel in systemLabels)
                                    {
                                        ISystemJSAMObject systemJSAMObject = systemPlantRoom.GetRelatedObjects<ISystemJSAMObject>(systemLabel)?.FirstOrDefault();
                                        if (systemJSAMObject != null)
                                        {
                                            global::TPD.ISystemComponent systemComponent = null;
                                            Controller controller = null;

                                            if (systemJSAMObject is ISystemConnection && dictionary_Ducts != null && dictionary_Ducts != null && dictionary_Ducts.TryGetValue(((ISystemConnection)systemJSAMObject).Guid, out Duct duct) && duct != null)
                                            {
                                                systemLabel.ToTPD(duct);
                                            }
                                            else if (systemJSAMObject is ISystemController && dictionary_Controller != null && dictionary_Controller != null && dictionary_Controller.TryGetValue((systemJSAMObject as dynamic).Guid, out controller) && controller != null)
                                            {
                                                systemLabel.ToTPD(controller);
                                            }
                                            else if (systemJSAMObject is Core.Systems.ISystemComponent && dictionary_SystemComponent != null && dictionary_SystemComponent != null && dictionary_SystemComponent.TryGetValue((systemJSAMObject as dynamic).Guid, out systemComponent) && systemComponent != null)
                                            {
                                                systemLabel.ToTPD(systemComponent);
                                            }
                                        }
                                    }
                                }
                            }

                            List<AirSystemGroup> airSystemGroups = systemPlantRoom.GetSystemGroups<AirSystemGroup>(airSystem);
                            if (airSystemGroups != null)
                            {
                                foreach (AirSystemGroup airSystemGroup in airSystemGroups)
                                {
                                    SortedDictionary<int, List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>>> sortedDictionary_SystemComponent = new SortedDictionary<int, List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>>>();
                                    SortedDictionary<int, List<Tuple<SystemController, Controller>>> sortedDictionary_Controller = new SortedDictionary<int, List<Tuple<SystemController, Controller>>>();

                                    List<Core.Systems.SystemComponent> systemComponents_SAM = systemPlantRoom.GetSystemComponents<Core.Systems.SystemComponent>(airSystemGroup);
                                    if (systemComponents_SAM != null)
                                    {
                                        systemComponents_SAM.RemoveAll(x => x is ISystemConnection || x is ISystemController);

                                        for (int i = systemComponents_SAM.Count - 1; i >= 0; i--)
                                        {
                                            Core.Systems.ISystemComponent systemComponent_SAM_Temp = systemComponents_SAM[i];

                                            int groupIndex = -1;

                                            if (!(systemComponent_SAM_Temp is IAirSystemComponent) || !(((dynamic)systemComponent_SAM_Temp).TryGetValue(AirSystemComponentParameter.GroupIndex, out groupIndex)))
                                            {
                                                continue;
                                            }

                                            if (!sortedDictionary_SystemComponent.TryGetValue(groupIndex, out List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>> tuples))
                                            {
                                                tuples = new List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>>();
                                                sortedDictionary_SystemComponent[groupIndex] = tuples;
                                            }

                                            if (!dictionary_SystemComponent.TryGetValue((systemComponent_SAM_Temp as dynamic).Guid, out global::TPD.ISystemComponent systemComponent_TPD_Temp))
                                            {
                                                systemComponent_TPD_Temp = null;
                                            }

                                            tuples.Add(new Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>(systemComponent_SAM_Temp, systemComponent_TPD_Temp));

                                            systemComponents_SAM.RemoveAt(i);
                                        }

                                        for (int i = systemComponents_SAM.Count - 1; i >= 0; i--)
                                        {
                                            Core.Systems.ISystemComponent systemComponent_SAM_Temp = systemComponents_SAM[i];

                                            int groupIndex = sortedDictionary_SystemComponent.Count == 0 ? 0 : sortedDictionary_SystemComponent.Keys.Max();

                                            if (!sortedDictionary_SystemComponent.TryGetValue(groupIndex, out List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>> tuples))
                                            {
                                                tuples = new List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>>();
                                                sortedDictionary_SystemComponent[groupIndex] = tuples;
                                            }

                                            if (!dictionary_SystemComponent.TryGetValue((systemComponent_SAM_Temp as dynamic).Guid, out global::TPD.ISystemComponent systemComponent_TPD_Temp))
                                            {
                                                systemComponent_TPD_Temp = null;
                                            }

                                            tuples.Add(new Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>(systemComponent_SAM_Temp, systemComponent_TPD_Temp));

                                            systemComponents_SAM.RemoveAt(i);
                                        }
                                    }

                                    int count = 1;

                                    List<global::TPD.ISystemComponent> systemComponents_TPD = new List<global::TPD.ISystemComponent>();
                                    foreach (List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>> tuples in sortedDictionary_SystemComponent.Values)
                                    {
                                        if (tuples == null || tuples.Count == 0)
                                        {
                                            continue;
                                        }

                                        global::TPD.ISystemComponent systemComponent_TPD_Temp = tuples.Find(x => x.Item2 != null)?.Item2;
                                        if (systemComponent_TPD_Temp == null)
                                        {
                                            continue;
                                        }

                                        systemComponents_TPD.Add(systemComponent_TPD_Temp);

                                        if (systemComponent_TPD_Temp is SystemZone)
                                        {
                                            count = tuples.Count;
                                        }

                                    }

                                    if (dictionary_Controller != null)
                                    {
                                        List<SystemController> systemControllers_SAM = systemPlantRoom.GetSystemComponents<SystemController>(airSystemGroup);
                                        if (systemControllers_SAM != null)
                                        {
                                            for (int i = systemControllers_SAM.Count - 1; i >= 0; i--)
                                            {
                                                SystemController systemController_SAM_Temp = systemControllers_SAM[i];

                                                int groupIndex = -1;

                                                if (!systemController_SAM_Temp.TryGetValue(SAM.Analytical.Systems.SystemControllerParameter.GroupIndex, out groupIndex))
                                                {
                                                    continue;
                                                }

                                                if (!sortedDictionary_Controller.TryGetValue(groupIndex, out List<Tuple<SystemController, Controller>> tuples))
                                                {
                                                    tuples = new List<Tuple<SystemController, Controller>>();
                                                    sortedDictionary_Controller[groupIndex] = tuples;
                                                }

                                                if (!dictionary_Controller.TryGetValue(systemController_SAM_Temp.Guid, out Controller controller_TPD_Temp))
                                                {
                                                    controller_TPD_Temp = null;
                                                }

                                                tuples.Add(new Tuple<SystemController, Controller>(systemController_SAM_Temp, controller_TPD_Temp));

                                                systemControllers_SAM.RemoveAt(i);
                                            }

                                            for (int i = systemControllers_SAM.Count - 1; i >= 0; i--)
                                            {
                                                SystemController systemController_SAM_Temp = systemControllers_SAM[i];

                                                int groupIndex = sortedDictionary_Controller.Count == 0 ? 0 : sortedDictionary_Controller.Keys.Max();

                                                if (!sortedDictionary_Controller.TryGetValue(groupIndex, out List<Tuple<SystemController, Controller>> tuples))
                                                {
                                                    tuples = new List<Tuple<SystemController, Controller>>();
                                                    sortedDictionary_Controller[groupIndex] = tuples;
                                                }

                                                if (!dictionary_Controller.TryGetValue(systemController_SAM_Temp.Guid, out Controller controller_TPD_Temp))
                                                {
                                                    controller_TPD_Temp = null;
                                                }

                                                tuples.Add(new Tuple<SystemController, Controller>(systemController_SAM_Temp, controller_TPD_Temp));

                                                systemControllers_SAM.RemoveAt(i);
                                            }

                                        }
                                    }


                                    List<Controller> controllers_TPD = new List<Controller>();
                                    foreach (List<Tuple<SystemController, Controller>> tuples in sortedDictionary_Controller.Values)
                                    {
                                        if (tuples == null || tuples.Count == 0)
                                        {
                                            continue;
                                        }

                                        Controller controller_TPD_Temp = tuples.Find(x => x.Item2 != null)?.Item2;
                                        if (controller_TPD_Temp == null)
                                        {
                                            continue;
                                        }

                                        controllers_TPD.Add(controller_TPD_Temp);
                                    }

                                    if (systemComponents_TPD.Count > 0)
                                    {
                                        ComponentGroup componentGroup = system.AddGroup(systemComponents_TPD.ToArray(), controllers_TPD.ToArray());
                                        airSystemGroup.SetReference(componentGroup.Reference());
                                        systemPlantRoom.Add(airSystemGroup);

                                        componentGroup.SetMultiplicity(count);

                                        int index = 0;

                                        List<global::TPD.SystemComponent> systemComponents_TPD_New = Query.SystemComponents<global::TPD.SystemComponent>(componentGroup, false, false);

                                        count = systemComponents_TPD_New.Count / componentGroup.GetMultiplicity();

                                        for (int i = 0; i < systemComponents_TPD_New.Count; i++)
                                        {
                                            global::TPD.SystemComponent systemComponent_TPD_New = systemComponents_TPD_New[i];

                                            if (sortedDictionary_SystemComponent.TryGetValue(index, out List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>> tuples) && tuples != null && tuples.Count != 0)
                                            {
                                                int index_Temp = tuples.FindIndex(x => (x.Item2 as dynamic)?.GUID == ((dynamic)systemComponent_TPD_New).GUID);
                                                if (index_Temp != -1)
                                                {
                                                    Core.Systems.ISystemComponent systemComponent_SAM = tuples[index_Temp].Item1;
                                                    if (systemComponent_SAM is DisplaySystemSpace && systemComponent_TPD_New is SystemZone)
                                                    {
                                                        Modify.AddSystemZoneComponents((SystemZone)systemComponent_TPD_New, (DisplaySystemSpace)systemComponent_SAM, systemPlantRoom);
                                                    }

                                                    tuples.RemoveAt(index_Temp);
                                                }
                                                else
                                                {
                                                    Core.Systems.ISystemComponent systemComponent_SAM = tuples[0].Item1;
                                                    tuples.RemoveAt(0);

                                                    if (systemComponent_SAM is DisplaySystemSpace && systemComponent_TPD_New is SystemZone)
                                                    {
                                                        ToTPD((DisplaySystemSpace)systemComponent_SAM, systemPlantRoom, system, (SystemZone)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemAirJunction && systemComponent_TPD_New is Junction)
                                                    {
                                                        ToTPD((DisplaySystemAirJunction)systemComponent_SAM, system, (Junction)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemCoolingCoil && systemComponent_TPD_New is global::TPD.CoolingCoil)
                                                    {
                                                        ToTPD((DisplaySystemCoolingCoil)systemComponent_SAM, system, (global::TPD.CoolingCoil)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemDamper && systemComponent_TPD_New is Damper)
                                                    {
                                                        ToTPD((DisplaySystemDamper)systemComponent_SAM, system, (Damper)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemDesiccantWheel && systemComponent_TPD_New is DesiccantWheel)
                                                    {
                                                        ToTPD((DisplaySystemDesiccantWheel)systemComponent_SAM, system, (DesiccantWheel)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemDXCoil && systemComponent_TPD_New is DXCoil)
                                                    {
                                                        ToTPD((DisplaySystemDXCoil)systemComponent_SAM, system, (DXCoil)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemExchanger && systemComponent_TPD_New is Exchanger)
                                                    {
                                                        ToTPD((DisplaySystemExchanger)systemComponent_SAM, system, (Exchanger)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemFan && systemComponent_TPD_New is global::TPD.Fan)
                                                    {
                                                        ToTPD((DisplaySystemFan)systemComponent_SAM, system, (global::TPD.Fan)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemLoadComponent && systemComponent_TPD_New is LoadComponent)
                                                    {
                                                        ToTPD((DisplaySystemLoadComponent)systemComponent_SAM, system, (LoadComponent)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemEconomiser && systemComponent_TPD_New is Optimiser)
                                                    {
                                                        ToTPD((DisplaySystemEconomiser)systemComponent_SAM, system, (Optimiser)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemMixingBox && systemComponent_TPD_New is Optimiser)
                                                    {
                                                        ToTPD((DisplaySystemMixingBox)systemComponent_SAM, system, (Optimiser)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemSprayHumidifier && systemComponent_TPD_New is SprayHumidifier)
                                                    {
                                                        ToTPD((DisplaySystemSprayHumidifier)systemComponent_SAM, system, (SprayHumidifier)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemSteamHumidifier && systemComponent_TPD_New is SteamHumidifier)
                                                    {
                                                        ToTPD((DisplaySystemSteamHumidifier)systemComponent_SAM, system, (SteamHumidifier)systemComponent_TPD_New);
                                                    }
                                                    else if (systemComponent_SAM is DisplaySystemDirectEvaporativeCooler && systemComponent_TPD_New is SprayHumidifier)
                                                    {
                                                        ToTPD((DisplaySystemDirectEvaporativeCooler)systemComponent_SAM, system, (SprayHumidifier)systemComponent_TPD_New);
                                                    }
                                                }
                                            }

                                            index++;
                                            if (index >= count)
                                            {
                                                index = 0;
                                            }
                                        }


                                        List<Controller> controllers_TPD_New = Query.Controllers(componentGroup)?.FindAll(x => x.ControlType != tpdControlType.tpdControlGroup);

                                        count = controllers_TPD_New.Count / componentGroup.GetMultiplicity();

                                        List<Tuple<Controller, IDisplaySystemController>> tuples_ControlType = new List<Tuple<Controller, IDisplaySystemController>>();
                                        for (int i = 0; i < controllers_TPD_New.Count; i++)
                                        {
                                            Controller controller_TPD_New = controllers_TPD_New[i];

                                            if (sortedDictionary_Controller.TryGetValue(index, out List<Tuple<SystemController, Controller>> tuples) && tuples != null && tuples.Count != 0)
                                            {
                                                int index_Controller = tuples.FindIndex(x => x.Item2 == controller_TPD_New);
                                                if (index_Controller == -1)
                                                {
                                                    index_Controller = tuples.FindIndex(x => x.Item2 == null);
                                                }

                                                if (index_Controller != -1)
                                                {
                                                    Tuple<SystemController, Controller> tuple = tuples[index_Controller];
                                                    if (tuple.Item2 == null)
                                                    {
                                                        IDisplaySystemController displaySystemController = tuple.Item1 as IDisplaySystemController;
                                                        if (displaySystemController != null)
                                                        {
                                                            Controller controller = ToTPD(displaySystemController, system, controller_TPD_New);
                                                            if (controller != null)
                                                            {
                                                                tuples_ControlType.Add(new Tuple<Controller, IDisplaySystemController>(controller, displaySystemController));
                                                            }
                                                        }
                                                    }

                                                    tuples.RemoveAt(index_Controller);
                                                }
                                            }

                                            index++;
                                            if (index >= count)
                                            {
                                                index = 0;
                                            }
                                        }

                                        foreach (Tuple<Controller, IDisplaySystemController> tuple_ControlType in tuples_ControlType)
                                        {
                                            IDisplaySystemController displaySystemController = tuple_ControlType.Item2;
                                            Controller controller = tuple_ControlType.Item1;

                                            controller?.SetControlType(displaySystemController);
                                        }
                                    }
                                }
                            }
                        }

                        systemEnergyCentre.Add(systemPlantRoom);
                    }

                }
            }

            return true;
        }

        public static bool ToTPD(this SystemEnergyCentre systemEnergyCentre, string path_TPD)
        {
            if (systemEnergyCentre == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                return false;
            }

            if (System.IO.File.Exists(path_TPD))
            {
                System.IO.File.Delete(path_TPD);
            }

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {
                TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                if (tPDDoc == null)
                {
                    return false;
                }

                ToTPD(systemEnergyCentre, tPDDoc);

                tPDDoc.Save();
            }

            return true;
        }
    }
}