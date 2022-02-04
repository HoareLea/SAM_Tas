﻿using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void CreateTPD(this string path_TPD, string path_TSD, AnalyticalModel analyticalModel = null)
        {
            Point offset = new Point(0, 0);
            double circuitLength = 10;

            if (string.IsNullOrWhiteSpace(path_TPD) || string.IsNullOrWhiteSpace(path_TSD))
            {
                return;
            }

            if(!System.IO.File.Exists(path_TSD))
            {
                return;
            }

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {
                TPD.TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                if (tPDDoc != null)
                {
                    TPD.EnergyCentre energyCentre = tPDDoc.EnergyCentre;

                    TPD.PlantRoom plantRoom = energyCentre.PlantRoom("Main PlantRoom");
                    if(plantRoom == null)
                    {
                        plantRoom = energyCentre.AddPlantRoom();
                        plantRoom.Name = "Main PlantRoom";
                    }

                    energyCentre.AddTSDData(path_TSD, 0);

                    TPD.TSDData tSDData = energyCentre.GetTSDData(1);


                    Dictionary<string, Tuple<List<TPD.ZoneLoad>, CoolingSystem, HeatingSystem, VentilationSystem>> dictionary = new Dictionary<string, Tuple<List<TPD.ZoneLoad>, CoolingSystem, HeatingSystem, VentilationSystem>>();

                    if (analyticalModel != null)
                    {
                        List<Space> spaces = analyticalModel.GetSpaces();

                        for (int j = 1; j <= tSDData.GetZoneLoadCount(); j++)
                        {
                            TPD.ZoneLoad zoneLoad = tSDData.GetZoneLoad(j);

                            Space space = spaces.Find(x => x.Name == zoneLoad.Name);
                            if(space == null)
                            {
                                continue;
                            }

                            CoolingSystem coolingSystem = analyticalModel.GetRelatedObjects<CoolingSystem>(space).FirstOrDefault();
                            HeatingSystem heatingSystem = analyticalModel.GetRelatedObjects<HeatingSystem>(space).FirstOrDefault();
                            VentilationSystem ventilationSystem = analyticalModel.GetRelatedObjects<VentilationSystem>(space).FirstOrDefault();

                            List<string> names = new List<string>();
                            names.Add(ventilationSystem?.DisplayName());
                            names.Add(heatingSystem?.FullName);
                            names.Add(coolingSystem?.FullName);

                            names.RemoveAll(x => string.IsNullOrEmpty(x));

                            string name = string.Join(":", names);
                            if(name == null)
                            {
                                name = string.Empty;
                            }

                            if (!dictionary.TryGetValue(name, out Tuple<List<TPD.ZoneLoad>, CoolingSystem, HeatingSystem, VentilationSystem> zoneLoads))
                            {
                                zoneLoads = new Tuple<List<TPD.ZoneLoad>, CoolingSystem, HeatingSystem, VentilationSystem>(new List<TPD.ZoneLoad>(), coolingSystem, heatingSystem, ventilationSystem);
                                dictionary[name] = zoneLoads;
                            }

                            zoneLoads.Item1.Add(zoneLoad);
                        }

                    }
                    else
                    {
                        for (int j = 1; j <= tSDData.GetZoneLoadGroupCount(); j++)
                        {
                            TPD.ZoneLoadGroup zoneLoadGroup = tSDData.GetZoneLoadGroup(j);
                            if (zoneLoadGroup == null)
                            {
                                continue;
                            }

                            dictionary[zoneLoadGroup.Name] = new Tuple<List<TPD.ZoneLoad>, CoolingSystem, HeatingSystem, VentilationSystem>(new List<TPD.ZoneLoad>(), null, null, null);
                            for (int k = 1; k <= zoneLoadGroup.GetZoneLoadCount(); k++)
                            {
                                dictionary[zoneLoadGroup.Name].Item1.Add(zoneLoadGroup.GetZoneLoad(k));
                            }
                        }
                    }

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
                    if(heatingGroup == null)
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
                        dHWGroup.LoadDistribution = TPD.tpdLoadDistribution.tpdLoadDistributionEven;
                        dHWGroup.SetPosition(200, 140);
                    }

                    //Fuel Sources

                    dynamic fuelSource_Electrical = energyCentre.FuelSource("Grid Supplied Electricity");
                    if(fuelSource_Electrical == null)
                    {
                        fuelSource_Electrical = energyCentre.AddFuelSource();
                        fuelSource_Electrical.Name = "Grid Supplied Electricity";
                        fuelSource_Electrical.Description = "";
                        fuelSource_Electrical.CO2Factor = 0.519;
                        fuelSource_Electrical.Electrical = 1;
                        fuelSource_Electrical.TimeOfUseType = TPD.tpdTimeOfUseType.tpdTimeOfUseValue;
                        fuelSource_Electrical.PeakCost = 0.13;
                    }

                    dynamic fuelSource_Gas = energyCentre.FuelSource("Natural Gas");
                    if(fuelSource_Gas == null)
                    {
                        fuelSource_Gas = energyCentre.AddFuelSource();
                        fuelSource_Gas.Name = "Natural Gas";
                        fuelSource_Gas.Description = "";
                        fuelSource_Gas.CO2Factor = 0.216;
                        fuelSource_Gas.Electrical = 0;
                        fuelSource_Gas.TimeOfUseType = TPD.tpdTimeOfUseType.tpdTimeOfUseValue;
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
                        electricalGroup_Fans.ElectricalGroupType = TPD.tpdElectricalGroupType.tpdElectricalGroupFans;
                    }

                    //DXCoilUnits
                    dynamic electricalGroup_DXCoilUnits = plantRoom.ElectricalGroup("Electrical Group - DXCoil Units");
                    if (electricalGroup_DXCoilUnits == null)
                    {
                        electricalGroup_DXCoilUnits = plantRoom.AddElectricalGroup();
                        electricalGroup_DXCoilUnits.SetPosition(820, 0);
                        electricalGroup_DXCoilUnits.Name = "Electrical Group - DXCoil Units";
                        electricalGroup_DXCoilUnits.SetFuelSource(1, fuelSource_Electrical);
                        electricalGroup_DXCoilUnits.ElectricalGroupType = TPD.tpdElectricalGroupType.tpdElectricalGroupEquipment;
                    }

                    //Humidifiers
                    dynamic electricalGroup_Humidifiers = plantRoom.ElectricalGroup("Electrical Group - Humidifiers");
                    if (electricalGroup_Humidifiers == null)
                    {
                        electricalGroup_Humidifiers = plantRoom.AddElectricalGroup();
                        electricalGroup_Humidifiers.SetPosition(660, 0);
                        electricalGroup_Humidifiers.Name = "Electrical Group - Humidifiers";
                        electricalGroup_Humidifiers.SetFuelSource(1, fuelSource_Electrical);
                        electricalGroup_Humidifiers.ElectricalGroupType = TPD.tpdElectricalGroupType.tpdElectricalGroupEquipment;
                    }

                    //FanCoilUnits
                    dynamic electricalGroup_FanCoilUnits = plantRoom.ElectricalGroup("Electrical Group - FanCoil Units");
                    if (electricalGroup_FanCoilUnits == null)
                    {
                        electricalGroup_FanCoilUnits = plantRoom.AddElectricalGroup();
                        electricalGroup_FanCoilUnits.SetPosition(740, 0);
                        electricalGroup_FanCoilUnits.Name = "Electrical Group - FanCoil Units";
                        electricalGroup_FanCoilUnits.SetFuelSource(1, fuelSource_Electrical);
                        electricalGroup_FanCoilUnits.ElectricalGroupType = TPD.tpdElectricalGroupType.tpdElectricalGroupEquipment;
                    }

                    //Lighting
                    dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
                    if(electricalGroup_Lighting == null)
                    {
                        electricalGroup_Lighting = plantRoom.AddElectricalGroup();
                        electricalGroup_Lighting.SetPosition(offset.X + 500, offset.Y + 0);
                        electricalGroup_Lighting.Name = "Electrical Group - Lighting";
                        electricalGroup_Lighting.Description = "Internal Lighting";
                        electricalGroup_Lighting.SetFuelSource(1, fuelSource_Electrical);
                        electricalGroup_Lighting.ElectricalGroupType = TPD.tpdElectricalGroupType.tpdElectricalGroupLighting;
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
                        electricalGroup_SmallPower.ElectricalGroupType = TPD.tpdElectricalGroupType.tpdElectricalGroupEquipment;
                    }

                    //Schedules

                    //Occupancy
                    dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");
                    if(plantSchedule_Occupancy == null)
                    {
                        plantSchedule_Occupancy = energyCentre.AddSchedule(TPD.tpdScheduleType.tpdScheduleFunction);
                        plantSchedule_Occupancy.Name = "Occupancy Schedule";
                        plantSchedule_Occupancy.FunctionType = TPD.tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                        plantSchedule_Occupancy.FunctionLoads = 1024; // occupant sensible
                    }

                    //System
                    dynamic plantSchedule_System = energyCentre.PlantSchedule("System Schedule");
                    if(plantSchedule_System == null)
                    {
                        plantSchedule_System = energyCentre.AddSchedule(TPD.tpdScheduleType.tpdScheduleFunction);
                        plantSchedule_System.Name = "System Schedule";
                        plantSchedule_System.FunctionType = TPD.tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                        plantSchedule_System.FunctionLoads = 4 + 8 + 1024; // heating, cooling, occupant sensible
                    }

                    //Zone
                    dynamic plantSchedule_Zone = energyCentre.PlantSchedule("Zone Schedule");
                    if(plantSchedule_Zone == null)
                    {
                        plantSchedule_Zone = energyCentre.AddSchedule(TPD.tpdScheduleType.tpdScheduleFunction);
                        plantSchedule_Zone.Name = "Zone Schedule";
                        plantSchedule_Zone.FunctionType = TPD.tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
                        plantSchedule_Zone.FunctionLoads = 4 + 8 + 1024; // heating, cooling, occupant sensible
                    }

                    //Design Condition Load

                    dynamic designConditionLoad_Annual = energyCentre.DesignConditionLoad("Annual Design Condition");
                    if(designConditionLoad_Annual == null)
                    {
                        designConditionLoad_Annual = energyCentre.AddDesignCondition();
                        designConditionLoad_Annual.Name = "Annual Design Condition";
                        designConditionLoad_Annual.PrecondHours = 48;
                    }

                    //Components

                    //Heating MultiBoiler
                    dynamic multiBoiler_Heating = plantRoom.MultiBoiler("Heating Circuit Boiler");
                    if(multiBoiler_Heating == null)
                    {
                        multiBoiler_Heating = plantRoom.AddMultiBoiler();
                        multiBoiler_Heating.Name = "Heating Circuit Boiler";
                        multiBoiler_Heating.DesignPressureDrop = 25;
                        multiBoiler_Heating.DesignDeltaT = 11;
                        multiBoiler_Heating.Setpoint.Value = 71;
                        multiBoiler_Heating.SetFuelSource(1, fuelSource_Gas);
                        multiBoiler_Heating.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                        multiBoiler_Heating.Duty.SizeFraction = 1.0;
                        multiBoiler_Heating.Duty.AddDesignCondition(designConditionLoad_Annual);
                        multiBoiler_Heating.SetPosition(offset.X, offset.Y);
                    }

                    //Heating Pump
                    dynamic pump_Heating = plantRoom.Component<TPD.Pump>("Heating Circuit Pump");
                    if(pump_Heating == null)
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

                    TPD.PlantController plantController_Heating = plantRoom.AddController();
                    plantController_Heating.AddControlArc(pump_Heating);
                    dynamic plantSensorArc = plantController_Heating.AddSensorArcToComponent(heatingGroup, 1);

                    plantController_Heating.SetPosition(offset.X + 180, offset.Y + 110);
                    plantController_Heating.SensorArc1 = plantSensorArc;
                    SetWaterSideController(plantController_Heating, WaterSideControllerSetup.Load, 0.1, 0.1);

                    //DHW MultiBoiler
                    dynamic multiBoiler_DHW = plantRoom.MultiBoiler("DHW Circuit Boiler");
                    if(multiBoiler_DHW == null)
                    {
                        multiBoiler_DHW = plantRoom.AddMultiBoiler();
                        multiBoiler_DHW.DesignPressureDrop = 25;
                        multiBoiler_DHW.Setpoint.Value = 60;
                        multiBoiler_DHW.SetFuelSource(1, fuelSource_Gas);
                        multiBoiler_DHW.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                        multiBoiler_DHW.Duty.SizeFraction = 1.0;
                        multiBoiler_DHW.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                        multiBoiler_DHW.SetPosition(0, 140);
                    }

                    //DHW Pump
                    dynamic pump_DHW = plantRoom.Component<TPD.Pump>("DHW Circuit Pump");
                    if(pump_DHW == null)
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
                    junction_DHW_In.SetDirection(TPD.tpdDirection.tpdRightLeft);

                    dynamic junction_DHW_Out = plantRoom.AddJunction();
                    junction_DHW_Out.Name = "DHW Junction Out";
                    junction_DHW_Out.Description = "DHW Junction Out";
                    junction_DHW_Out.SetPosition(160, 210);
                    junction_DHW_Out.SetDirection(TPD.tpdDirection.tpdRightLeft);

                    plantRoom.AddPipe(junction_DHW_In, 1, multiBoiler_DHW, 1);
                    TPD.Pipe pipe_DHW_Multiboiler_Pump = plantRoom.AddPipe(multiBoiler_DHW, 1, pump_DHW, 1);
                    pipe_DHW_Multiboiler_Pump.AddNode(80, 180);
                    plantRoom.AddPipe(pump_DHW, 1, dHWGroup, 1);

                    TPD.Pipe pipe = plantRoom.AddPipe(dHWGroup, 1, junction_DHW_Out, 1);

                    dynamic plantController_Load = plantRoom.AddController();
                    plantController_Load.Name = "DHW Load Controller";
                    plantController_Load.SetPosition(80, 250);

                    dynamic plantSensorArc_Load = plantController_Load.AddSensorArcToComponent(dHWGroup, 1);
                    plantController_Load.SensorArc1 = plantSensorArc_Load;

                    SetWaterSideController(plantController_Load, WaterSideControllerSetup.Load, 0.1, 0.1);

                    dynamic plantController_Temperature = plantRoom.AddController();
                    plantController_Temperature.Name = "DHW Temperature Controller";
                    plantController_Temperature.SetPosition(140, 250);

                    SetWaterSideController(plantController_Temperature, WaterSideControllerSetup.TemperatureLowZero, 10, 0);

                    dynamic plantSensorArc_Temperature = plantController_Temperature.AddSensorArc(pipe);
                    plantController_Temperature.SensorArc1 = plantSensorArc_Temperature;

                    dynamic plantController_Max = plantRoom.AddController();
                    plantController_Max.Name = "DHW Max Controller";
                    plantController_Max.SetPosition(110, 210);
                    plantController_Max.ControlType = TPD.tpdControlType.tpdControlMin;
                    plantController_Max.AddControlArc(pump_DHW);
                    plantController_Max.AddChainArc(plantController_Load);
                    plantController_Max.AddChainArc(plantController_Temperature);

                    dynamic multiChiller = plantRoom.AddMultiChiller();
                    multiChiller.Name = "Cooling Circuit Chiller";
                    multiChiller.DesignPressureDrop = 25;
                    multiChiller.DesignDeltaT = 6;
                    multiChiller.Setpoint.Value = 10;
                    multiChiller.SetFuelSource(1, fuelSource_Electrical);
                    multiChiller.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
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
                    SetWaterSideController(plantController_Cooling, WaterSideControllerSetup.Load, 0.1, 0.1);

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
                        TPD.PlantDayType plantDayType = plantRoom.GetEnergyCentre().GetCalendar().GetDayType(j);

                        //Water Side
                        plantController_Heating.AddDayType(plantDayType);
                        plantController_Max.AddDayType(plantDayType);
                        plantController_Load.AddDayType(plantDayType);
                        plantController_Temperature.AddDayType(plantDayType);
                        plantController_Cooling.AddDayType(plantDayType);
                    }

                    foreach (KeyValuePair<string, Tuple<List<TPD.ZoneLoad>, CoolingSystem, HeatingSystem, VentilationSystem>> keyValuePair in dictionary)
                    {
                        CreateTPD(energyCentre, keyValuePair.Key, keyValuePair.Value.Item1, keyValuePair.Value.Item4, keyValuePair.Value.Item3, keyValuePair.Value.Item2);
                    }

                    plantRoom.SimulateEx(1, 8760, 0, energyCentre.ExternalPollutant.Value, 10.0, (int)TPD.tpdSimulationData.tpdSimulationDataLoad + (int)TPD.tpdSimulationData.tpdSimulationDataPipe, 0, 0);

                    if(analyticalModel != null)
                    {
                        double totalConsumption = 0;
                        double CO2Emission = 0;
                        double cost = 0;
                        double unmetHours = 0;

                        TPD.WrResultSet wrResultSet = (TPD.WrResultSet)energyCentre.GetResultSet(TPD.tpdResultsPeriod.tpdResultsPeriodAnnual, 0, 0, 0, null);

                        int count;

                        count = wrResultSet.GetVectorSize(TPD.tpdResultVectorType.tpdConsumption);
                        for (int j = 1; j <= count; j++)
                        {
                            TPD.WrResultItem wrResultItem = (TPD.WrResultItem)wrResultSet.GetResultItem(TPD.tpdResultVectorType.tpdConsumption, j);
                            if (wrResultItem != null)
                            {
                                Array array = (Array)wrResultItem.GetValues();
                                if (array != null && array.Length != 0)
                                {
                                    totalConsumption += (double)array.GetValue(0);
                                }
                            }
                        }

                        count = wrResultSet.GetVectorSize(TPD.tpdResultVectorType.tpdCost);
                        for (int j = 1; j <= count; j++)
                        {
                            TPD.WrResultItem wrResultItem = (TPD.WrResultItem)wrResultSet.GetResultItem(TPD.tpdResultVectorType.tpdCost, j);
                            if (wrResultItem != null)
                            {
                                Array array = (Array)wrResultItem.GetValues();
                                if (array != null && array.Length != 0)
                                {
                                    cost += (double)array.GetValue(0);
                                }
                            }
                        }

                        count = wrResultSet.GetVectorSize(TPD.tpdResultVectorType.tpdCo2);
                        for (int j = 1; j <= count; j++)
                        {
                            TPD.WrResultItem wrResultItem = (TPD.WrResultItem)wrResultSet.GetResultItem(TPD.tpdResultVectorType.tpdCo2, j);
                            if (wrResultItem != null)
                            {
                                Array array = (Array)wrResultItem.GetValues();
                                if (array != null && array.Length != 0)
                                {
                                    CO2Emission += (double)array.GetValue(0);
                                }
                            }
                        }

                        count = wrResultSet.GetVectorSize(TPD.tpdResultVectorType.tpdUnmetHours);
                        for (int j = 1; j <= count; j++)
                        {
                            TPD.WrResultItem wrResultItem = (TPD.WrResultItem)wrResultSet.GetResultItem(TPD.tpdResultVectorType.tpdUnmetHours, j);
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

                        AnalyticalModelSimulationResult analyticalModelSimulationResult = new AnalyticalModelSimulationResult(analyticalModel.Name, Assembly.GetExecutingAssembly().GetName()?.Name, path_TPD);
                        analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.AnnualTotalConsumption, totalConsumption);
                        analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.AnnualCO2Emission, CO2Emission);
                        analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.AnnualCost, cost);
                        analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.AnnualUnmetHours, unmetHours);
                        analyticalModel.AddResult(analyticalModelSimulationResult);
                    }

                    tPDDoc.Save();
                }
            }

        }

        private static void CreateTPD(this TPD.EnergyCentre energyCentre, string name, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            if(string.IsNullOrWhiteSpace(name) || energyCentre == null || zoneLoads == null || zoneLoads.Count() == 0)
            {
                return;
            }

            if(name.StartsWith("UV"))
            {
                CreateTPD_UV(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem);
            }
            else if(name.StartsWith("NV"))
            {
                CreateTPD_NV(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem);
            }
            else if (name.StartsWith("EOL"))
            {
                CreateTPD_EOL(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem);
            }
            else if (name.StartsWith("EOC"))
            {
                CreateTPD_EOC(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem);
            }
            else if (name.StartsWith("CAV"))
            {
                CreateTPD_CAV(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem);
            }
            else if (name.StartsWith("MV"))
            {
                CreateTPD_MV(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem);
            }
            else if (name.StartsWith("MVRE"))
            {
                CreateTPD_MVRE(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem);
            }
            else if (name.StartsWith("DISP"))
            {
                CreateTPD_VAV(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem, true);
            }
            else if (name.StartsWith("VAV"))
            {
                CreateTPD_VAV(energyCentre, zoneLoads, ventilationSystem, heatingSystem, coolingSystem, false);
            }
        }

        private static void CreateTPD_UV(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if(plantRoom == null)
            {
                return;
            }

            Point offset = new Point(0, 0);

            TPD.System system = plantRoom.AddSystem();
            system.Name = "UV";
            system.Multiplicity = zoneLoads.Count();

            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");
            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            dynamic junction_In = system.AddJunction();
            junction_In.SetPosition(offset.X + 100, offset.Y + 120);
            junction_In.SetDirection(TPD.tpdDirection.tpdLeftRight);

            dynamic junction_Out = system.AddJunction();
            junction_Out.SetPosition(offset.X + 260, offset.Y + 120);
            junction_Out.SetDirection(TPD.tpdDirection.tpdLeftRight);

            dynamic zone = system.AddSystemZone();
            zone.SetPosition(offset.X + 160, offset.Y + 100);

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[3];
            systemComponents[0] = (TPD.SystemComponent)zone;
            systemComponents[1] = (TPD.SystemComponent)junction_In;
            systemComponents[2] = (TPD.SystemComponent)junction_Out;

            system.AddDuct(junction_In, 1, zone, 1);
            system.AddDuct(zone, 1, junction_Out, 1);

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int i = 1;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic systemZone_Group = componentGroup.GetComponent(i);
                systemZone_Group.AddZoneLoad(zoneLoad);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                //systemZone.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowDeltaT;
                systemZone_Group.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                if (electricalGroup_SmallPower != null)
                {
                    (systemZone_Group as dynamic).SetElectricalGroup1(electricalGroup_SmallPower);
                }

                if (electricalGroup_Lighting != null)
                {
                    (systemZone_Group as dynamic).SetElectricalGroup2(electricalGroup_Lighting);
                }

                if(dHWGroup != null)
                {
                    systemZone_Group.SetDHWGroup(dHWGroup);
                }

                AddComponents(systemZone_Group as TPD.SystemZone, energyCentre, heatingSystem, coolingSystem);

                i += 3;
            }

        }

        private static void CreateTPD_NV(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            Point offset = new Point(0, 0);

            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            TPD.System system = plantRoom.AddSystem();
            system.Name = "NV";
            system.Multiplicity = zoneLoads.Count();

            dynamic zone = system.AddSystemZone();
            zone.SetPosition(offset.X + 630, offset.Y + 80);

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[1];
            systemComponents[0] = (TPD.SystemComponent)zone;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int i = 1;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic systemZone_Group = componentGroup.GetComponent(i);
                systemZone_Group.AddZoneLoad(zoneLoad);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                systemZone_Group.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                if (electricalGroup_SmallPower != null)
                {
                    (systemZone_Group as dynamic).SetElectricalGroup1(electricalGroup_SmallPower);
                }

                if (electricalGroup_Lighting != null)
                {
                    (systemZone_Group as dynamic).SetElectricalGroup2(electricalGroup_Lighting);
                }

                if (dHWGroup != null)
                {
                    systemZone_Group.SetDHWGroup(dHWGroup);
                }

                AddComponents(systemZone_Group as TPD.SystemZone, energyCentre, heatingSystem, coolingSystem);

                i++;
            }
        }

        private static void CreateTPD_EOL(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            Point offset = new Point(0, 0);

            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            string name = ventilationSystem?.DisplayName();
            if (string.IsNullOrEmpty(name))
            {
                name = "EOL";
            }

            TPD.System system = plantRoom.AddSystem();
            system.Name = name;
            system.Multiplicity = zoneLoads.Count();

            dynamic plantSchedule = energyCentre.AddSchedule(TPD.tpdScheduleType.tpdScheduleFunction);
            plantSchedule.Name = "System Schedule";
            plantSchedule.FunctionType = TPD.tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
            plantSchedule.FunctionLoads = 4 + 8 + 1024; // heating, cooling, occupant sensible

            dynamic zone = system.AddSystemZone();
            zone.SetPosition(offset.X + 0, offset.Y + 0);

            dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            dynamic fan = system.AddFan();
            fan.name = "Fresh Air Fan";
            fan.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
            fan.DesignFlowRate.Value = 150;
            fan.OverallEfficiency.Value = 1;
            fan.Pressure = 1000;
            fan.HeatGainFactor = 0;
            fan.SetElectricalGroup1(electricalGroup_Fans);
            fan.PartLoad.Value = 0;
            fan.PartLoad.ClearModifiers();
            fan.SetSchedule(plantSchedule);
            fan.SetPosition(offset.X + 140, offset.Y + 10);
            fan.SetDirection(TPD.tpdDirection.tpdLeftRight);
            fan.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

            TPD.ProfileDataModifierTable profileDataModifierTable = fan.PartLoad.AddModifierTable();
            profileDataModifierTable.Name = "Fan Part Load Curve";
            profileDataModifierTable.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable.Clear();
            profileDataModifierTable.AddPoint(0, 0);
            profileDataModifierTable.AddPoint(10, 3);
            profileDataModifierTable.AddPoint(20, 7);
            profileDataModifierTable.AddPoint(30, 13);
            profileDataModifierTable.AddPoint(40, 21);
            profileDataModifierTable.AddPoint(50, 30);
            profileDataModifierTable.AddPoint(60, 41);
            profileDataModifierTable.AddPoint(70, 54);
            profileDataModifierTable.AddPoint(80, 68);
            profileDataModifierTable.AddPoint(90, 83);
            profileDataModifierTable.AddPoint(100, 100);

            dynamic junction_Out = system.AddJunction();
            junction_Out.SetPosition(offset.X + 220, offset.Y + 10);
            junction_Out.SetDirection(TPD.tpdDirection.tpdLeftRight);

            dynamic junction_In = system.AddJunction();
            junction_In.SetPosition(offset.X - 60, offset.Y + 20);
            junction_In.SetDirection(TPD.tpdDirection.tpdLeftRight);

            dynamic damper = system.AddDamper();
            damper.SetPosition(offset.X + 80, offset.Y + 10);
            damper.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

            system.AddDuct(junction_In, 1, zone, 1);
            system.AddDuct(zone, 1, damper, 1);
            system.AddDuct(damper, 1, fan, 1);
            system.AddDuct(fan, 1, junction_Out, 1);

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[3];
            systemComponents[0] = (TPD.SystemComponent)zone;
            systemComponents[1] = (TPD.SystemComponent)damper;
            systemComponents[2] = (TPD.SystemComponent)fan;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");


            List<string> names = new List<string>();
            for(int k=1; k < componentGroup.GetComponentCount(); k ++)
            {
                TPD.SystemComponent systemComponent = componentGroup.GetComponent(k);
                names.Add((systemComponent as dynamic)?.name);
            }

            int i = 1;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                TPD.SystemZone systemZone_Group = componentGroup.GetComponent(i * 3) as TPD.SystemZone;
                (systemZone_Group as dynamic).AddZoneLoad(zoneLoad);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowACH;
                systemZone_Group.FlowRate.Value = 5;
                //(systemZone as dynamic).SetDHWGroup(tpdDHWGrp);
                if (electricalGroup_SmallPower != null)
                {
                    (systemZone_Group as dynamic).SetElectricalGroup1(electricalGroup_SmallPower);
                }

                if (electricalGroup_Lighting != null)
                {
                    (systemZone_Group as dynamic).SetElectricalGroup2(electricalGroup_Lighting);
                }

                if (dHWGroup != null)
                {
                    (systemZone_Group as dynamic).SetDHWGroup(dHWGroup);
                }

                TPD.SizedFlowVariable sizedFlowVariable_FreshAir = systemZone_Group.FreshAir;
                sizedFlowVariable_FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                TPD.Damper damper_Zone = componentGroup.GetComponent((i * 3) + 1) as TPD.Damper;
                damper_Zone.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                AddComponents(systemZone_Group, energyCentre, heatingSystem, coolingSystem);

                i++;
            }
        }

        private static void CreateTPD_EOC(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            Point offset = new Point(0, 0);

            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            string name = ventilationSystem?.DisplayName();
            if (string.IsNullOrEmpty(name))
            {
                name = "EOC";
            }

            TPD.System system = plantRoom.AddSystem();
            system.Name = name;
            system.Multiplicity = zoneLoads.Count();

            dynamic plantSchedule = energyCentre.AddSchedule(TPD.tpdScheduleType.tpdScheduleFunction);
            plantSchedule.Name = "System Schedule";
            plantSchedule.FunctionType = TPD.tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;
            plantSchedule.FunctionLoads = 4 + 8 + 1024; // heating, cooling, occupant sensible

            dynamic zone = system.AddSystemZone();
            zone.SetPosition(offset.X + 0, offset.Y + 0);

            dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            dynamic fan = system.AddFan();
            fan.name = "Fresh Air Fan";
            fan.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
            fan.DesignFlowRate.Value = 150;
            fan.OverallEfficiency.Value = 1;
            fan.Pressure = 1000;
            fan.HeatGainFactor = 0;
            fan.SetElectricalGroup1(electricalGroup_Fans);
            fan.PartLoad.Value = 0;
            fan.PartLoad.ClearModifiers();
            fan.SetSchedule(plantSchedule);
            fan.SetPosition(offset.X + 140, offset.Y + 10);
            fan.SetDirection(TPD.tpdDirection.tpdLeftRight);
            fan.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

            TPD.ProfileDataModifierTable profileDataModifierTable = fan.PartLoad.AddModifierTable();
            profileDataModifierTable.Name = "Fan Part Load Curve";
            profileDataModifierTable.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable.Clear();
            profileDataModifierTable.AddPoint(0, 0);
            profileDataModifierTable.AddPoint(10, 3);
            profileDataModifierTable.AddPoint(20, 7);
            profileDataModifierTable.AddPoint(30, 13);
            profileDataModifierTable.AddPoint(40, 21);
            profileDataModifierTable.AddPoint(50, 30);
            profileDataModifierTable.AddPoint(60, 41);
            profileDataModifierTable.AddPoint(70, 54);
            profileDataModifierTable.AddPoint(80, 68);
            profileDataModifierTable.AddPoint(90, 83);
            profileDataModifierTable.AddPoint(100, 100);

            dynamic junction_Out = system.AddJunction();
            junction_Out.SetPosition(offset.X + 220, offset.Y + 10);
            junction_Out.SetDirection(TPD.tpdDirection.tpdLeftRight);

            dynamic junction_In = system.AddJunction();
            junction_In.SetPosition(offset.X - 60, offset.Y + 20);
            junction_In.SetDirection(TPD.tpdDirection.tpdLeftRight);

            dynamic damper = system.AddDamper();
            damper.SetPosition(offset.X + 80, offset.Y + 10);
            damper.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

            system.AddDuct(junction_In, 1, zone, 1);
            system.AddDuct(zone, 1, damper, 1);
            system.AddDuct(damper, 1, fan, 1);
            system.AddDuct(fan, 1, junction_Out, 1);

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[2];
            systemComponents[0] = (TPD.SystemComponent)zone;
            systemComponents[1] = (TPD.SystemComponent)damper;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int i = 1;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                TPD.SystemZone systemZone_Group = componentGroup.GetComponent(i + 2) as TPD.SystemZone;
                (systemZone_Group as dynamic).AddZoneLoad(zoneLoad);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowACH;
                systemZone_Group.FlowRate.Value = 5;
                //(systemZone as dynamic).SetDHWGroup(tpdDHWGrp);
                if (electricalGroup_SmallPower != null)
                {
                    (systemZone_Group as dynamic).SetElectricalGroup1(electricalGroup_SmallPower);
                }

                if (electricalGroup_Lighting != null)
                {
                    (systemZone_Group as dynamic).SetElectricalGroup2(electricalGroup_Lighting);
                }

                if (dHWGroup != null)
                {
                    (systemZone_Group as dynamic).SetDHWGroup(dHWGroup);
                }

                TPD.SizedFlowVariable sizedFlowVariable_FreshAir = systemZone_Group.FreshAir;
                sizedFlowVariable_FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                TPD.Damper damper_Zone = componentGroup.GetComponent(i + 3) as TPD.Damper;
                damper_Zone.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                AddComponents(systemZone_Group, energyCentre, heatingSystem, coolingSystem);

                i += 2;
            }

        }

        private static void CreateTPD_CAV(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");
            dynamic plantSchedule_System = energyCentre.PlantSchedule("System Schedule");

            dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");

            dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            string name = ventilationSystem?.DisplayName();
            if (string.IsNullOrEmpty(name))
            {
                name = "CAV";
            }
  
            TPD.System system = plantRoom.AddSystem();
            system.Name = name;
            system.Multiplicity = zoneLoads.Count();

            dynamic junction_FreshAir = system.AddJunction();
            junction_FreshAir.Name = "Junction Fresh Air";
            junction_FreshAir.Description = "Junction Fresh Air";
            junction_FreshAir.SetPosition(0, 110);

            dynamic junction_ExhaustAir = system.AddJunction();
            junction_ExhaustAir.Name = "Junction Exhaust Air";
            junction_ExhaustAir.Description = "Junction Exhaust Air";
            junction_ExhaustAir.SetDirection(TPD.tpdDirection.tpdRightLeft);
            junction_ExhaustAir.SetPosition(0, 190);

            dynamic exchanger = system.AddExchanger();
            exchanger.ExchLatType = TPD.tpdExchangerLatentType.tpdExchangerLatentEnthalpy;
            exchanger.LatentEfficiency.Value = 0.0;
            exchanger.SensibleEfficiency.Value = 0.7;
            exchanger.Setpoint.Value = 14;
            exchanger.Flags = TPD.tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;
            exchanger.SetPosition(160, 100);

            dynamic fan_FreashAir = system.AddFan();
            fan_FreashAir.name = "Fresh Air Fan";
            fan_FreashAir.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
            fan_FreashAir.DesignFlowRate.Value = 150;
            fan_FreashAir.OverallEfficiency.Value = 1;
            fan_FreashAir.Pressure = 1000;
            fan_FreashAir.HeatGainFactor = 0;
            fan_FreashAir.SetElectricalGroup1(electricalGroup_Fans);
            fan_FreashAir.PartLoad.Value = 0;
            fan_FreashAir.PartLoad.ClearModifiers();
            fan_FreashAir.SetSchedule(plantSchedule_System);
            fan_FreashAir.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_FreashAir.SetPosition(390, 100);

            TPD.ProfileDataModifierTable profileDataModifierTable_FreshAir = fan_FreashAir.PartLoad.AddModifierTable();
            profileDataModifierTable_FreshAir.Name = "Fan Part Load Curve";
            profileDataModifierTable_FreshAir.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_FreshAir.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_FreshAir.Clear();
            profileDataModifierTable_FreshAir.AddPoint(0, 0);
            profileDataModifierTable_FreshAir.AddPoint(10, 3);
            profileDataModifierTable_FreshAir.AddPoint(20, 7);
            profileDataModifierTable_FreshAir.AddPoint(30, 13);
            profileDataModifierTable_FreshAir.AddPoint(40, 21);
            profileDataModifierTable_FreshAir.AddPoint(50, 30);
            profileDataModifierTable_FreshAir.AddPoint(60, 41);
            profileDataModifierTable_FreshAir.AddPoint(70, 54);
            profileDataModifierTable_FreshAir.AddPoint(80, 68);
            profileDataModifierTable_FreshAir.AddPoint(90, 83);
            profileDataModifierTable_FreshAir.AddPoint(100, 100);

            dynamic fan_Return = system.AddFan();
            fan_Return.name = "Return Air Fan";
            fan_Return.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.ExhaustUnitName);
            fan_Return.DesignFlowRate.Value = 150;
            fan_Return.OverallEfficiency.Value = 1;
            fan_Return.Pressure = 600;
            fan_Return.HeatGainFactor = 0;
            fan_Return.SetElectricalGroup1(electricalGroup_Fans);
            fan_Return.PartLoad.Value = 0;
            fan_Return.PartLoad.ClearModifiers();
            fan_Return.SetSchedule(plantSchedule_System);
            fan_Return.SetDirection(TPD.tpdDirection.tpdRightLeft);
            fan_Return.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_Return.SetPosition(600, 240);

            dynamic profileDataModifierTable_Return = fan_Return.PartLoad.AddModifierTable();
            profileDataModifierTable_Return.Name = "Fan Part Load Curve";
            profileDataModifierTable_Return.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_Return.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_Return.Clear();
            profileDataModifierTable_Return.AddPoint(0, 0);
            profileDataModifierTable_Return.AddPoint(10, 3);
            profileDataModifierTable_Return.AddPoint(20, 7);
            profileDataModifierTable_Return.AddPoint(30, 13);
            profileDataModifierTable_Return.AddPoint(40, 21);
            profileDataModifierTable_Return.AddPoint(50, 30);
            profileDataModifierTable_Return.AddPoint(60, 41);
            profileDataModifierTable_Return.AddPoint(70, 54);
            profileDataModifierTable_Return.AddPoint(80, 68);
            profileDataModifierTable_Return.AddPoint(90, 83);
            profileDataModifierTable_Return.AddPoint(100, 100);

            dynamic damper = system.AddDamper();
            damper.SetPosition(530, 90);

            dynamic systemZone = system.AddSystemZone();
            systemZone.SetPosition(630, 80);

            dynamic optimiser = system.AddOptimiser();
            optimiser.SetSchedule(plantSchedule_Occupancy);
            optimiser.ScheduleMode = TPD.tpdOptimiserScheduleMode.tpdOptimiserScheduleRecirc;
            optimiser.MinFreshAirType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir;
            optimiser.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            optimiser.SetPosition(240, 100);

            dynamic junction_Return = system.AddJunction();
            junction_Return.Name = "Junction Return";
            junction_Return.Description = "Junction Return";
            junction_Return.SetPosition(240, 200);
            junction_Return.SetDirection(TPD.tpdDirection.tpdBottomTop);

            dynamic heatingCoil = system.AddHeatingCoil();
            heatingCoil.Setpoint.Value = 14;
            heatingCoil.SetHeatingGroup(heatingGroup);
            heatingCoil.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            heatingCoil.Duty.SizeFraction = 1.0;
            heatingCoil.Duty.AddDesignCondition(energyCentre.GetDesignCondition(1));
            heatingCoil.MaximumOffcoil.Value = 28;
            heatingCoil.SetPosition(350, 100);

            dynamic coolingCoil = system.AddCoolingCoil();
            coolingCoil.SetCoolingGroup(coolingGroup);
            coolingCoil.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            coolingCoil.Duty.SizeFraction = 1.0;
            coolingCoil.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
            coolingCoil.BypassFactor.Value = 0.1;
            coolingCoil.MinimumOffcoil.Value = 16;
            coolingCoil.SetPosition(310, 100);

            system.AddDuct(junction_FreshAir, 1, exchanger, 1);
            system.AddDuct(exchanger, 1, optimiser, 1);
            system.AddDuct(optimiser, 1, coolingCoil, 1);
            system.AddDuct(coolingCoil, 1, heatingCoil, 1);

            TPD.Duct duct_OffCoils = system.AddDuct(heatingCoil, 1, fan_FreashAir, 1);
            system.AddDuct(fan_FreashAir, 1, damper, 1);
            system.AddDuct(damper, 1, systemZone, 1);

            TPD.Duct duct_ZoneOut = system.AddDuct(systemZone, 1, fan_Return, 1);
            duct_ZoneOut.AddNode(680, 110);
            duct_ZoneOut.AddNode(680, 260);
            duct_ZoneOut = system.AddDuct(fan_Return, 1, junction_Return, 1);
            duct_ZoneOut.AddNode(250, 250);

            system.AddDuct(junction_Return, 1, exchanger, 2);
            system.AddDuct(junction_Return, 1, optimiser, 2);
            system.AddDuct(exchanger, 2, junction_ExhaustAir, 1);

            TPD.Controller controller_HeatingGroup = system.AddController();
            controller_HeatingGroup.Name = "Heating Group";
            controller_HeatingGroup.SetPosition(570, 160);

            TPD.Controller controller_HeatingGroupCombiner = system.AddController();
            controller_HeatingGroupCombiner.Name = "Heat Group Combiner";
            controller_HeatingGroupCombiner.SetPosition(370, 160);
            controller_HeatingGroupCombiner.AddControlArc(heatingCoil).AddNode(360, 170);
            controller_HeatingGroupCombiner.AddChainArc(controller_HeatingGroup).AddNode(380, 170);
            controller_HeatingGroupCombiner.ControlType = TPD.tpdControlType.tpdControlMin;

            TPD.Controller controller_CoolingGroup = system.AddController();
            controller_CoolingGroup.Name = "Cooling Group";
            controller_CoolingGroup.SetPosition(540, 180);

            TPD.Controller controller_CoolingGroupCombiner = system.AddController();
            controller_CoolingGroupCombiner.Name = "Cooling Group Combiner";
            controller_CoolingGroupCombiner.SetPosition(330, 180);
            controller_CoolingGroupCombiner.AddControlArc(coolingCoil).AddNode(320, 190);
            controller_CoolingGroupCombiner.AddChainArc(controller_CoolingGroup).AddNode(340, 190);
            controller_CoolingGroupCombiner.ControlType = TPD.tpdControlType.tpdControlMax;

            TPD.Controller controller_PassThroughExchanger = system.AddController();
            controller_PassThroughExchanger.Name = "Pass Through Ex";
            controller_PassThroughExchanger.SetPosition(320, 40);
            controller_PassThroughExchanger.AddControlArc(exchanger).AddNode(180, 50);

            TPD.SensorArc sensorArc_HeatingGroup = controller_HeatingGroup.AddSensorArcToComponent(systemZone, 1);
            sensorArc_HeatingGroup.AddNode(645, 170);
            controller_HeatingGroup.SensorArc1 = sensorArc_HeatingGroup;
            SetAirSideController(controller_HeatingGroup, AirSideControllerSetup.ThermLL, 0, 0.5);

            TPD.SensorArc sensorArc_CoolingGroup = controller_CoolingGroup.AddSensorArcToComponent(systemZone, 1);
            sensorArc_CoolingGroup.AddNode(645, 190);
            controller_CoolingGroup.SensorArc1 = sensorArc_CoolingGroup;
            SetAirSideController(controller_CoolingGroup, AirSideControllerSetup.ThermUL, 0, 0.5);

            TPD.SensorArc sensorArc_PassThroughExchanger = controller_PassThroughExchanger.AddSensorArc(duct_OffCoils);
            sensorArc_PassThroughExchanger.AddNode(380, 50);
            controller_PassThroughExchanger.SensorArc1 = sensorArc_PassThroughExchanger;
            SetAirSideController(controller_PassThroughExchanger, AirSideControllerSetup.TempPassThrough);

            dynamic controller_Optimiser = system.AddController();
            controller_Optimiser.SetPosition(320, 70);
            controller_Optimiser.AddControlArc(optimiser).AddNode(270, 80);

            TPD.SensorArc sensorArc_Optimiser = controller_Optimiser.AddSensorArc(duct_OffCoils);
            sensorArc_Optimiser.AddNode(380, 80);

            controller_Optimiser.SensorArc1 = sensorArc_Optimiser;
            SetAirSideController(controller_Optimiser, AirSideControllerSetup.TempPassThrough);
            controller_Optimiser.Name = "Pass Through Optimiser";

            TPD.PlantDayType plantDayType = null;
            for (int i = 1; i <= energyCentre.GetCalendar().GetDayTypeCount(); i++)
            {
                plantDayType = energyCentre.GetCalendar().GetDayType(i);

                // Air Side
                controller_HeatingGroupCombiner.AddDayType(plantDayType);
                controller_HeatingGroup.AddDayType(plantDayType);
                controller_CoolingGroupCombiner.AddDayType(plantDayType);
                controller_CoolingGroup.AddDayType(plantDayType);
                controller_PassThroughExchanger.AddDayType(plantDayType);
                controller_Optimiser.AddDayType(plantDayType);
            }

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[2];
            systemComponents[0] = (TPD.SystemComponent)damper;
            systemComponents[1] = (TPD.SystemComponent)systemZone;

            TPD.Controller[] controllers = new TPD.Controller[2];
            controllers[0] = (TPD.Controller)controller_HeatingGroup;
            controllers[1] = (TPD.Controller)controller_CoolingGroup;

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int index = 0;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic damper_Group = componentGroup.GetComponent(2 + (index * 2) + 1);
                damper_Group.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                // System Zone
                dynamic systemZone_Group = componentGroup.GetComponent(2 + (index * 2) + 2);
                systemZone_Group.AddZoneLoad(zoneLoad);
                systemZone_Group.SetDHWGroup(dHWGroup);
                systemZone_Group.SetElectricalGroup1(electricalGroup_SmallPower);
                systemZone_Group.SetElectricalGroup2(electricalGroup_Lighting);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowDeltaT;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                systemZone_Group.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FreshAir.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FreshAir.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                AddComponents(systemZone_Group as TPD.SystemZone, energyCentre, heatingSystem, coolingSystem);

                index++;
            }
        }

        private static void CreateTPD_VAV(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem, bool displacementVent = false)
        {
            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");

            dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");

            dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            string name = ventilationSystem?.DisplayName();
            if (string.IsNullOrEmpty(name))
            {
                name = displacementVent ? "DISP" : "VAV";
            }

            TPD.System system = plantRoom.AddSystem();
            system.Name = name;
            system.Multiplicity = zoneLoads.Count();

            //dynamic junction_Zero = system.AddJunction();
            //junction_Zero.Description = "0, 0";
            //junction_Zero.SetPosition(0, 0);

            //dynamic junction_Fourty = system.AddJunction();
            //junction_Fourty.Description = "40, 40";
            //junction_Fourty.SetPosition(40, 40);

            dynamic junction_FreshAir = system.AddJunction();
            junction_FreshAir.Name = "Junction Fresh Air";
            junction_FreshAir.Description = "Junction Fresh Air 0,110";
            junction_FreshAir.SetPosition(0, 110);

            dynamic junction_ExhaustAir = system.AddJunction();
            junction_ExhaustAir.Name = "Junction Exhaust Air";
            junction_ExhaustAir.Description = "Junction Exhaust Air";
            junction_ExhaustAir.SetDirection(TPD.tpdDirection.tpdRightLeft);
            junction_ExhaustAir.SetPosition(0, 190);

            dynamic exchanger = system.AddExchanger();
            exchanger.ExchLatType = TPD.tpdExchangerLatentType.tpdExchangerLatentEnthalpy;
            exchanger.LatentEfficiency.Value = 0.0;
            exchanger.SensibleEfficiency.Value = 0.7;
            exchanger.Setpoint.Value = 14;
            exchanger.Flags = TPD.tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;
            exchanger.SetPosition(160, 100);

            dynamic fan_FreashAir = system.AddFan();
            fan_FreashAir.name = "Fresh Air Fan";
            fan_FreashAir.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
            fan_FreashAir.DesignFlowRate.Value = 150;
            fan_FreashAir.OverallEfficiency.Value = 1;
            fan_FreashAir.Pressure = 1000;
            fan_FreashAir.HeatGainFactor = 1;
            fan_FreashAir.SetElectricalGroup1(electricalGroup_Fans);
            fan_FreashAir.PartLoad.Value = 0;
            fan_FreashAir.PartLoad.ClearModifiers();
            fan_FreashAir.SetSchedule(plantSchedule_Occupancy);
            fan_FreashAir.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_FreashAir.SetPosition(390, 100);

            TPD.ProfileDataModifierTable profileDataModifierTable_FreshAir = fan_FreashAir.PartLoad.AddModifierTable();
            profileDataModifierTable_FreshAir.Name = "Fan Part Load Curve";
            profileDataModifierTable_FreshAir.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_FreshAir.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_FreshAir.Clear();
            profileDataModifierTable_FreshAir.AddPoint(0, 0);
            profileDataModifierTable_FreshAir.AddPoint(10, 3);
            profileDataModifierTable_FreshAir.AddPoint(20, 7);
            profileDataModifierTable_FreshAir.AddPoint(30, 13);
            profileDataModifierTable_FreshAir.AddPoint(40, 21);
            profileDataModifierTable_FreshAir.AddPoint(50, 30);
            profileDataModifierTable_FreshAir.AddPoint(60, 41);
            profileDataModifierTable_FreshAir.AddPoint(70, 54);
            profileDataModifierTable_FreshAir.AddPoint(80, 68);
            profileDataModifierTable_FreshAir.AddPoint(90, 83);
            profileDataModifierTable_FreshAir.AddPoint(100, 100);

            dynamic fan_Return = system.AddFan();
            fan_Return.name = "Return Air Fan";
            fan_Return.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.ExhaustUnitName);
            fan_Return.DesignFlowRate.Value = 150;
            fan_Return.OverallEfficiency.Value = 1;
            fan_Return.Pressure = 600;
            fan_Return.HeatGainFactor = 1;
            fan_Return.SetElectricalGroup1(electricalGroup_Fans);
            fan_Return.PartLoad.Value = 0;
            fan_Return.PartLoad.ClearModifiers();
            fan_Return.SetSchedule(plantSchedule_Occupancy);
            fan_Return.SetDirection(TPD.tpdDirection.tpdRightLeft);
            fan_Return.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_Return.SetPosition(600, 240);

            dynamic profileDataModifierTable_Return = fan_Return.PartLoad.AddModifierTable();
            profileDataModifierTable_Return.Name = "Fan Part Load Curve";
            profileDataModifierTable_Return.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_Return.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_Return.Clear();
            profileDataModifierTable_Return.AddPoint(0, 0);
            profileDataModifierTable_Return.AddPoint(10, 3);
            profileDataModifierTable_Return.AddPoint(20, 7);
            profileDataModifierTable_Return.AddPoint(30, 13);
            profileDataModifierTable_Return.AddPoint(40, 21);
            profileDataModifierTable_Return.AddPoint(50, 30);
            profileDataModifierTable_Return.AddPoint(60, 41);
            profileDataModifierTable_Return.AddPoint(70, 54);
            profileDataModifierTable_Return.AddPoint(80, 68);
            profileDataModifierTable_Return.AddPoint(90, 83);
            profileDataModifierTable_Return.AddPoint(100, 100);

            dynamic damper = system.AddDamper();
            damper.SetPosition(530, 90);

            dynamic systemZone = system.AddSystemZone();
            systemZone.SetPosition(630, 80);

            dynamic junction_SystemZone_In = system.AddJunction();
            junction_SystemZone_In.Description = "System Zone In";
            junction_SystemZone_In.SetPosition(490, 260);

            dynamic junction_SystemZone_Out = system.AddJunction();
            junction_SystemZone_Out.Description = "System Zone Out";
            junction_SystemZone_Out.SetPosition(690, 260);

            dynamic junction_Return = system.AddJunction();
            junction_Return.Name = "Junction Return";
            junction_Return.Description = "Junction Return";
            junction_Return.SetPosition(240, 200);
            junction_Return.SetDirection(TPD.tpdDirection.tpdBottomTop);

            dynamic heatingCoil = system.AddHeatingCoil();
            heatingCoil.Setpoint.Value = 14;
            heatingCoil.SetHeatingGroup(heatingGroup);
            heatingCoil.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            heatingCoil.Duty.SizeFraction = 1.25; //defult ASHRAE oversizing factors
            heatingCoil.Duty.AddDesignCondition(energyCentre.GetDesignCondition(3));//change from 1 to 3 for annual dsign condition
            heatingCoil.MaximumOffcoil.Value = 60;
            heatingCoil.SetPosition(350, 100);

            dynamic coolingCoil = system.AddCoolingCoil();
            coolingCoil.SetCoolingGroup(coolingGroup);
            coolingCoil.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            coolingCoil.Duty.SizeFraction = 1.15;//defult ASHRAE oversizing factors
            coolingCoil.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
            coolingCoil.BypassFactor.Value = 0.1;
            coolingCoil.MinimumOffcoil.Value = 10;
            coolingCoil.SetPosition(310, 100);

            system.AddDuct(junction_FreshAir, 1, exchanger, 1);
            system.AddDuct(exchanger, 1, coolingCoil, 1);
            system.AddDuct(coolingCoil, 1, heatingCoil, 1);

            TPD.Duct duct_OffCoils = system.AddDuct(heatingCoil, 1, fan_FreashAir, 1);
            TPD.Duct duct_FreshAir = system.AddDuct(fan_FreashAir, 1, junction_SystemZone_In, 1);
            system.AddDuct(junction_SystemZone_In, 1, damper, 1);
            system.AddDuct(damper, 1, systemZone, 1);

            system.AddDuct(systemZone, 1, junction_SystemZone_Out, 1);
            TPD.Duct duct_ZoneOut = system.AddDuct(junction_SystemZone_Out, 1, fan_Return, 1);
            duct_ZoneOut.AddNode(680, 110);
            duct_ZoneOut.AddNode(680, 260);
            duct_ZoneOut = system.AddDuct(fan_Return, 1, junction_Return, 1);
            duct_ZoneOut.AddNode(250, 250);

            system.AddDuct(junction_Return, 1, exchanger, 2);
            system.AddDuct(exchanger, 2, junction_ExhaustAir, 1);

            TPD.Controller controller_HeatingCoilController = system.AddController();
            controller_HeatingCoilController.Name = "Heating Coil Controller";
            controller_HeatingCoilController.Description = "AHU Heating Coil Controller";
            controller_HeatingCoilController.SetPosition(370, 160);
            controller_HeatingCoilController.AddControlArc(heatingCoil).AddNode(360, 170); //connection  in front of controller
            controller_HeatingCoilController.ControlType = TPD.tpdControlType.tpdControlMin;

            TPD.Controller controller_CoolingCoilController = system.AddController();
            controller_CoolingCoilController.Name = "Cooling Coil Controller";
            controller_CoolingCoilController.Description = "AHU Cooling Coil Controller";
            controller_CoolingCoilController.SetPosition(330, 180);
            controller_CoolingCoilController.AddControlArc(coolingCoil).AddNode(320, 190); //connection  in front of controller
            controller_CoolingCoilController.ControlType = TPD.tpdControlType.tpdControlMax;

            TPD.SensorArc sensorArc_HeatingCoil = controller_HeatingCoilController.AddSensorArc(duct_FreshAir);
            sensorArc_HeatingCoil.AddNode(490, 170); //connection after node
            controller_HeatingCoilController.SensorArc1 = sensorArc_HeatingCoil;
            SetAirSideController(controller_HeatingCoilController, AirSideControllerSetup.TempHighZero, 20, 1.5);

            TPD.SensorArc sensorArc_CoolingCoil = controller_CoolingCoilController.AddSensorArc(duct_FreshAir);
            sensorArc_CoolingCoil.AddNode(490, 190);  //connection after node
            controller_CoolingCoilController.SensorArc1 = sensorArc_CoolingCoil;
            SetAirSideController(controller_CoolingCoilController, AirSideControllerSetup.TempLowZero, 13, 1.5);

            TPD.PlantDayType plantDayType = null;
            for (int i = 1; i <= plantRoom.GetEnergyCentre().GetCalendar().GetDayTypeCount(); i++)
            {
                plantDayType = energyCentre.GetCalendar().GetDayType(i);

                // Air Side
                controller_HeatingCoilController.AddDayType(plantDayType);
                controller_CoolingCoilController.AddDayType(plantDayType);
            }

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[2];
            systemComponents[0] = (TPD.SystemComponent)damper;
            systemComponents[1] = (TPD.SystemComponent)systemZone;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int index = 0;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic damper_Group = componentGroup.GetComponent(2 + (index * 2) + 1);
                damper_Group.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                // System Zone
                dynamic systemZone_Group = componentGroup.GetComponent(2 + (index * 2) + 2);
                systemZone_Group.AddZoneLoad(zoneLoad);
                systemZone_Group.SetDHWGroup(dHWGroup);
                systemZone_Group.SetElectricalGroup1(electricalGroup_SmallPower);
                systemZone_Group.SetElectricalGroup2(electricalGroup_Lighting);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                //systemZone_Group.FlowRate.Value = 100;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                systemZone_Group.DisplacementVent = displacementVent ? 1 : 0;

                systemZone_Group.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FreshAir.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                //systemZone_Group.FreshAir.Value = 100;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FreshAir.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                AddComponents(systemZone_Group as TPD.SystemZone, energyCentre, heatingSystem, coolingSystem);

                index++;
            }
        }

        private static void CreateTPD_VAV_Special(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem, bool displacementVent = false)
        {
            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");

            dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");
            dynamic electricalGroup_Humidifiers = plantRoom.ElectricalGroup("Electrical Group - Humidifiers");

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");

            dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            string name = ventilationSystem?.DisplayName();
            if (string.IsNullOrEmpty(name))
            {
                name = displacementVent ? "DISP" : "VAV";
            }

            TPD.System system = plantRoom.AddSystem();
            system.Name = name;
            system.Multiplicity = zoneLoads.Count();

            //dynamic junction_Zero = system.AddJunction();
            //junction_Zero.Description = "0, 0";
            //junction_Zero.SetPosition(0, 0);

            //dynamic junction_Fourty = system.AddJunction();
            //junction_Fourty.Description = "40, 40";
            //junction_Fourty.SetPosition(40, 40);

            dynamic junction_FreshAir = system.AddJunction();
            junction_FreshAir.Name = "Junction Fresh Air";
            junction_FreshAir.Description = "Junction Fresh Air 0,110";
            junction_FreshAir.SetPosition(0, 110);

            dynamic junction_ExhaustAir = system.AddJunction();
            junction_ExhaustAir.Name = "Junction Exhaust Air";
            junction_ExhaustAir.Description = "Junction Exhaust Air";
            junction_ExhaustAir.SetDirection(TPD.tpdDirection.tpdRightLeft);
            junction_ExhaustAir.SetPosition(0, 190);

            dynamic desiccantWheel = system.AddDesiccantWheel();
            //desiccantWheel.ExchLatType = TPD.tpdExchangerLatentType.tpdExchangerLatentEnthalpy;
            //desiccantWheel.LatentEfficiency.Value = 0.0;
            //desiccantWheel.SensibleEfficiency.Value = 0.7;
            //desiccantWheel.Setpoint.Value = 14;
            //desiccantWheel.Flags = TPD.tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;
            desiccantWheel.SetPosition(160, 100);

            dynamic fan_FreashAir = system.AddFan();
            fan_FreashAir.name = "Fresh Air Fan";
            fan_FreashAir.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
            fan_FreashAir.DesignFlowRate.Value = 150;
            fan_FreashAir.OverallEfficiency.Value = 1;
            fan_FreashAir.Pressure = 1000;
            fan_FreashAir.HeatGainFactor = 1;
            fan_FreashAir.SetElectricalGroup1(electricalGroup_Fans);
            fan_FreashAir.PartLoad.Value = 0;
            fan_FreashAir.PartLoad.ClearModifiers();
            fan_FreashAir.SetSchedule(plantSchedule_Occupancy);
            fan_FreashAir.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_FreashAir.SetPosition(390, 100);

            TPD.ProfileDataModifierTable profileDataModifierTable_FreshAir = fan_FreashAir.PartLoad.AddModifierTable();
            profileDataModifierTable_FreshAir.Name = "Fan Part Load Curve";
            profileDataModifierTable_FreshAir.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_FreshAir.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_FreshAir.Clear();
            profileDataModifierTable_FreshAir.AddPoint(0, 0);
            profileDataModifierTable_FreshAir.AddPoint(10, 3);
            profileDataModifierTable_FreshAir.AddPoint(20, 7);
            profileDataModifierTable_FreshAir.AddPoint(30, 13);
            profileDataModifierTable_FreshAir.AddPoint(40, 21);
            profileDataModifierTable_FreshAir.AddPoint(50, 30);
            profileDataModifierTable_FreshAir.AddPoint(60, 41);
            profileDataModifierTable_FreshAir.AddPoint(70, 54);
            profileDataModifierTable_FreshAir.AddPoint(80, 68);
            profileDataModifierTable_FreshAir.AddPoint(90, 83);
            profileDataModifierTable_FreshAir.AddPoint(100, 100);

            dynamic sprayHumidifier = system.AddSprayHumidifier();
            sprayHumidifier.Setpoint.Value = 90;
            sprayHumidifier.SetElectricalGroup1(electricalGroup_Humidifiers);
            sprayHumidifier.SetDirection(TPD.tpdDirection.tpdRightLeft);
            sprayHumidifier.ElectricalLoad.Value = 100;
            sprayHumidifier.WaterFlowCapacity.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            sprayHumidifier.WaterFlowCapacity.AddDesignCondition(energyCentre.GetDesignCondition(3));//change from 1 to 3 for annual dsign condition
            sprayHumidifier.WaterFlowCapacity.SizeFraction = 1.15; //defult ASHRAE oversizing factors
            sprayHumidifier.SetPosition(600, 230);

            dynamic fan_Return = system.AddFan();
            fan_Return.name = "Return Air Fan";
            fan_Return.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.ExhaustUnitName);
            fan_Return.DesignFlowRate.Value = 150;
            fan_Return.OverallEfficiency.Value = 1;
            fan_Return.Pressure = 600;
            fan_Return.HeatGainFactor = 1;
            fan_Return.SetElectricalGroup1(electricalGroup_Fans);
            fan_Return.PartLoad.Value = 0;
            fan_Return.PartLoad.ClearModifiers();
            fan_Return.SetSchedule(plantSchedule_Occupancy);
            fan_Return.SetDirection(TPD.tpdDirection.tpdRightLeft);
            fan_Return.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_Return.SetPosition(80, 190);

            dynamic profileDataModifierTable_Return = fan_Return.PartLoad.AddModifierTable();
            profileDataModifierTable_Return.Name = "Fan Part Load Curve";
            profileDataModifierTable_Return.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_Return.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_Return.Clear();
            profileDataModifierTable_Return.AddPoint(0, 0);
            profileDataModifierTable_Return.AddPoint(10, 3);
            profileDataModifierTable_Return.AddPoint(20, 7);
            profileDataModifierTable_Return.AddPoint(30, 13);
            profileDataModifierTable_Return.AddPoint(40, 21);
            profileDataModifierTable_Return.AddPoint(50, 30);
            profileDataModifierTable_Return.AddPoint(60, 41);
            profileDataModifierTable_Return.AddPoint(70, 54);
            profileDataModifierTable_Return.AddPoint(80, 68);
            profileDataModifierTable_Return.AddPoint(90, 83);
            profileDataModifierTable_Return.AddPoint(100, 100);

            dynamic damper = system.AddDamper();
            damper.SetPosition(530, 90);

            dynamic systemZone = system.AddSystemZone();
            systemZone.SetPosition(630, 80);

            dynamic junction_Return = system.AddJunction();
            junction_Return.Name = "Junction Return";
            junction_Return.Description = "Junction Return";
            junction_Return.SetPosition(240, 200);
            junction_Return.SetDirection(TPD.tpdDirection.tpdBottomTop);

            dynamic heatingCoil = system.AddHeatingCoil();
            heatingCoil.Setpoint.Value = 14;
            heatingCoil.SetHeatingGroup(heatingGroup);
            heatingCoil.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            heatingCoil.Duty.SizeFraction = 1.25; //defult ASHRAE oversizing factors
            heatingCoil.Duty.AddDesignCondition(energyCentre.GetDesignCondition(3));//change from 1 to 3 for annual dsign condition
            heatingCoil.MaximumOffcoil.Value = 60;
            heatingCoil.SetPosition(350, 100);

            dynamic coolingCoil = system.AddCoolingCoil();
            coolingCoil.SetCoolingGroup(coolingGroup);
            coolingCoil.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            coolingCoil.Duty.SizeFraction = 1.15;//defult ASHRAE oversizing factors
            coolingCoil.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
            coolingCoil.BypassFactor.Value = 0.1;
            coolingCoil.MinimumOffcoil.Value = 10;
            coolingCoil.SetPosition(310, 100);

            system.AddDuct(junction_FreshAir, 1, desiccantWheel, 1);
            system.AddDuct(desiccantWheel, 1, coolingCoil, 1);
            system.AddDuct(coolingCoil, 1, heatingCoil, 1);

            TPD.Duct duct_OffCoils = system.AddDuct(heatingCoil, 1, fan_FreashAir, 1);
            TPD.Duct duct_FreshAir = system.AddDuct(fan_FreashAir, 1, damper, 1);
            system.AddDuct(damper, 1, systemZone, 1);

            TPD.Duct duct_ZoneOut = system.AddDuct(systemZone, 1, sprayHumidifier, 1);
            duct_ZoneOut.AddNode(680, 110);
            duct_ZoneOut.AddNode(680, 250);
            duct_ZoneOut = system.AddDuct(sprayHumidifier, 1, junction_Return, 1);
            duct_ZoneOut.AddNode(250, 250);

            system.AddDuct(junction_Return, 1, desiccantWheel, 2);
            system.AddDuct(desiccantWheel, 2, fan_Return, 1);
            system.AddDuct(fan_Return, 1, junction_ExhaustAir, 1);

            TPD.Controller controller_HeatingCoilController = system.AddController();
            controller_HeatingCoilController.Name = "Heating Coil Controller";
            controller_HeatingCoilController.Description = "AHU Heating Coil Controller";
            controller_HeatingCoilController.SetPosition(370, 160);
            controller_HeatingCoilController.AddControlArc(heatingCoil).AddNode(360, 170); //connection  in front of controller
            controller_HeatingCoilController.ControlType = TPD.tpdControlType.tpdControlMin;

            TPD.Controller controller_CoolingCoilController = system.AddController();
            controller_CoolingCoilController.Name = "Cooling Coil Controller";
            controller_CoolingCoilController.Description = "AHU Cooling Coil Controller";
            controller_CoolingCoilController.SetPosition(330, 180);
            controller_CoolingCoilController.AddControlArc(coolingCoil).AddNode(320, 190); //connection  in front of controller
            controller_CoolingCoilController.ControlType = TPD.tpdControlType.tpdControlMax;

            TPD.SensorArc sensorArc_HeatingCoil = controller_HeatingCoilController.AddSensorArc(duct_FreshAir);
            sensorArc_HeatingCoil.AddNode(490, 170); //connection after node
            controller_HeatingCoilController.SensorArc1 = sensorArc_HeatingCoil;
            SetAirSideController(controller_HeatingCoilController, AirSideControllerSetup.TempHighZero, 20, 1.5);

            TPD.SensorArc sensorArc_CoolingCoil = controller_CoolingCoilController.AddSensorArc(duct_FreshAir);
            sensorArc_CoolingCoil.AddNode(490, 190);  //connection after node
            controller_CoolingCoilController.SensorArc1 = sensorArc_CoolingCoil;
            SetAirSideController(controller_CoolingCoilController, AirSideControllerSetup.TempLowZero, 13, 1.5);

            TPD.PlantDayType plantDayType = null;
            for (int i = 1; i <= plantRoom.GetEnergyCentre().GetCalendar().GetDayTypeCount(); i++)
            {
                plantDayType = energyCentre.GetCalendar().GetDayType(i);

                // Air Side
                controller_HeatingCoilController.AddDayType(plantDayType);
                controller_CoolingCoilController.AddDayType(plantDayType);
            }

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[2];
            systemComponents[0] = (TPD.SystemComponent)damper;
            systemComponents[1] = (TPD.SystemComponent)systemZone;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int index = 0;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic damper_Group = componentGroup.GetComponent(2 + (index * 2) + 1);
                damper_Group.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                // System Zone
                dynamic systemZone_Group = componentGroup.GetComponent(2 + (index * 2) + 2);
                systemZone_Group.AddZoneLoad(zoneLoad);
                systemZone_Group.SetDHWGroup(dHWGroup);
                systemZone_Group.SetElectricalGroup1(electricalGroup_SmallPower);
                systemZone_Group.SetElectricalGroup2(electricalGroup_Lighting);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                //systemZone_Group.FlowRate.Value = 100;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                systemZone_Group.DisplacementVent = displacementVent ? 1 : 0;

                systemZone_Group.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FreshAir.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                //systemZone_Group.FreshAir.Value = 100;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FreshAir.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                AddComponents(systemZone_Group as TPD.SystemZone, energyCentre, heatingSystem, coolingSystem);

                index++;
            }
        }

        private static void CreateTPD_MVRE(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");

            dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");

            dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            string name = ventilationSystem?.DisplayName();
            if (string.IsNullOrEmpty(name))
            {
                name = "MVRE";
            }

            TPD.System system = plantRoom.AddSystem();
            system.Name = name;
            system.Multiplicity = zoneLoads.Count();

            dynamic junction_FreshAir = system.AddJunction();
            junction_FreshAir.Name = "Junction Fresh Air";
            junction_FreshAir.Description = "Junction Fresh Air (0,110)";
            junction_FreshAir.SetPosition(0, 110);

            dynamic junction_ExhaustAir = system.AddJunction();
            junction_ExhaustAir.Name = "Junction Exhaust Air";
            junction_ExhaustAir.Description = "Junction Exhaust Air";
            junction_ExhaustAir.SetDirection(TPD.tpdDirection.tpdRightLeft);
            junction_ExhaustAir.SetPosition(0, 190);

            dynamic exchanger = system.AddExchanger();
            exchanger.ExchLatType = TPD.tpdExchangerLatentType.tpdExchangerLatentEnthalpy;
            exchanger.LatentEfficiency.Value = 0.0;
            exchanger.SensibleEfficiency.Value = 0.7;
            exchanger.Setpoint.Value = 14;
            exchanger.Flags = TPD.tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;
            exchanger.SetPosition(160, 100);

            dynamic fan_FreashAir = system.AddFan();
            fan_FreashAir.name = "Fresh Air Fan";
            fan_FreashAir.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
            fan_FreashAir.DesignFlowRate.Value = 150;
            fan_FreashAir.OverallEfficiency.Value = 1;
            fan_FreashAir.Pressure = 1000;
            fan_FreashAir.HeatGainFactor = 1;
            fan_FreashAir.SetElectricalGroup1(electricalGroup_Fans);
            fan_FreashAir.PartLoad.Value = 0;
            fan_FreashAir.PartLoad.ClearModifiers();
            fan_FreashAir.SetSchedule(plantSchedule_Occupancy);
            fan_FreashAir.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_FreashAir.SetPosition(390, 100);

            TPD.ProfileDataModifierTable profileDataModifierTable_FreshAir = fan_FreashAir.PartLoad.AddModifierTable();
            profileDataModifierTable_FreshAir.Name = "Fan Part Load Curve";
            profileDataModifierTable_FreshAir.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_FreshAir.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_FreshAir.Clear();
            profileDataModifierTable_FreshAir.AddPoint(0, 0);
            profileDataModifierTable_FreshAir.AddPoint(10, 3);
            profileDataModifierTable_FreshAir.AddPoint(20, 7);
            profileDataModifierTable_FreshAir.AddPoint(30, 13);
            profileDataModifierTable_FreshAir.AddPoint(40, 21);
            profileDataModifierTable_FreshAir.AddPoint(50, 30);
            profileDataModifierTable_FreshAir.AddPoint(60, 41);
            profileDataModifierTable_FreshAir.AddPoint(70, 54);
            profileDataModifierTable_FreshAir.AddPoint(80, 68);
            profileDataModifierTable_FreshAir.AddPoint(90, 83);
            profileDataModifierTable_FreshAir.AddPoint(100, 100);

            dynamic fan_Return = system.AddFan();
            fan_Return.name = "Return Air Fan";
            fan_Return.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.ExhaustUnitName);
            fan_Return.DesignFlowRate.Value = 150;
            fan_Return.OverallEfficiency.Value = 1;
            fan_Return.Pressure = 600;
            fan_Return.HeatGainFactor = 1;
            fan_Return.SetElectricalGroup1(electricalGroup_Fans);
            fan_Return.PartLoad.Value = 0;
            fan_Return.PartLoad.ClearModifiers();
            fan_Return.SetSchedule(plantSchedule_Occupancy);
            fan_Return.SetDirection(TPD.tpdDirection.tpdRightLeft);
            fan_Return.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_Return.SetPosition(600, 240);

            dynamic profileDataModifierTable_Return = fan_Return.PartLoad.AddModifierTable();
            profileDataModifierTable_Return.Name = "Fan Part Load Curve";
            profileDataModifierTable_Return.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_Return.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_Return.Clear();
            profileDataModifierTable_Return.AddPoint(0, 0);
            profileDataModifierTable_Return.AddPoint(10, 3);
            profileDataModifierTable_Return.AddPoint(20, 7);
            profileDataModifierTable_Return.AddPoint(30, 13);
            profileDataModifierTable_Return.AddPoint(40, 21);
            profileDataModifierTable_Return.AddPoint(50, 30);
            profileDataModifierTable_Return.AddPoint(60, 41);
            profileDataModifierTable_Return.AddPoint(70, 54);
            profileDataModifierTable_Return.AddPoint(80, 68);
            profileDataModifierTable_Return.AddPoint(90, 83);
            profileDataModifierTable_Return.AddPoint(100, 100);

            dynamic damper = system.AddDamper();
            damper.SetPosition(530, 90);

            dynamic systemZone = system.AddSystemZone();
            systemZone.SetPosition(630, 80);

            dynamic junction_Return = system.AddJunction();
            junction_Return.Name = "Junction Return";
            junction_Return.Description = "Junction Return";
            junction_Return.SetPosition(240, 200);
            junction_Return.SetDirection(TPD.tpdDirection.tpdBottomTop);

            system.AddDuct(junction_FreshAir, 1, exchanger, 1);
            system.AddDuct(exchanger, 1, fan_FreashAir, 1);
            system.AddDuct(fan_FreashAir, 1, damper, 1);
            system.AddDuct(damper, 1, systemZone, 1);

            TPD.Duct duct_ZoneOut = system.AddDuct(systemZone, 1, fan_Return, 1);
            duct_ZoneOut.AddNode(680, 110);
            duct_ZoneOut.AddNode(680, 260);
            duct_ZoneOut = system.AddDuct(fan_Return, 1, junction_Return, 1);
            duct_ZoneOut.AddNode(250, 250);

            system.AddDuct(junction_Return, 1, exchanger, 2);
            system.AddDuct(exchanger, 2, junction_ExhaustAir, 1);

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[2];
            systemComponents[0] = (TPD.SystemComponent)damper;
            systemComponents[1] = (TPD.SystemComponent)systemZone;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int index = 0;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic damper_Group = componentGroup.GetComponent(2 + (index * 2) + 1);
                damper_Group.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                // System Zone
                dynamic systemZone_Group = componentGroup.GetComponent(2 + (index * 2) + 2);
                systemZone_Group.AddZoneLoad(zoneLoad);
                systemZone_Group.SetDHWGroup(dHWGroup);
                systemZone_Group.SetElectricalGroup1(electricalGroup_SmallPower);
                systemZone_Group.SetElectricalGroup2(electricalGroup_Lighting);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                //systemZone_Group.FlowRate.Value = 100;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                systemZone_Group.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FreshAir.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                //systemZone_Group.FreshAir.Value = 100;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FreshAir.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                AddComponents(systemZone_Group as TPD.SystemZone, energyCentre, heatingSystem, coolingSystem);

                index++;
            }
        }

        private static void CreateTPD_MV(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads, VentilationSystem ventilationSystem, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");

            dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            string name = ventilationSystem?.DisplayName();
            if (string.IsNullOrEmpty(name))
            {
                name = "MV";
            }

            TPD.System system = plantRoom.AddSystem();
            system.Name = name;
            system.Multiplicity = zoneLoads.Count();

            dynamic junction_FreshAir = system.AddJunction();
            junction_FreshAir.Name = "Junction Fresh Air";
            junction_FreshAir.Description = "Junction Fresh Air (0,110)";
            junction_FreshAir.SetPosition(0, 110);

            dynamic junction_ExhaustAir = system.AddJunction();
            junction_ExhaustAir.Name = "Junction Exhaust Air";
            junction_ExhaustAir.Description = "Junction Exhaust Air";
            junction_ExhaustAir.SetDirection(TPD.tpdDirection.tpdRightLeft);
            junction_ExhaustAir.SetPosition(0, 190);

            dynamic fan_FreashAir = system.AddFan();
            fan_FreashAir.name = "Fresh Air Fan";
            fan_FreashAir.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
            fan_FreashAir.DesignFlowRate.Value = 150;
            fan_FreashAir.OverallEfficiency.Value = 1;
            fan_FreashAir.Pressure = 1000;
            fan_FreashAir.HeatGainFactor = 1;
            fan_FreashAir.SetElectricalGroup1(electricalGroup_Fans);
            fan_FreashAir.PartLoad.Value = 0;
            fan_FreashAir.PartLoad.ClearModifiers();
            fan_FreashAir.SetSchedule(plantSchedule_Occupancy);
            fan_FreashAir.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_FreashAir.SetPosition(390, 100);

            TPD.ProfileDataModifierTable profileDataModifierTable_FreshAir = fan_FreashAir.PartLoad.AddModifierTable();
            profileDataModifierTable_FreshAir.Name = "Fan Part Load Curve";
            profileDataModifierTable_FreshAir.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_FreshAir.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_FreshAir.Clear();
            profileDataModifierTable_FreshAir.AddPoint(0, 0);
            profileDataModifierTable_FreshAir.AddPoint(10, 3);
            profileDataModifierTable_FreshAir.AddPoint(20, 7);
            profileDataModifierTable_FreshAir.AddPoint(30, 13);
            profileDataModifierTable_FreshAir.AddPoint(40, 21);
            profileDataModifierTable_FreshAir.AddPoint(50, 30);
            profileDataModifierTable_FreshAir.AddPoint(60, 41);
            profileDataModifierTable_FreshAir.AddPoint(70, 54);
            profileDataModifierTable_FreshAir.AddPoint(80, 68);
            profileDataModifierTable_FreshAir.AddPoint(90, 83);
            profileDataModifierTable_FreshAir.AddPoint(100, 100);

            dynamic fan_Return = system.AddFan();
            fan_Return.name = "Return Air Fan";
            fan_Return.Description = ventilationSystem?.GetValue<string>(VentilationSystemParameter.ExhaustUnitName);
            fan_Return.DesignFlowRate.Value = 150;
            fan_Return.OverallEfficiency.Value = 1;
            fan_Return.Pressure = 600;
            fan_Return.HeatGainFactor = 1;
            fan_Return.SetElectricalGroup1(electricalGroup_Fans);
            fan_Return.PartLoad.Value = 0;
            fan_Return.PartLoad.ClearModifiers();
            fan_Return.SetSchedule(plantSchedule_Occupancy);
            fan_Return.SetDirection(TPD.tpdDirection.tpdRightLeft);
            fan_Return.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;
            fan_Return.SetPosition(600, 240);

            dynamic profileDataModifierTable_Return = fan_Return.PartLoad.AddModifierTable();
            profileDataModifierTable_Return.Name = "Fan Part Load Curve";
            profileDataModifierTable_Return.SetVariable(1, TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable_Return.Multiplier = TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable_Return.Clear();
            profileDataModifierTable_Return.AddPoint(0, 0);
            profileDataModifierTable_Return.AddPoint(10, 3);
            profileDataModifierTable_Return.AddPoint(20, 7);
            profileDataModifierTable_Return.AddPoint(30, 13);
            profileDataModifierTable_Return.AddPoint(40, 21);
            profileDataModifierTable_Return.AddPoint(50, 30);
            profileDataModifierTable_Return.AddPoint(60, 41);
            profileDataModifierTable_Return.AddPoint(70, 54);
            profileDataModifierTable_Return.AddPoint(80, 68);
            profileDataModifierTable_Return.AddPoint(90, 83);
            profileDataModifierTable_Return.AddPoint(100, 100);

            dynamic damper = system.AddDamper();
            damper.SetPosition(530, 90);

            dynamic systemZone = system.AddSystemZone();
            systemZone.SetPosition(630, 80);

            dynamic junction_Return = system.AddJunction();
            junction_Return.Name = "Junction Return";
            junction_Return.Description = "Junction Return";
            junction_Return.SetPosition(240, 200);
            junction_Return.SetDirection(TPD.tpdDirection.tpdBottomTop);

            system.AddDuct(junction_FreshAir, 1, fan_FreashAir, 1);
            system.AddDuct(fan_FreashAir, 1, damper, 1);
            system.AddDuct(damper, 1, systemZone, 1);

            TPD.Duct duct_ZoneOut = system.AddDuct(systemZone, 1, fan_Return, 1);
            duct_ZoneOut.AddNode(680, 110);
            duct_ZoneOut.AddNode(680, 260);
            duct_ZoneOut = system.AddDuct(fan_Return, 1, junction_Return, 1);
            duct_ZoneOut.AddNode(250, 250);

            system.AddDuct(junction_Return, 1, junction_ExhaustAir, 1);

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[2];
            systemComponents[0] = (TPD.SystemComponent)damper;
            systemComponents[1] = (TPD.SystemComponent)systemZone;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int index = 0;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic damper_Group = componentGroup.GetComponent(2 + (index * 2) + 1);
                damper_Group.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                // System Zone
                dynamic systemZone_Group = componentGroup.GetComponent(2 + (index * 2) + 2);
                systemZone_Group.AddZoneLoad(zoneLoad);
                systemZone_Group.SetDHWGroup(dHWGroup);
                systemZone_Group.SetElectricalGroup1(electricalGroup_SmallPower);
                systemZone_Group.SetElectricalGroup2(electricalGroup_Lighting);
                systemZone_Group.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                //systemZone_Group.FlowRate.Value = 100;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                systemZone_Group.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone_Group.FreshAir.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;
                //systemZone_Group.FreshAir.Value = 100;
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    systemZone_Group.FreshAir.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                AddComponents(systemZone_Group as TPD.SystemZone, energyCentre, heatingSystem, coolingSystem);

                index++;
            }
        }

    }
}