using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Drawing;
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


                PlantRoom plantRoom = energyCentre.PlantRoom("Main PlantRoom");
                if (plantRoom == null)
                {
                    plantRoom = energyCentre.AddPlantRoom();
                    plantRoom.Name = "Main PlantRoom";
                }

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
                    if(designConditions != null)
                    {
                        foreach(DesignCondition designCondition in designConditions)
                        {
                            DesignConditionLoad designConditionLoad = energyCentre.Add(designCondition);
                        }
                    }
                }

                List<SystemEnergySource> systemEnergySources = systemEnergyCentre.GetSystemEnergySources();
                if(systemEnergySources != null && systemEnergySources.Count != 0)
                {
                    foreach(SystemEnergySource systemEnergySource in systemEnergySources)
                    {
                        FuelSource fuelSource = systemEnergySource.ToTPD(energyCentre);
                    }
                }

                Point offset = new Point(0, 0);
                //double circuitLength = 10;

                ////Heating Refrigerant Groups

                //dynamic refrigerantGroup = plantRoom.RefrigerantGroup("DXCoil Units Refrigerant Group");
                //if (refrigerantGroup == null)
                //{
                //    refrigerantGroup = plantRoom.AddRefrigerantGroup();
                //    refrigerantGroup.Name = "DXCoil Units Refrigerant Group";
                //    refrigerantGroup.SetPosition(200, 440);
                //}

                ////Heating Groups

                //dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");
                //if (heatingGroup == null)
                //{
                //    heatingGroup = plantRoom.AddHeatingGroup();
                //    heatingGroup.Name = "Heating Circuit Group";
                //    heatingGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                //    heatingGroup.SetPosition(200, 0);
                //}

                ////Cooling Groups

                //dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");
                //if (coolingGroup == null)
                //{
                //    coolingGroup = plantRoom.AddCoolingGroup();
                //    coolingGroup.Name = "Cooling Circuit Group";
                //    coolingGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                //    coolingGroup.SetPosition(200, 280);
                //}

                ////DHW Groups

                //dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");
                //if (dHWGroup == null)
                //{
                //    dHWGroup = plantRoom.AddDHWGroup();
                //    dHWGroup.Name = "DHW Circuit Group";
                //    dHWGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                //    dHWGroup.LoadDistribution = tpdLoadDistribution.tpdLoadDistributionEven;
                //    dHWGroup.SetPosition(200, 140);
                //}

                ////Fuel Sources

                //dynamic fuelSource_Electrical = energyCentre.FuelSource("Grid Supplied Electricity");
                //if (fuelSource_Electrical == null)
                //{
                //    fuelSource_Electrical = energyCentre.AddFuelSource();
                //    fuelSource_Electrical.Name = "Grid Supplied Electricity";
                //    fuelSource_Electrical.Description = "";
                //    fuelSource_Electrical.CO2Factor = 0.519;
                //    fuelSource_Electrical.Electrical = 1;
                //    fuelSource_Electrical.TimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseValue;
                //    fuelSource_Electrical.PeakCost = 0.13;
                //}

                //dynamic fuelSource_Gas = energyCentre.FuelSource("Natural Gas");
                //if (fuelSource_Gas == null)
                //{
                //    fuelSource_Gas = energyCentre.AddFuelSource();
                //    fuelSource_Gas.Name = "Natural Gas";
                //    fuelSource_Gas.Description = "";
                //    fuelSource_Gas.CO2Factor = 0.216;
                //    fuelSource_Gas.Electrical = 0;
                //    fuelSource_Gas.TimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseValue;
                //    fuelSource_Gas.PeakCost = 0.05;
                //}

                //// Electrical Groups

                ////Fans
                //dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
                //if (electricalGroup_Fans == null)
                //{
                //    electricalGroup_Fans = plantRoom.AddElectricalGroup();
                //    electricalGroup_Fans.SetPosition(400, 0);
                //    electricalGroup_Fans.Name = "Electrical Group - Fans";
                //    electricalGroup_Fans.SetFuelSource(1, fuelSource_Electrical);
                //    electricalGroup_Fans.ElectricalGroupType = tpdElectricalGroupType.tpdElectricalGroupFans;
                //}

                ////DXCoilUnits
                //dynamic electricalGroup_DXCoilUnits = plantRoom.ElectricalGroup("Electrical Group - DXCoil Units");
                //if (electricalGroup_DXCoilUnits == null)
                //{
                //    electricalGroup_DXCoilUnits = plantRoom.AddElectricalGroup();
                //    electricalGroup_DXCoilUnits.SetPosition(820, 0);
                //    electricalGroup_DXCoilUnits.Name = "Electrical Group - DXCoil Units";
                //    electricalGroup_DXCoilUnits.SetFuelSource(1, fuelSource_Electrical);
                //    electricalGroup_DXCoilUnits.ElectricalGroupType = tpdElectricalGroupType.tpdElectricalGroupEquipment;
                //}

                ////Humidifiers
                //dynamic electricalGroup_Humidifiers = plantRoom.ElectricalGroup("Electrical Group - Humidifiers");
                //if (electricalGroup_Humidifiers == null)
                //{
                //    electricalGroup_Humidifiers = plantRoom.AddElectricalGroup();
                //    electricalGroup_Humidifiers.SetPosition(660, 0);
                //    electricalGroup_Humidifiers.Name = "Electrical Group - Humidifiers";
                //    electricalGroup_Humidifiers.SetFuelSource(1, fuelSource_Electrical);
                //    electricalGroup_Humidifiers.ElectricalGroupType = tpdElectricalGroupType.tpdElectricalGroupEquipment;
                //}

                ////FanCoilUnits
                //dynamic electricalGroup_FanCoilUnits = plantRoom.ElectricalGroup("Electrical Group - FanCoil Units");
                //if (electricalGroup_FanCoilUnits == null)
                //{
                //    electricalGroup_FanCoilUnits = plantRoom.AddElectricalGroup();
                //    electricalGroup_FanCoilUnits.SetPosition(740, 0);
                //    electricalGroup_FanCoilUnits.Name = "Electrical Group - FanCoil Units";
                //    electricalGroup_FanCoilUnits.SetFuelSource(1, fuelSource_Electrical);
                //    electricalGroup_FanCoilUnits.ElectricalGroupType = tpdElectricalGroupType.tpdElectricalGroupEquipment;
                //}

                ////Lighting
                //dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
                //if (electricalGroup_Lighting == null)
                //{
                //    electricalGroup_Lighting = plantRoom.AddElectricalGroup();
                //    electricalGroup_Lighting.SetPosition(offset.X + 500, offset.Y + 0);
                //    electricalGroup_Lighting.Name = "Electrical Group - Lighting";
                //    electricalGroup_Lighting.Description = "Internal Lighting";
                //    electricalGroup_Lighting.SetFuelSource(1, fuelSource_Electrical);
                //    electricalGroup_Lighting.ElectricalGroupType = tpdElectricalGroupType.tpdElectricalGroupLighting;
                //}

                ////Equipment
                //dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");
                //if (electricalGroup_SmallPower == null)
                //{
                //    electricalGroup_SmallPower = plantRoom.AddElectricalGroup();
                //    electricalGroup_SmallPower.SetPosition(offset.X + 580, offset.Y + 0);
                //    electricalGroup_SmallPower.Name = "Electrical Group - Small Power";
                //    electricalGroup_SmallPower.Description = "Space Equipment";
                //    electricalGroup_SmallPower.SetFuelSource(1, fuelSource_Electrical);
                //    electricalGroup_SmallPower.ElectricalGroupType = tpdElectricalGroupType.tpdElectricalGroupEquipment;
                //}

                ////Schedules
                ///

                //dynamic plantSchedule_Occupancy = null;




                ////Occupancy
                //dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");
                //if (plantSchedule_Occupancy == null)
                //{
                //    plantSchedule_Occupancy = energyCentre.AddSchedule(tpdScheduleType.tpdScheduleFunction);
                //    plantSchedule_Occupancy.Name = "Occupancy Schedule";
                //    plantSchedule_Occupancy.FunctionType = tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                //    plantSchedule_Occupancy.FunctionLoads = 1024; // occupant sensible
                //}

                //////System
                //dynamic plantSchedule_System = energyCentre.PlantSchedule("System Schedule");
                //if (plantSchedule_System == null)
                //{
                //    plantSchedule_System = energyCentre.AddSchedule(tpdScheduleType.tpdScheduleFunction);
                //    plantSchedule_System.Name = "System Schedule";
                //    plantSchedule_System.FunctionType = tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                //    plantSchedule_System.FunctionLoads = 4 + 8 + 1024; // heating, cooling, occupant sensible
                //}

                ////Zone
                //dynamic plantSchedule_Zone = energyCentre.PlantSchedule("Zone Schedule");
                //if (plantSchedule_Zone == null)
                //{
                //    plantSchedule_Zone = energyCentre.AddSchedule(tpdScheduleType.tpdScheduleFunction);
                //    plantSchedule_Zone.Name = "Zone Schedule";
                //    plantSchedule_Zone.FunctionType = tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                //    plantSchedule_Zone.FunctionLoads = 4 + 8 + 1024; // heating, cooling, occupant sensible
                //}

                ////Design Condition Load

                //dynamic designConditionLoad_Annual = energyCentre.DesignConditionLoad("Annual Design Condition");
                //if (designConditionLoad_Annual == null)
                //{
                //    designConditionLoad_Annual = energyCentre.AddDesignCondition();
                //    designConditionLoad_Annual.Name = "Annual Design Condition";
                //    designConditionLoad_Annual.PrecondHours = 48;
                //}

                ////Components

                ////Heating MultiBoiler
                //dynamic multiBoiler_Heating = plantRoom.MultiBoiler("Heating Circuit Boiler");
                //if (multiBoiler_Heating == null)
                //{
                //    multiBoiler_Heating = plantRoom.AddMultiBoiler();
                //    multiBoiler_Heating.Name = "Heating Circuit Boiler";
                //    multiBoiler_Heating.DesignPressureDrop = 25;
                //    multiBoiler_Heating.DesignDeltaT = 11;
                //    multiBoiler_Heating.Setpoint.Value = 71;
                //    multiBoiler_Heating.SetFuelSource(1, fuelSource_Gas);
                //    multiBoiler_Heating.Duty.Type = tpdSizedVariable.tpdSizedVariableSize;
                //    multiBoiler_Heating.Duty.SizeFraction = 1.0;
                //    multiBoiler_Heating.Duty.AddDesignCondition(designConditionLoad_Annual);
                //    multiBoiler_Heating.SetPosition(offset.X, offset.Y);
                //}

                ////Heating Pump
                //dynamic pump_Heating = plantRoom.Component<Pump>("Heating Circuit Pump");
                //if (pump_Heating == null)
                //{
                //    pump_Heating = plantRoom.AddPump();
                //    pump_Heating.Name = "Heating Circuit Pump";
                //    pump_Heating.DesignFlowRate = 0;
                //    pump_Heating.Capacity = 1;
                //    pump_Heating.OverallEfficiency.Value = 1;
                //    pump_Heating.SetFuelSource(1, fuelSource_Electrical);
                //    pump_Heating.Pressure = (multiBoiler_Heating.DesignPressureDrop + heatingGroup.DesignPressureDrop) / 0.712;
                //    pump_Heating.SetPosition(offset.X + 100, offset.Y);
                //}

                //plantRoom.AddPipe(multiBoiler_Heating, 1, pump_Heating, 1);
                //plantRoom.AddPipe(pump_Heating, 1, heatingGroup, 1);
                //plantRoom.AddPipe(heatingGroup, 1, multiBoiler_Heating, 1);

                //PlantController plantController_Heating = plantRoom.AddController();
                //plantController_Heating.AddControlArc(pump_Heating);
                //dynamic plantSensorArc = plantController_Heating.AddSensorArcToComponent(heatingGroup, 1);

                //plantController_Heating.SetPosition(offset.X + 180, offset.Y + 110);
                //plantController_Heating.SensorArc1 = plantSensorArc;
                //Modify.SetWaterSideController(plantController_Heating, WaterSideControllerSetup.Load, 0.1, 0.1);

                ////DHW MultiBoiler
                //dynamic multiBoiler_DHW = plantRoom.MultiBoiler("DHW Circuit Boiler");
                //if (multiBoiler_DHW == null)
                //{
                //    multiBoiler_DHW = plantRoom.AddMultiBoiler();
                //    multiBoiler_DHW.DesignPressureDrop = 25;
                //    multiBoiler_DHW.Setpoint.Value = 60;
                //    multiBoiler_DHW.SetFuelSource(1, fuelSource_Gas);
                //    multiBoiler_DHW.Duty.Type = tpdSizedVariable.tpdSizedVariableSize;
                //    multiBoiler_DHW.Duty.SizeFraction = 1.0;
                //    multiBoiler_DHW.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                //    multiBoiler_DHW.SetPosition(0, 140);
                //}

                ////DHW Pump
                //dynamic pump_DHW = plantRoom.Component<Pump>("DHW Circuit Pump");
                //if (pump_DHW == null)
                //{
                //    pump_DHW = plantRoom.AddPump();
                //    pump_DHW.Name = "DHW Circuit Pump";
                //    pump_DHW.Description = "DHW Circuit Pump";
                //    pump_DHW.DesignFlowRate = 1;
                //    pump_DHW.Capacity = 1;
                //    pump_DHW.OverallEfficiency.Value = 1;
                //    pump_DHW.SetFuelSource(1, fuelSource_Electrical);
                //    pump_DHW.Pressure = (multiBoiler_DHW.DesignPressureDrop + dHWGroup.DesignPressureDrop) / 0.712;
                //    pump_DHW.SetPosition(100, 150);
                //}

                //dynamic junction_DHW_In = plantRoom.AddJunction();
                //junction_DHW_In.Name = "DHW Junction In";
                //junction_DHW_In.Description = "Main Cold Water Supply";
                //junction_DHW_In.SetPosition(80, 210);
                //junction_DHW_In.SetDirection(tpdDirection.tpdRightLeft);

                //dynamic junction_DHW_Out = plantRoom.AddJunction();
                //junction_DHW_Out.Name = "DHW Junction Out";
                //junction_DHW_Out.Description = "DHW Junction Out";
                //junction_DHW_Out.SetPosition(160, 210);
                //junction_DHW_Out.SetDirection(tpdDirection.tpdRightLeft);

                //plantRoom.AddPipe(junction_DHW_In, 1, multiBoiler_DHW, 1);
                //Pipe pipe_DHW_Multiboiler_Pump = plantRoom.AddPipe(multiBoiler_DHW, 1, pump_DHW, 1);
                //pipe_DHW_Multiboiler_Pump.AddNode(80, 180);
                //plantRoom.AddPipe(pump_DHW, 1, dHWGroup, 1);

                //Pipe pipe = plantRoom.AddPipe(dHWGroup, 1, junction_DHW_Out, 1);

                //dynamic plantController_Load = plantRoom.AddController();
                //plantController_Load.Name = "DHW Load Controller";
                //plantController_Load.SetPosition(80, 250);

                //dynamic plantSensorArc_Load = plantController_Load.AddSensorArcToComponent(dHWGroup, 1);
                //plantController_Load.SensorArc1 = plantSensorArc_Load;

                //Modify.SetWaterSideController(plantController_Load, WaterSideControllerSetup.Load, 0.1, 0.1);

                //dynamic plantController_Temperature = plantRoom.AddController();
                //plantController_Temperature.Name = "DHW Temperature Controller";
                //plantController_Temperature.SetPosition(140, 250);

                //Modify.SetWaterSideController(plantController_Temperature, WaterSideControllerSetup.TemperatureLowZero, 10, 0);

                //dynamic plantSensorArc_Temperature = plantController_Temperature.AddSensorArc(pipe);
                //plantController_Temperature.SensorArc1 = plantSensorArc_Temperature;

                //dynamic plantController_Max = plantRoom.AddController();
                //plantController_Max.Name = "DHW Max Controller";
                //plantController_Max.SetPosition(110, 210);
                //plantController_Max.ControlType = tpdControlType.tpdControlMin;
                //plantController_Max.AddControlArc(pump_DHW);
                //plantController_Max.AddChainArc(plantController_Load);
                //plantController_Max.AddChainArc(plantController_Temperature);

                //dynamic multiChiller = plantRoom.AddMultiChiller();
                //multiChiller.Name = "Cooling Circuit Chiller";
                //multiChiller.DesignPressureDrop = 25;
                //multiChiller.DesignDeltaT = 6;
                //multiChiller.Setpoint.Value = 10;
                //multiChiller.SetFuelSource(1, fuelSource_Electrical);
                //multiChiller.Duty.Type = tpdSizedVariable.tpdSizedVariableSize;
                //multiChiller.Duty.SizeFraction = 1.0;
                //multiChiller.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                //multiChiller.SetPosition(0, 280);

                //dynamic pump_Cooling = plantRoom.AddPump();
                //pump_Cooling.Name = "Cooling Circuit Pump";
                //pump_Cooling.DesignFlowRate = 0;
                //pump_Cooling.Capacity = 1;
                //pump_Cooling.OverallEfficiency.Value = 1;
                //pump_Cooling.SetFuelSource(1, fuelSource_Electrical);
                //pump_Cooling.Pressure = (multiChiller.DesignPressureDrop + coolingGroup.DesignPressureDrop) / 0.712;
                //pump_Cooling.SetPosition(100, 280);

                //plantRoom.AddPipe(multiChiller, 1, pump_Cooling, 1);
                //plantRoom.AddPipe(pump_Cooling, 1, coolingGroup, 1);
                //plantRoom.AddPipe(coolingGroup, 1, multiChiller, 1);

                //dynamic plantController_Cooling = plantRoom.AddController();
                //plantController_Cooling.AddControlArc(pump_Cooling);
                //dynamic plantSensorArc_Cooling = plantController_Cooling.AddSensorArcToComponent(coolingGroup, 1);

                //plantController_Cooling.SetPosition(180, 380);
                //plantController_Cooling.SensorArc1 = plantSensorArc_Cooling;
                //Modify.SetWaterSideController(plantController_Cooling, WaterSideControllerSetup.Load, 0.1, 0.1);

                //dynamic airSourceHeatPump = plantRoom.AddAirSourceHeatPump();
                //airSourceHeatPump.SetPosition(0, 440);
                //airSourceHeatPump.SetFuelSource(1, fuelSource_Electrical);
                //airSourceHeatPump.SetFuelSource(2, fuelSource_Electrical);
                //airSourceHeatPump.SetFuelSource(3, fuelSource_Electrical);
                //airSourceHeatPump.Name = "DXCoil Units Air Source Heat Pump";
                //plantRoom.AddPipe(refrigerantGroup, 1, airSourceHeatPump, 1);
                //plantRoom.AddPipe(airSourceHeatPump, 1, refrigerantGroup, 1);

                //for (int j = 1; j <= energyCentre.GetCalendar().GetDayTypeCount(); j++)
                //{
                //    PlantDayType plantDayType = plantRoom.GetEnergyCentre().GetCalendar().GetDayType(j);

                //    //Water Side
                //    plantController_Heating.AddDayType(plantDayType);
                //    plantController_Max.AddDayType(plantDayType);
                //    plantController_Load.AddDayType(plantDayType);
                //    plantController_Temperature.AddDayType(plantDayType);
                //    plantController_Cooling.AddDayType(plantDayType);
                //}

                List<SystemPlantRoom> systemPlantRooms = systemEnergyCentre.GetSystemPlantRooms();
                if (systemPlantRooms != null && systemPlantRooms.Count != 0)
                {
                    foreach (SystemPlantRoom systemPlantRoom in systemPlantRooms)
                    {
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

                                List<Core.Systems.ISystemComponent> systemComponents = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(liquidSystem, ConnectorStatus.Undefined, Direction.Out);
                                if (systemComponents == null || systemComponents.Count == 0)
                                {
                                    systemComponents = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(liquidSystem);
                                }

                                List<ISystemCollection> systemCollections = systemPlantRoom.GetSystemComponents<ISystemCollection>(liquidSystem);
                                if(systemCollections != null)
                                {
                                    foreach (ISystemCollection systemCollection in systemCollections)
                                    {
                                        if(systemCollection == null)
                                        {
                                            continue;
                                        }

                                        PlantComponent plantComponent_TPD = ToTPD(systemCollection as dynamic, plantRoom) as PlantComponent;

                                        dictionary_SystemComponents_TPD[(systemCollection as dynamic).Guid] = plantComponent_TPD;
                                        systemCollection.SetReference(Query.Reference(plantComponent_TPD));
                                        systemPlantRoom.Add(systemCollection);

                                        if(systemComponents != null)
                                        {
                                            systemComponents.RemoveAll(x => x is ISystemCollection && ((dynamic)x).Guid == ((dynamic)x).Guid);
                                        }
                                    }
                                }

                                if (systemComponents  == null || systemComponents.Count == 0)
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

                                    if (plantComponent_TPD == null)
                                    {
                                        continue;
                                    }

                                    dictionary_SystemComponents_TPD[systemComponent_Temp.Guid] = plantComponent_TPD;
                                    systemComponent_Temp.SetReference(Query.Reference(plantComponent_TPD));
                                    systemPlantRoom.Add(systemComponent_Temp);
                                }

                                Create.Pipes(systemPlantRoom, plantRoom, dictionary_SystemComponents_TPD, out Dictionary<Guid, Pipe> dictionary_Pipes);
                                Create.PlantControllers(systemPlantRoom, plantRoom, liquidSystem, dictionary_SystemComponents_TPD, dictionary_Pipes);
                            }
                        }

                        List<Core.Systems.ISystemComponent> systemComponents_All = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>();
                        if(systemComponents_All != null)
                        {
                            foreach(Core.Systems.ISystemComponent systemComponent_Temp in systemComponents_All)
                            {
                                List<Core.Systems.ISystem> systems = systemPlantRoom.GetRelatedObjects<Core.Systems.ISystem>(systemComponent_Temp);
                                if(systems != null && systems.Count != 0)
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
                                Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponent_TPD = new Dictionary<Guid, global::TPD.ISystemComponent>();
                                Dictionary<Guid, Core.Systems.ISystemComponent> dictionary_SystemComponent_SAM = new Dictionary<Guid, Core.Systems.ISystemComponent>();

                                global::TPD.System system = airSystem.ToTPD(plantRoom);
                                if (system == null)
                                {
                                    continue;
                                }

                                Modify.SetReference(airSystem, system.Reference());
                                systemPlantRoom.Add(airSystem);

                                offset = new Point(0, 0);

                                List<Core.Systems.ISystemComponent> systemComponents_AirSystem_NoConnector = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(airSystem);

                                List <Core.Systems.ISystemComponent> systemComponents_AirSystem = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(airSystem, ConnectorStatus.Unconnected, Direction.Out);
                                if(systemComponents_AirSystem != null && systemComponents_AirSystem.Count != 0)
                                {
                                    foreach (Core.Systems.ISystemComponent systemComponent in systemComponents_AirSystem)
                                    {
                                        systemComponents_AirSystem_NoConnector.RemoveAll(x => ((dynamic)x).Guid == ((dynamic)systemComponent).Guid);
                                    }

                                    while (systemComponents_AirSystem.Count > 0)
                                    {
                                        Core.Systems.ISystemComponent systemComponent = systemComponents_AirSystem[0];
                                        systemComponents_AirSystem.RemoveAt(0);

                                        if (systemComponent == null)
                                        {
                                            continue;
                                        }

                                        List<Core.Systems.ISystemComponent> systemComponents_Ordered = systemPlantRoom.GetOrderedSystemComponents(systemComponent, airSystem, Direction.In);
                                        if (systemComponents_Ordered == null || systemComponents_Ordered.Count == 0)
                                        {
                                            continue;
                                        }

                                        if(systemComponents_Ordered.Count > 1)
                                        {
                                            systemComponents_Ordered.Insert(0, systemComponent);
                                        }

                                        foreach (Core.Systems.ISystemComponent systemComponents_Temp in systemComponents_Ordered)
                                        {
                                            dictionary_SystemComponent_SAM[systemPlantRoom.GetGuid(systemComponents_Temp)] = systemComponents_Temp;
                                        }

                                        foreach (Core.Systems.SystemComponent systemComponent_Temp in systemComponents_Ordered)
                                        {
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
                                                List<AirSystemGroup> airSystemGroups_SystemSpace =  systemPlantRoom.GetRelatedObjects<AirSystemGroup>(systemComponent_Temp);
                                                bool addSystemSpaceComponets = airSystemGroups_SystemSpace == null || airSystemGroups_SystemSpace.Count == 0;

                                                systemComponent_TPD = ToTPD((DisplaySystemSpace)systemComponent_Temp, systemPlantRoom, system, null, addSystemSpaceComponets) as global::TPD.ISystemComponent;
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

                                            dictionary_SystemComponent_TPD[systemComponent_Temp.Guid] = systemComponent_TPD;
                                            systemComponent_Temp.SetReference(Query.Reference(systemComponent_TPD));
                                            systemPlantRoom.Add(systemComponent_Temp);
                                        }

                                        Create.Ducts(systemPlantRoom, system, dictionary_SystemComponent_TPD, out Dictionary<Guid, Duct> dictionary_Ducts);
                                        Create.Controllers(systemPlantRoom, system, airSystem, dictionary_SystemComponent_TPD, dictionary_Ducts);

                                        List<AirSystemGroup> airSystemGroups = systemPlantRoom.GetSystemGroups<AirSystemGroup>(airSystem);
                                        if(airSystemGroups != null)
                                        {
                                            foreach (AirSystemGroup airSystemGroup in airSystemGroups)
                                            {
                                                SortedDictionary<int, List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>>> sortedDictionary = new SortedDictionary<int, List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>>>();

                                                List <Core.Systems.ISystemComponent> systemComponents_SAM = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(airSystemGroup);
                                                if(systemComponents_SAM != null)
                                                {
                                                    for (int i = systemComponents_SAM.Count - 1; i >= 0; i--)
                                                    {
                                                        Core.Systems.ISystemComponent systemComponent_SAM_Temp = systemComponents_SAM[i];

                                                        int groupIndex = -1;

                                                        if (!(systemComponent_SAM_Temp is IAirSystemComponent) || !(((dynamic)systemComponent_SAM_Temp).TryGetValue(AirSystemComponentParameter.GroupIndex, out groupIndex)))
                                                        {
                                                            continue;
                                                        }

                                                        if (!sortedDictionary.TryGetValue(groupIndex, out List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>> tuples))
                                                        {
                                                            tuples = new List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>>();
                                                            sortedDictionary[groupIndex] = tuples;
                                                        }

                                                        if (!dictionary_SystemComponent_TPD.TryGetValue((systemComponent_SAM_Temp as dynamic).Guid, out global::TPD.ISystemComponent systemComponent_TPD_Temp))
                                                        {
                                                            systemComponent_TPD_Temp = null;
                                                        }

                                                        tuples.Add(new Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>(systemComponent_SAM_Temp, systemComponent_TPD_Temp));

                                                        systemComponents_SAM.RemoveAt(i);
                                                    }

                                                    for (int i = systemComponents_SAM.Count - 1; i >= 0; i--)
                                                    {
                                                        Core.Systems.ISystemComponent systemComponent_SAM_Temp = systemComponents_SAM[i];

                                                        int groupIndex = sortedDictionary.Count == 0 ? 0 : sortedDictionary.Keys.Max();

                                                        if (!sortedDictionary.TryGetValue(groupIndex, out List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>> tuples))
                                                        {
                                                            tuples = new List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>>();
                                                            sortedDictionary[groupIndex] = tuples;
                                                        }

                                                        if (!dictionary_SystemComponent_TPD.TryGetValue((systemComponent_SAM_Temp as dynamic).Guid, out global::TPD.ISystemComponent systemComponent_TPD_Temp))
                                                        {
                                                            systemComponent_TPD_Temp = null;
                                                        }

                                                        tuples.Add(new Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>(systemComponent_SAM_Temp, systemComponent_TPD_Temp));

                                                        systemComponents_SAM.RemoveAt(i);
                                                    }
                                                }

                                                int count = 1;

                                                List<global::TPD.ISystemComponent> systemComponents_TPD = new List<global::TPD.ISystemComponent>();
                                                foreach(List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>> tuples in sortedDictionary.Values)
                                                {
                                                    if(tuples == null || tuples.Count == 0)
                                                    {
                                                        continue;
                                                    }

                                                    global::TPD.ISystemComponent systemComponent_TPD_Temp = tuples.Find(x => x.Item2 != null)?.Item2;
                                                    if(systemComponent_TPD_Temp == null)
                                                    {
                                                        continue;
                                                    }

                                                    systemComponents_TPD.Add(systemComponent_TPD_Temp);

                                                    if(systemComponent_TPD_Temp is SystemZone)
                                                    {
                                                        count = tuples.Count;
                                                    }

                                                }

                                                //TODO: Implement controllers
                                                Controller[] controllers = new Controller[0];

                                                ComponentGroup componentGroup = system.AddGroup(systemComponents_TPD.ToArray(), controllers);

                                                componentGroup.SetMultiplicity(count);

                                                int index = 0;

                                                List<global::TPD.SystemComponent> systemComponents_TPD_New = Query.SystemComponents<global::TPD.SystemComponent>(componentGroup, false, false);
                                                for (int i = 0; i < systemComponents_TPD_New.Count; i++)
                                                {
                                                    global::TPD.SystemComponent systemComponent_TPD_New = systemComponents_TPD_New[i];

                                                    if(sortedDictionary.TryGetValue(index, out List<Tuple<Core.Systems.ISystemComponent, global::TPD.ISystemComponent>> tuples) && tuples != null && tuples.Count != 0)
                                                    {
                                                        int index_Temp = tuples.FindIndex(x => (x.Item2 as dynamic)?.GUID == ((dynamic)systemComponent_TPD_New).GUID);
                                                        if(index_Temp != -1)
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
                                                            else if (systemComponent_SAM is DisplaySystemDamper && systemComponent_TPD_New is global::TPD.Damper)
                                                            {
                                                                ToTPD((DisplaySystemDamper)systemComponent_SAM, system, (global::TPD.Damper)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemDesiccantWheel && systemComponent_TPD_New is global::TPD.DesiccantWheel)
                                                            {
                                                                ToTPD((DisplaySystemDesiccantWheel)systemComponent_SAM, system, (global::TPD.DesiccantWheel)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemDXCoil && systemComponent_TPD_New is global::TPD.DXCoil)
                                                            {
                                                                ToTPD((DisplaySystemDXCoil)systemComponent_SAM, system, (global::TPD.DXCoil)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemExchanger && systemComponent_TPD_New is global::TPD.Exchanger)
                                                            {
                                                                ToTPD((DisplaySystemExchanger)systemComponent_SAM, system, (global::TPD.Exchanger)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemFan && systemComponent_TPD_New is global::TPD.Fan)
                                                            {
                                                                ToTPD((DisplaySystemFan)systemComponent_SAM, system, (global::TPD.Fan)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemLoadComponent && systemComponent_TPD_New is global::TPD.LoadComponent)
                                                            {
                                                                ToTPD((DisplaySystemLoadComponent)systemComponent_SAM, system, (global::TPD.LoadComponent)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemEconomiser && systemComponent_TPD_New is global::TPD.Optimiser)
                                                            {
                                                                ToTPD((DisplaySystemEconomiser)systemComponent_SAM, system, (global::TPD.Optimiser)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemMixingBox && systemComponent_TPD_New is global::TPD.Optimiser)
                                                            {
                                                                ToTPD((DisplaySystemMixingBox)systemComponent_SAM, system, (global::TPD.Optimiser)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemSprayHumidifier && systemComponent_TPD_New is global::TPD.SprayHumidifier)
                                                            {
                                                                ToTPD((DisplaySystemSprayHumidifier)systemComponent_SAM, system, (global::TPD.SprayHumidifier)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemSteamHumidifier && systemComponent_TPD_New is global::TPD.SteamHumidifier)
                                                            {
                                                                ToTPD((DisplaySystemSteamHumidifier)systemComponent_SAM, system, (global::TPD.SteamHumidifier)systemComponent_TPD_New);
                                                            }
                                                            else if (systemComponent_SAM is DisplaySystemDirectEvaporativeCooler && systemComponent_TPD_New is global::TPD.SprayHumidifier)
                                                            {
                                                                ToTPD((DisplaySystemDirectEvaporativeCooler)systemComponent_SAM, system, (global::TPD.SprayHumidifier)systemComponent_TPD_New);
                                                            }
                                                        }
                                                    }

                                                    index++;
                                                    if (index >= count)
                                                    {
                                                        index = 0;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (systemComponents_AirSystem_NoConnector != null && systemComponents_AirSystem_NoConnector.Count != 0)
                                {
                                    foreach (Core.Systems.ISystemComponent systemComponent in systemComponents_AirSystem_NoConnector)
                                    {
                                        global::TPD.ISystemComponent systemComponent_TPD = null;

                                        if (systemComponent is DisplaySystemLoadComponent)
                                        {
                                            systemComponent_TPD = ToTPD((DisplaySystemLoadComponent)systemComponent, system) as global::TPD.ISystemComponent;
                                        }

                                        if (systemComponent_TPD == null)
                                        {
                                            continue;
                                        }

                                        dictionary_SystemComponent_TPD[((dynamic)systemComponent).Guid] = systemComponent_TPD;
                                        systemComponent.SetReference(Query.Reference(systemComponent_TPD));
                                        systemPlantRoom.Add(systemComponent);
                                    }
                                }
                            }

                            systemEnergyCentre.Add(systemPlantRoom);
                        }
                    }
                }

                if (systemEnergyCentreConversionSettings == null)
                {
                    systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings();
                }

                if (systemEnergyCentreConversionSettings.Simulate)
                {
                    plantRoom.SimulateEx(systemEnergyCentreConversionSettings.StartHour + 1, systemEnergyCentreConversionSettings.EndHour + 1, 0, energyCentre.ExternalPollutant.Value, 10.0, (int)tpdSimulationData.tpdSimulationDataLoad + (int)tpdSimulationData.tpdSimulationDataPipe + (int)tpdSimulationData.tpdSimulationDataDuct + (int)tpdSimulationData.tpdSimulationDataSimEvents, 1, 0);
                    if (systemEnergyCentreConversionSettings.IncludeComponentResults)
                    {
                        Modify.CopyResults(energyCentre, systemEnergyCentre, systemEnergyCentreConversionSettings.StartHour + 1, systemEnergyCentreConversionSettings.EndHour + 1);
                    }
                }

                tPDDoc.Save();
            }

            return true;
        }
    }
}