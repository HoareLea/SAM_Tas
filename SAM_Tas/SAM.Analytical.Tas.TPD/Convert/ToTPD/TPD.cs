using SAM.Core.Systems;
using SAM.Core.Tas;
using TPD;
using System.Drawing;
using System.Reflection;
using System;
using System.Collections.Generic;
using SAM.Analytical.Systems;
using SAM.Core;
using System.Linq;
using SAM.Geometry.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static bool ToTPD(this SystemEnergyCentre systemEnergyCentre, string path_TPD, string path_TSD, SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = null)
        {
            if(systemEnergyCentre == null)
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

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {
                TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                if (tPDDoc == null)
                {
                    return false;
                }

                Point offset = new Point(0, 0);
                double circuitLength = 10;

                EnergyCentre energyCentre = tPDDoc.EnergyCentre;

                PlantRoom plantRoom = energyCentre.PlantRoom("Main PlantRoom");
                if (plantRoom == null)
                {
                    plantRoom = energyCentre.AddPlantRoom();
                    plantRoom.Name = "Main PlantRoom";
                }

                energyCentre.AddTSDData(path_TSD, 0);

                TSDData tSDData = energyCentre.GetTSDData(1);

                //Heating Refrigerant Groups

                dynamic refrigerantGroup = plantRoom.RefrigerantGroup("DXCoil Units Refrigerant Group");
                if (refrigerantGroup == null)
                {
                    refrigerantGroup = plantRoom.AddRefrigerantGroup();
                    refrigerantGroup.Name = "DXCoil Units Refrigerant Group";
                    refrigerantGroup.SetPosition(200, 440);
                }

                //Heating Groups

                dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");
                if (heatingGroup == null)
                {
                    heatingGroup = plantRoom.AddHeatingGroup();
                    heatingGroup.Name = "Heating Circuit Group";
                    heatingGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                    heatingGroup.SetPosition(200, 0);
                }

                //Cooling Groups

                dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");
                if (coolingGroup == null)
                {
                    coolingGroup = plantRoom.AddCoolingGroup();
                    coolingGroup.Name = "Cooling Circuit Group";
                    coolingGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                    coolingGroup.SetPosition(200, 280);
                }

                //DHW Groups

                dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");
                if (dHWGroup == null)
                {
                    dHWGroup = plantRoom.AddDHWGroup();
                    dHWGroup.Name = "DHW Circuit Group";
                    dHWGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                    dHWGroup.LoadDistribution = global::TPD.tpdLoadDistribution.tpdLoadDistributionEven;
                    dHWGroup.SetPosition(200, 140);
                }

                //Fuel Sources

                dynamic fuelSource_Electrical = energyCentre.FuelSource("Grid Supplied Electricity");
                if (fuelSource_Electrical == null)
                {
                    fuelSource_Electrical = energyCentre.AddFuelSource();
                    fuelSource_Electrical.Name = "Grid Supplied Electricity";
                    fuelSource_Electrical.Description = "";
                    fuelSource_Electrical.CO2Factor = 0.519;
                    fuelSource_Electrical.Electrical = 1;
                    fuelSource_Electrical.TimeOfUseType = global::TPD.tpdTimeOfUseType.tpdTimeOfUseValue;
                    fuelSource_Electrical.PeakCost = 0.13;
                }

                dynamic fuelSource_Gas = energyCentre.FuelSource("Natural Gas");
                if (fuelSource_Gas == null)
                {
                    fuelSource_Gas = energyCentre.AddFuelSource();
                    fuelSource_Gas.Name = "Natural Gas";
                    fuelSource_Gas.Description = "";
                    fuelSource_Gas.CO2Factor = 0.216;
                    fuelSource_Gas.Electrical = 0;
                    fuelSource_Gas.TimeOfUseType = global::TPD.tpdTimeOfUseType.tpdTimeOfUseValue;
                    fuelSource_Gas.PeakCost = 0.05;
                }

                // Electrical Groups

                //Fans
                dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
                if (electricalGroup_Fans == null)
                {
                    electricalGroup_Fans = plantRoom.AddElectricalGroup();
                    electricalGroup_Fans.SetPosition(400, 0);
                    electricalGroup_Fans.Name = "Electrical Group - Fans";
                    electricalGroup_Fans.SetFuelSource(1, fuelSource_Electrical);
                    electricalGroup_Fans.ElectricalGroupType = global::TPD.tpdElectricalGroupType.tpdElectricalGroupFans;
                }

                //DXCoilUnits
                dynamic electricalGroup_DXCoilUnits = plantRoom.ElectricalGroup("Electrical Group - DXCoil Units");
                if (electricalGroup_DXCoilUnits == null)
                {
                    electricalGroup_DXCoilUnits = plantRoom.AddElectricalGroup();
                    electricalGroup_DXCoilUnits.SetPosition(820, 0);
                    electricalGroup_DXCoilUnits.Name = "Electrical Group - DXCoil Units";
                    electricalGroup_DXCoilUnits.SetFuelSource(1, fuelSource_Electrical);
                    electricalGroup_DXCoilUnits.ElectricalGroupType = global::TPD.tpdElectricalGroupType.tpdElectricalGroupEquipment;
                }

                //Humidifiers
                dynamic electricalGroup_Humidifiers = plantRoom.ElectricalGroup("Electrical Group - Humidifiers");
                if (electricalGroup_Humidifiers == null)
                {
                    electricalGroup_Humidifiers = plantRoom.AddElectricalGroup();
                    electricalGroup_Humidifiers.SetPosition(660, 0);
                    electricalGroup_Humidifiers.Name = "Electrical Group - Humidifiers";
                    electricalGroup_Humidifiers.SetFuelSource(1, fuelSource_Electrical);
                    electricalGroup_Humidifiers.ElectricalGroupType = global::TPD.tpdElectricalGroupType.tpdElectricalGroupEquipment;
                }

                //FanCoilUnits
                dynamic electricalGroup_FanCoilUnits = plantRoom.ElectricalGroup("Electrical Group - FanCoil Units");
                if (electricalGroup_FanCoilUnits == null)
                {
                    electricalGroup_FanCoilUnits = plantRoom.AddElectricalGroup();
                    electricalGroup_FanCoilUnits.SetPosition(740, 0);
                    electricalGroup_FanCoilUnits.Name = "Electrical Group - FanCoil Units";
                    electricalGroup_FanCoilUnits.SetFuelSource(1, fuelSource_Electrical);
                    electricalGroup_FanCoilUnits.ElectricalGroupType = global::TPD.tpdElectricalGroupType.tpdElectricalGroupEquipment;
                }

                //Lighting
                dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
                if (electricalGroup_Lighting == null)
                {
                    electricalGroup_Lighting = plantRoom.AddElectricalGroup();
                    electricalGroup_Lighting.SetPosition(offset.X + 500, offset.Y + 0);
                    electricalGroup_Lighting.Name = "Electrical Group - Lighting";
                    electricalGroup_Lighting.Description = "Internal Lighting";
                    electricalGroup_Lighting.SetFuelSource(1, fuelSource_Electrical);
                    electricalGroup_Lighting.ElectricalGroupType = global::TPD.tpdElectricalGroupType.tpdElectricalGroupLighting;
                }

                //Equipment
                dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");
                if (electricalGroup_SmallPower == null)
                {
                    electricalGroup_SmallPower = plantRoom.AddElectricalGroup();
                    electricalGroup_SmallPower.SetPosition(offset.X + 580, offset.Y + 0);
                    electricalGroup_SmallPower.Name = "Electrical Group - Small Power";
                    electricalGroup_SmallPower.Description = "Space Equipment";
                    electricalGroup_SmallPower.SetFuelSource(1, fuelSource_Electrical);
                    electricalGroup_SmallPower.ElectricalGroupType = global::TPD.tpdElectricalGroupType.tpdElectricalGroupEquipment;
                }

                //Schedules

                //Occupancy
                dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");
                if (plantSchedule_Occupancy == null)
                {
                    plantSchedule_Occupancy = energyCentre.AddSchedule(global::TPD.tpdScheduleType.tpdScheduleFunction);
                    plantSchedule_Occupancy.Name = "Occupancy Schedule";
                    plantSchedule_Occupancy.FunctionType = global::TPD.tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                    plantSchedule_Occupancy.FunctionLoads = 1024; // occupant sensible
                }

                //System
                dynamic plantSchedule_System = energyCentre.PlantSchedule("System Schedule");
                if (plantSchedule_System == null)
                {
                    plantSchedule_System = energyCentre.AddSchedule(global::TPD.tpdScheduleType.tpdScheduleFunction);
                    plantSchedule_System.Name = "System Schedule";
                    plantSchedule_System.FunctionType = global::TPD.tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                    plantSchedule_System.FunctionLoads = 4 + 8 + 1024; // heating, cooling, occupant sensible
                }

                //Zone
                dynamic plantSchedule_Zone = energyCentre.PlantSchedule("Zone Schedule");
                if (plantSchedule_Zone == null)
                {
                    plantSchedule_Zone = energyCentre.AddSchedule(global::TPD.tpdScheduleType.tpdScheduleFunction);
                    plantSchedule_Zone.Name = "Zone Schedule";
                    plantSchedule_Zone.FunctionType = global::TPD.tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                    plantSchedule_Zone.FunctionLoads = 4 + 8 + 1024; // heating, cooling, occupant sensible
                }

                //Design Condition Load

                dynamic designConditionLoad_Annual = energyCentre.DesignConditionLoad("Annual Design Condition");
                if (designConditionLoad_Annual == null)
                {
                    designConditionLoad_Annual = energyCentre.AddDesignCondition();
                    designConditionLoad_Annual.Name = "Annual Design Condition";
                    designConditionLoad_Annual.PrecondHours = 48;
                }

                //Components

                //Heating MultiBoiler
                dynamic multiBoiler_Heating = plantRoom.MultiBoiler("Heating Circuit Boiler");
                if (multiBoiler_Heating == null)
                {
                    multiBoiler_Heating = plantRoom.AddMultiBoiler();
                    multiBoiler_Heating.Name = "Heating Circuit Boiler";
                    multiBoiler_Heating.DesignPressureDrop = 25;
                    multiBoiler_Heating.DesignDeltaT = 11;
                    multiBoiler_Heating.Setpoint.Value = 71;
                    multiBoiler_Heating.SetFuelSource(1, fuelSource_Gas);
                    multiBoiler_Heating.Duty.Type = global::TPD.tpdSizedVariable.tpdSizedVariableSize;
                    multiBoiler_Heating.Duty.SizeFraction = 1.0;
                    multiBoiler_Heating.Duty.AddDesignCondition(designConditionLoad_Annual);
                    multiBoiler_Heating.SetPosition(offset.X, offset.Y);
                }

                //Heating Pump
                dynamic pump_Heating = plantRoom.Component<global::TPD.Pump>("Heating Circuit Pump");
                if (pump_Heating == null)
                {
                    pump_Heating = plantRoom.AddPump();
                    pump_Heating.Name = "Heating Circuit Pump";
                    pump_Heating.DesignFlowRate = 0;
                    pump_Heating.Capacity = 1;
                    pump_Heating.OverallEfficiency.Value = 1;
                    pump_Heating.SetFuelSource(1, fuelSource_Electrical);
                    pump_Heating.Pressure = (multiBoiler_Heating.DesignPressureDrop + heatingGroup.DesignPressureDrop) / 0.712;
                    pump_Heating.SetPosition(offset.X + 100, offset.Y);
                }

                plantRoom.AddPipe(multiBoiler_Heating, 1, pump_Heating, 1);
                plantRoom.AddPipe(pump_Heating, 1, heatingGroup, 1);
                plantRoom.AddPipe(heatingGroup, 1, multiBoiler_Heating, 1);

                global::TPD.PlantController plantController_Heating = plantRoom.AddController();
                plantController_Heating.AddControlArc(pump_Heating);
                dynamic plantSensorArc = plantController_Heating.AddSensorArcToComponent(heatingGroup, 1);

                plantController_Heating.SetPosition(offset.X + 180, offset.Y + 110);
                plantController_Heating.SensorArc1 = plantSensorArc;
                Modify.SetWaterSideController(plantController_Heating, WaterSideControllerSetup.Load, 0.1, 0.1);

                //DHW MultiBoiler
                dynamic multiBoiler_DHW = plantRoom.MultiBoiler("DHW Circuit Boiler");
                if (multiBoiler_DHW == null)
                {
                    multiBoiler_DHW = plantRoom.AddMultiBoiler();
                    multiBoiler_DHW.DesignPressureDrop = 25;
                    multiBoiler_DHW.Setpoint.Value = 60;
                    multiBoiler_DHW.SetFuelSource(1, fuelSource_Gas);
                    multiBoiler_DHW.Duty.Type = global::TPD.tpdSizedVariable.tpdSizedVariableSize;
                    multiBoiler_DHW.Duty.SizeFraction = 1.0;
                    multiBoiler_DHW.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                    multiBoiler_DHW.SetPosition(0, 140);
                }

                //DHW Pump
                dynamic pump_DHW = plantRoom.Component<global::TPD.Pump>("DHW Circuit Pump");
                if (pump_DHW == null)
                {
                    pump_DHW = plantRoom.AddPump();
                    pump_DHW.Name = "DHW Circuit Pump";
                    pump_DHW.Description = "DHW Circuit Pump";
                    pump_DHW.DesignFlowRate = 1;
                    pump_DHW.Capacity = 1;
                    pump_DHW.OverallEfficiency.Value = 1;
                    pump_DHW.SetFuelSource(1, fuelSource_Electrical);
                    pump_DHW.Pressure = (multiBoiler_DHW.DesignPressureDrop + dHWGroup.DesignPressureDrop) / 0.712;
                    pump_DHW.SetPosition(100, 150);
                }

                dynamic junction_DHW_In = plantRoom.AddJunction();
                junction_DHW_In.Name = "DHW Junction In";
                junction_DHW_In.Description = "Main Cold Water Supply";
                junction_DHW_In.SetPosition(80, 210);
                junction_DHW_In.SetDirection(global::TPD.tpdDirection.tpdRightLeft);

                dynamic junction_DHW_Out = plantRoom.AddJunction();
                junction_DHW_Out.Name = "DHW Junction Out";
                junction_DHW_Out.Description = "DHW Junction Out";
                junction_DHW_Out.SetPosition(160, 210);
                junction_DHW_Out.SetDirection(global::TPD.tpdDirection.tpdRightLeft);

                plantRoom.AddPipe(junction_DHW_In, 1, multiBoiler_DHW, 1);
                global::TPD.Pipe pipe_DHW_Multiboiler_Pump = plantRoom.AddPipe(multiBoiler_DHW, 1, pump_DHW, 1);
                pipe_DHW_Multiboiler_Pump.AddNode(80, 180);
                plantRoom.AddPipe(pump_DHW, 1, dHWGroup, 1);

                global::TPD.Pipe pipe = plantRoom.AddPipe(dHWGroup, 1, junction_DHW_Out, 1);

                dynamic plantController_Load = plantRoom.AddController();
                plantController_Load.Name = "DHW Load Controller";
                plantController_Load.SetPosition(80, 250);

                dynamic plantSensorArc_Load = plantController_Load.AddSensorArcToComponent(dHWGroup, 1);
                plantController_Load.SensorArc1 = plantSensorArc_Load;

                Modify.SetWaterSideController(plantController_Load, WaterSideControllerSetup.Load, 0.1, 0.1);

                dynamic plantController_Temperature = plantRoom.AddController();
                plantController_Temperature.Name = "DHW Temperature Controller";
                plantController_Temperature.SetPosition(140, 250);

                Modify.SetWaterSideController(plantController_Temperature, WaterSideControllerSetup.TemperatureLowZero, 10, 0);

                dynamic plantSensorArc_Temperature = plantController_Temperature.AddSensorArc(pipe);
                plantController_Temperature.SensorArc1 = plantSensorArc_Temperature;

                dynamic plantController_Max = plantRoom.AddController();
                plantController_Max.Name = "DHW Max Controller";
                plantController_Max.SetPosition(110, 210);
                plantController_Max.ControlType = global::TPD.tpdControlType.tpdControlMin;
                plantController_Max.AddControlArc(pump_DHW);
                plantController_Max.AddChainArc(plantController_Load);
                plantController_Max.AddChainArc(plantController_Temperature);

                dynamic multiChiller = plantRoom.AddMultiChiller();
                multiChiller.Name = "Cooling Circuit Chiller";
                multiChiller.DesignPressureDrop = 25;
                multiChiller.DesignDeltaT = 6;
                multiChiller.Setpoint.Value = 10;
                multiChiller.SetFuelSource(1, fuelSource_Electrical);
                multiChiller.Duty.Type = global::TPD.tpdSizedVariable.tpdSizedVariableSize;
                multiChiller.Duty.SizeFraction = 1.0;
                multiChiller.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                multiChiller.SetPosition(0, 280);

                dynamic pump_Cooling = plantRoom.AddPump();
                pump_Cooling.Name = "Cooling Circuit Pump";
                pump_Cooling.DesignFlowRate = 0;
                pump_Cooling.Capacity = 1;
                pump_Cooling.OverallEfficiency.Value = 1;
                pump_Cooling.SetFuelSource(1, fuelSource_Electrical);
                pump_Cooling.Pressure = (multiChiller.DesignPressureDrop + coolingGroup.DesignPressureDrop) / 0.712;
                pump_Cooling.SetPosition(100, 280);

                plantRoom.AddPipe(multiChiller, 1, pump_Cooling, 1);
                plantRoom.AddPipe(pump_Cooling, 1, coolingGroup, 1);
                plantRoom.AddPipe(coolingGroup, 1, multiChiller, 1);

                dynamic plantController_Cooling = plantRoom.AddController();
                plantController_Cooling.AddControlArc(pump_Cooling);
                dynamic plantSensorArc_Cooling = plantController_Cooling.AddSensorArcToComponent(coolingGroup, 1);

                plantController_Cooling.SetPosition(180, 380);
                plantController_Cooling.SensorArc1 = plantSensorArc_Cooling;
                Modify.SetWaterSideController(plantController_Cooling, WaterSideControllerSetup.Load, 0.1, 0.1);

                dynamic airSourceHeatPump = plantRoom.AddAirSourceHeatPump();
                airSourceHeatPump.SetPosition(0, 440);
                airSourceHeatPump.SetFuelSource(1, fuelSource_Electrical);
                airSourceHeatPump.SetFuelSource(2, fuelSource_Electrical);
                airSourceHeatPump.SetFuelSource(3, fuelSource_Electrical);
                airSourceHeatPump.Name = "DXCoil Units Air Source Heat Pump";
                plantRoom.AddPipe(refrigerantGroup, 1, airSourceHeatPump, 1);
                plantRoom.AddPipe(airSourceHeatPump, 1, refrigerantGroup, 1);

                for (int j = 1; j <= energyCentre.GetCalendar().GetDayTypeCount(); j++)
                {
                    global::TPD.PlantDayType plantDayType = plantRoom.GetEnergyCentre().GetCalendar().GetDayType(j);

                    //Water Side
                    plantController_Heating.AddDayType(plantDayType);
                    plantController_Max.AddDayType(plantDayType);
                    plantController_Load.AddDayType(plantDayType);
                    plantController_Temperature.AddDayType(plantDayType);
                    plantController_Cooling.AddDayType(plantDayType);
                }

                List<SystemPlantRoom> systemPlantRooms = systemEnergyCentre.GetSystemPlantRooms();
                if(systemPlantRooms != null && systemPlantRooms.Count != 0)
                {
                    foreach(SystemPlantRoom systemPlantRoom in systemPlantRooms)
                    {
                        List<AirSystem> airSystems = systemPlantRoom.GetSystems<AirSystem>();
                        if(airSystems != null && airSystems.Count != 0)
                        {
                            List<global::TPD.ISystemComponent> systemComponents_TPD = new List<global::TPD.ISystemComponent>();
                            foreach(AirSystem airSystem in airSystems)
                            {
                                global::TPD.System system = airSystem.ToTPD(plantRoom);
                                if(system == null)
                                {
                                    continue;
                                }

                                offset = new Point(0, 0);

                                Core.Systems.ISystemComponent systemComponent = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>(airSystem, ConnectorStatus.Unconnected, Core.Direction.In)?.FirstOrDefault();
                                if (systemComponent == null)
                                {
                                    continue;
                                }

                                List<Core.Systems.SystemComponent> systemComponents_Ordered = systemPlantRoom.GetOrderedSystemComponents(systemComponent as Core.Systems.SystemComponent, airSystem, Core.Direction.Out);
                                if (systemComponents_Ordered == null || systemComponents_Ordered.Count == 0)
                                {
                                    continue;
                                }

                                foreach (Core.Systems.SystemComponent systemComponent_Ordered in systemComponents_Ordered)
                                {
                                    global::TPD.ISystemComponent systemComponent_TPD = null;

                                    if (systemComponent_Ordered is DisplaySystemAirJunction)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemAirJunction)systemComponent_Ordered, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Ordered is DisplayAirSystemGroup)
                                    {
                                        global::TPD.Controller[] controllers = new global::TPD.Controller[0];

                                        systemComponent_TPD = ToTPD((DisplayAirSystemGroup)systemComponent_Ordered, systemPlantRoom, energyCentre, system, controllers, dHWGroup, electricalGroup_SmallPower, electricalGroup_Lighting) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Ordered is DisplaySystemCoolingCoil)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemCoolingCoil)systemComponent_Ordered, system, coolingGroup, energyCentre.GetDesignCondition(2)) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Ordered is DisplaySystemHeatingCoil)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemHeatingCoil)systemComponent_Ordered, system, heatingGroup, energyCentre.GetDesignCondition(3)) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Ordered is DisplaySystemExchanger)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemExchanger)systemComponent_Ordered, system) as global::TPD.ISystemComponent;
                                    }
                                    else if (systemComponent_Ordered is DisplaySystemFan)
                                    {
                                        systemComponent_TPD = ToTPD((DisplaySystemFan)systemComponent_Ordered, system, electricalGroup_Fans, plantSchedule_Occupancy) as global::TPD.ISystemComponent;
                                    }

                                    if (systemComponent_TPD != null)
                                    {
                                        systemComponents_TPD.Add(systemComponent_TPD);
                                    }
                                }
                            }
                        }
                    }
                }

                if(systemEnergyCentreConversionSettings == null)
                {
                    systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings();
                }

                if(!systemEnergyCentreConversionSettings.Simulate)
                {
                    return true;
                }

                plantRoom.SimulateEx(systemEnergyCentreConversionSettings.StartHour + 1, systemEnergyCentreConversionSettings.EndHour + 1, 0, energyCentre.ExternalPollutant.Value, 10.0, (int)tpdSimulationData.tpdSimulationDataLoad + (int)tpdSimulationData.tpdSimulationDataPipe + (int)tpdSimulationData.tpdSimulationDataDuct + (int)tpdSimulationData.tpdSimulationDataSimEvents, 0, 0);

                if (systemEnergyCentre != null)
                {

                    //TODO: [Jakub] NOT IMPLEMENTED
                    double totalConsumption = 0;
                    double CO2Emission = 0;
                    double cost = 0;
                    double unmetHours = 0;

                    global::TPD.WrResultSet wrResultSet = (global::TPD.WrResultSet)energyCentre.GetResultSet(global::TPD.tpdResultsPeriod.tpdResultsPeriodAnnual, 0, 0, 0, null);

                    int count;

                    count = wrResultSet.GetVectorSize(global::TPD.tpdResultVectorType.tpdConsumption);
                    for (int j = 1; j <= count; j++)
                    {
                        global::TPD.WrResultItem wrResultItem = (global::TPD.WrResultItem)wrResultSet.GetResultItem(global::TPD.tpdResultVectorType.tpdConsumption, j);
                        if (wrResultItem != null)
                        {
                            Array array = (Array)wrResultItem.GetValues();
                            if (array != null && array.Length != 0)
                            {
                                totalConsumption += (double)array.GetValue(0);
                            }
                        }
                    }

                    count = wrResultSet.GetVectorSize(global::TPD.tpdResultVectorType.tpdCost);
                    for (int j = 1; j <= count; j++)
                    {
                        global::TPD.WrResultItem wrResultItem = (global::TPD.WrResultItem)wrResultSet.GetResultItem(global::TPD.tpdResultVectorType.tpdCost, j);
                        if (wrResultItem != null)
                        {
                            Array array = (Array)wrResultItem.GetValues();
                            if (array != null && array.Length != 0)
                            {
                                cost += (double)array.GetValue(0);
                            }
                        }
                    }

                    count = wrResultSet.GetVectorSize(global::TPD.tpdResultVectorType.tpdCo2);
                    for (int j = 1; j <= count; j++)
                    {
                        global::TPD.WrResultItem wrResultItem = (global::TPD.WrResultItem)wrResultSet.GetResultItem(global::TPD.tpdResultVectorType.tpdCo2, j);
                        if (wrResultItem != null)
                        {
                            Array array = (Array)wrResultItem.GetValues();
                            if (array != null && array.Length != 0)
                            {
                                CO2Emission += (double)array.GetValue(0);
                            }
                        }
                    }

                    count = wrResultSet.GetVectorSize(global::TPD.tpdResultVectorType.tpdUnmetHours);
                    for (int j = 1; j <= count; j++)
                    {
                        global::TPD.WrResultItem wrResultItem = (global::TPD.WrResultItem)wrResultSet.GetResultItem(global::TPD.tpdResultVectorType.tpdUnmetHours, j);
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

                    //AnalyticalModelSimulationResult analyticalModelSimulationResult = new AnalyticalModelSimulationResult(analyticalModel.Name, Assembly.GetExecutingAssembly().GetName()?.Name, path_TPD);
                    //analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.AnnualTotalConsumption, totalConsumption);
                    //analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.AnnualCO2Emission, CO2Emission);
                    //analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.AnnualCost, cost);
                    //analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.AnnualUnmetHours, unmetHours);
                    //analyticalModel.AddResult(analyticalModelSimulationResult);
                }

                tPDDoc.Save();

            }

            return true;
        }
    }
}
