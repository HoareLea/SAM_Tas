using SAM.Core.Tas;
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

                    Dictionary<string, List<TPD.ZoneLoad>> dictionary = new Dictionary<string, List<TPD.ZoneLoad>>();
                    for (int j = 1; j <= tSDData.GetZoneLoadGroupCount(); j++)
                    {
                        TPD.ZoneLoadGroup zoneLoadGroup = tSDData.GetZoneLoadGroup(j);
                        if (zoneLoadGroup == null)
                        {
                            continue;
                        }

                        dictionary[zoneLoadGroup.Name] = new List<TPD.ZoneLoad>();
                        for (int k = 1; k <= zoneLoadGroup.GetZoneLoadCount(); k++)
                        {
                            dictionary[zoneLoadGroup.Name].Add(zoneLoadGroup.GetZoneLoad(k));
                        }
                    }

                    //Heating Groups

                    dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");
                    if(heatingGroup == null)
                    {
                        heatingGroup = plantRoom.AddHeatingGroup();
                        heatingGroup.Name = "Heating Circuit Group";
                        heatingGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                        heatingGroup.SetPosition(offset.X + 200, offset.Y);
                    }

                    //Cooling Groups

                    dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");
                    if (coolingGroup == null)
                    {
                        coolingGroup = plantRoom.AddCoolingGroup();
                        coolingGroup.Name = "Cooling Circuit Group";
                        coolingGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                        coolingGroup.SetPosition(offset.X + 200, offset.Y);
                    }

                    //DHW Groups

                    dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");
                    if (dHWGroup == null)
                    {
                        dHWGroup = plantRoom.AddDHWGroup();
                        dHWGroup.Name = "DHW Circuit Group";
                        dHWGroup.DesignPressureDrop = 17 + (circuitLength / 4);
                        dHWGroup.LoadDistribution = TPD.tpdLoadDistribution.tpdLoadDistributionEven;
                        dHWGroup.SetPosition(offset.X + 200, offset.Y);
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
                    }

                    foreach (KeyValuePair<string, List<TPD.ZoneLoad>> keyValuePair in dictionary)
                    {
                        CreateTPD(energyCentre, keyValuePair.Key, keyValuePair.Value);
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

        private static void CreateTPD(this TPD.EnergyCentre energyCentre, string name, IEnumerable<TPD.ZoneLoad> zoneLoads)
        {
            if(string.IsNullOrWhiteSpace(name) || energyCentre == null || zoneLoads == null || zoneLoads.Count() == 0)
            {
                return;
            }

            switch(name)
            {
                case "UV":
                    CreateTPD_UV(energyCentre, zoneLoads);
                    break;

                case "NV":
                    CreateTPD_NV(energyCentre, zoneLoads);
                    break;

                case "EOL":
                    CreateTPD_EOL(energyCentre, zoneLoads);
                    break;

                case "EOC":
                    CreateTPD_EOC(energyCentre, zoneLoads);
                    break;

                case "CAV":
                    CreateTPD_AHU(energyCentre, zoneLoads);
                    break;
            }
        }

        private static void CreateTPD_UV(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads)
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
                dynamic systemZone;
                systemZone = componentGroup.GetComponent(i);
                systemZone.AddZoneLoad(zoneLoad);
                systemZone.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                //systemZone.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowDeltaT;
                systemZone.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                if (electricalGroup_SmallPower != null)
                {
                    (systemZone as dynamic).SetElectricalGroup1(electricalGroup_SmallPower);
                }

                if (electricalGroup_Lighting != null)
                {
                    (systemZone as dynamic).SetElectricalGroup2(electricalGroup_Lighting);
                }

                i += 3;
            }

        }

        private static void CreateTPD_NV(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads)
        {
            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            Point offset = new Point(0, 0);

            dynamic designConditionLoad_Annual = energyCentre.DesignConditionLoad("Annual Design Condition");

            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic fuelSource_Electrical = energyCentre.FuelSource("Grid Supplied Electricity");
            dynamic fuelSource_Gas = energyCentre.FuelSource("Natural Gas");

            dynamic multiBoiler = plantRoom.AddMultiBoiler();
            multiBoiler.Name = "Heating Circuit Boiler";
            multiBoiler.DesignPressureDrop = 25;
            multiBoiler.DesignDeltaT = 11;
            multiBoiler.Setpoint.Value = 71;
            multiBoiler.SetFuelSource(1, fuelSource_Gas);
            multiBoiler.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            multiBoiler.Duty.SizeFraction = 1.0;
            multiBoiler.Duty.AddDesignCondition(designConditionLoad_Annual);
            multiBoiler.SetPosition(offset.X, offset.Y);

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");

            dynamic pump = plantRoom.AddPump();
            pump.Name = "Heating Circuit Pump";
            pump.DesignFlowRate = 0;
            pump.Capacity = 1;
            pump.OverallEfficiency.Value = 1;
            pump.SetFuelSource(1, fuelSource_Electrical);
            pump.Pressure = (multiBoiler.DesignPressureDrop + heatingGroup.DesignPressureDrop) / 0.712;
            pump.SetPosition(offset.X + 100, offset.Y);

            plantRoom.AddPipe(multiBoiler, 1, pump, 1);
            plantRoom.AddPipe(pump, 1, heatingGroup, 1);
            plantRoom.AddPipe(heatingGroup, 1, multiBoiler, 1);

            TPD.PlantController plantController = plantRoom.AddController();
            plantController.AddControlArc(pump);
            dynamic plantSensorArc = plantController.AddSensorArcToComponent(heatingGroup, 1);

            plantController.SetPosition(offset.X + 180, offset.Y + 110);
            plantController.SensorArc1 = plantSensorArc;
            SetWaterSideController(plantController, WaterSideControllerSetup.Load, 0.1, 0.1);
            offset.X += 300;

            for (int j = 1; j <= plantRoom.GetEnergyCentre().GetCalendar().GetDayTypeCount(); j++)
            {
                TPD.PlantDayType plantDayType = plantRoom.GetEnergyCentre().GetCalendar().GetDayType(j);
                plantController.AddDayType(plantDayType);
            }

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
                dynamic systemZone = componentGroup.GetComponent(i);
                systemZone.AddZoneLoad(zoneLoad);
                systemZone.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                systemZone.FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                if (electricalGroup_SmallPower != null)
                {
                    (systemZone as dynamic).SetElectricalGroup1(electricalGroup_SmallPower);
                }

                if (electricalGroup_Lighting != null)
                {
                    (systemZone as dynamic).SetElectricalGroup2(electricalGroup_Lighting);
                }

                dynamic radiator = systemZone.AddRadiator();
                radiator.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                radiator.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                radiator.Duty.SizeFraction = 1;

                radiator.SetHeatingGroup(heatingGroup);
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    radiator.Duty.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                i++;
            }
        }

        private static void CreateTPD_EOL(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads)
        {
            Point offset = new Point(0, 0);

            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            TPD.System system = plantRoom.AddSystem();
            system.Name = "EOL";
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

            dynamic fan = system.AddFan();
            fan.name = "Fresh Air Fan";
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
                TPD.SystemZone systemZone = componentGroup.GetComponent(i + 2) as TPD.SystemZone;
                (systemZone as dynamic).AddZoneLoad(zoneLoad);
                systemZone.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowACH;
                systemZone.FlowRate.Value = 5;
                //(systemZone as dynamic).SetDHWGroup(tpdDHWGrp);
                if (electricalGroup_SmallPower != null)
                {
                    (systemZone as dynamic).SetElectricalGroup1(electricalGroup_SmallPower);
                }

                if (electricalGroup_Lighting != null)
                {
                    (systemZone as dynamic).SetElectricalGroup2(electricalGroup_Lighting);
                }

                TPD.SizedFlowVariable sizedFlowVariable_FreshAir = systemZone.FreshAir;
                sizedFlowVariable_FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                TPD.Damper damper_Zone = componentGroup.GetComponent(i + 3) as TPD.Damper;
                damper_Zone.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                TPD.Radiator radiator_Zone = systemZone.AddRadiator();
                radiator_Zone.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                radiator_Zone.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                radiator_Zone.Duty.SizeFraction = 1;

                if (heatingGroup != null)
                {
                    (radiator_Zone as dynamic).SetHeatingGroup(heatingGroup);
                }

                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    radiator_Zone.Duty.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                i += 2;
            }
        }

        private static void CreateTPD_EOC(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads)
        {
            Point offset = new Point(0, 0);

            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            TPD.System system = plantRoom.AddSystem();
            system.Name = "EOC";
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

            dynamic fan = system.AddFan();
            fan.name = "Fresh Air Fan";
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

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");

            int i = 1;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                TPD.SystemZone systemZone = componentGroup.GetComponent(i + 2) as TPD.SystemZone;
                (systemZone as dynamic).AddZoneLoad(zoneLoad);
                systemZone.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowACH;
                systemZone.FlowRate.Value = 5;
                //(systemZone as dynamic).SetDHWGroup(tpdDHWGrp);
                if (electricalGroup_SmallPower != null)
                {
                    (systemZone as dynamic).SetElectricalGroup1(electricalGroup_SmallPower);
                }

                if (electricalGroup_Lighting != null)
                {
                    (systemZone as dynamic).SetElectricalGroup2(electricalGroup_Lighting);
                }

                TPD.SizedFlowVariable sizedFlowVariable_FreshAir = systemZone.FreshAir;
                sizedFlowVariable_FreshAir.Type = TPD.tpdSizedVariable.tpdSizedVariableNone;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                TPD.Damper damper_Zone = componentGroup.GetComponent(i + 3) as TPD.Damper;
                damper_Zone.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                TPD.Radiator radiator_Zone = systemZone.AddRadiator();
                radiator_Zone.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                radiator_Zone.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                radiator_Zone.Duty.SizeFraction = 1;

                if (heatingGroup != null)
                {
                    (radiator_Zone as dynamic).SetHeatingGroup(heatingGroup);
                }

                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    radiator_Zone.Duty.AddDesignCondition(energyCentre.GetDesignCondition(j));
                }

                i += 2;
            }

        }

        private static void CreateTPD_AHU(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads)
        {
            Point offset = new Point(0, 0);

            TPD.PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }

            dynamic plantSchedule_Occupancy = energyCentre.PlantSchedule("Occupancy Schedule");
            dynamic plantSchedule_System = energyCentre.PlantSchedule("System Schedule");
            dynamic plantSchedule_Zone = energyCentre.PlantSchedule("Zone Schedule");

            dynamic designConditionLoad_Annual = energyCentre.DesignConditionLoad("Annual Design Condition");

            dynamic fuelSource_Electrical = energyCentre.FuelSource("Grid Supplied Electricity");
            dynamic fuelSource_Gas = energyCentre.FuelSource("Natural Gas");

            dynamic electricalGroup_Fans = plantRoom.ElectricalGroup("Electrical Group - Fans");
            dynamic electricalGroup_Lighting = plantRoom.ElectricalGroup("Electrical Group - Lighting");
            dynamic electricalGroup_SmallPower = plantRoom.ElectricalGroup("Electrical Group - Small Power");

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");

            dynamic multiBoiler_Heating = plantRoom.AddMultiBoiler();
            multiBoiler_Heating.Name = "Heating Circuit Boiler";
            multiBoiler_Heating.DesignPressureDrop = 25;
            multiBoiler_Heating.DesignDeltaT = 11;
            multiBoiler_Heating.Setpoint.Value = 71;
            multiBoiler_Heating.SetFuelSource(1, fuelSource_Gas);
            multiBoiler_Heating.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            multiBoiler_Heating.Duty.SizeFraction = 1.0;
            multiBoiler_Heating.Duty.AddDesignCondition(designConditionLoad_Annual);
            multiBoiler_Heating.SetPosition(offset.X, offset.Y);

            dynamic pump_Heating = plantRoom.AddPump();
            pump_Heating.Name = "Heating Circuit Pump";
            pump_Heating.DesignFlowRate = 0;
            pump_Heating.Capacity = 1;
            pump_Heating.OverallEfficiency.Value = 1;
            pump_Heating.SetFuelSource(1, fuelSource_Electrical);
            pump_Heating.Pressure = (multiBoiler_Heating.DesignPressureDrop + heatingGroup.DesignPressureDrop) / 0.712;
            pump_Heating.SetPosition(offset.X + 100, offset.Y);

            plantRoom.AddPipe(multiBoiler_Heating, 1, pump_Heating, 1);
            plantRoom.AddPipe(pump_Heating, 1, heatingGroup, 1);
            plantRoom.AddPipe(heatingGroup, 1, multiBoiler_Heating, 1);

            TPD.PlantController plantController_Heating = plantRoom.AddController();
            plantController_Heating.AddControlArc(pump_Heating);
            dynamic plantSensorArc_Heating = plantController_Heating.AddSensorArcToComponent(heatingGroup, 1);

            plantController_Heating.SetPosition(offset.X + 180, offset.Y + 110);
            plantController_Heating.SensorArc1 = plantSensorArc_Heating;
            SetWaterSideController(plantController_Heating, WaterSideControllerSetup.Load, 0.1, 0.1);
            offset.X += 300;

            dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");

            dynamic multiChiller = plantRoom.AddMultiChiller();
            multiChiller.Name = "Cooling Circuit Chiller";
            multiChiller.DesignPressureDrop = 25;
            multiChiller.DesignDeltaT = 6;
            multiChiller.Setpoint.Value = 10;
            multiChiller.SetFuelSource(1, fuelSource_Electrical);
            multiChiller.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            multiChiller.Duty.SizeFraction = 1.0;
            multiChiller.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
            multiChiller.SetPosition(offset.X, offset.Y);

            dynamic pump_Cooling = plantRoom.AddPump();
            pump_Cooling.Name = "Cooling Circuit Pump";
            pump_Cooling.DesignFlowRate = 0;
            pump_Cooling.Capacity = 1;
            pump_Cooling.OverallEfficiency.Value = 1;
            pump_Cooling.SetFuelSource(1, fuelSource_Electrical);
            pump_Cooling.Pressure = (multiChiller.DesignPressureDrop + coolingGroup.DesignPressureDrop) / 0.712;
            pump_Cooling.SetPosition(offset.X + 100, offset.Y);

            plantRoom.AddPipe(multiChiller, 1, pump_Cooling, 1);
            plantRoom.AddPipe(pump_Cooling, 1, coolingGroup, 1);
            plantRoom.AddPipe(coolingGroup, 1, multiChiller, 1);

            dynamic plantController_Cooling = plantRoom.AddController();
            plantController_Cooling.AddControlArc(pump_Cooling);
            dynamic plantSensorArc_Cooling = plantController_Cooling.AddSensorArcToComponent(coolingGroup, 1);

            plantController_Cooling.SetPosition(offset.X + 180, offset.Y + 110);
            plantController_Cooling.SensorArc1 = plantSensorArc_Cooling;
            SetWaterSideController(plantController_Cooling, WaterSideControllerSetup.Load, 0.1, 0.1);
            offset.X += 300;

            dynamic dHWGroup = plantRoom.DHWGroup("DHW Circuit Group");

            dynamic multiBoiler_DHW = plantRoom.AddMultiBoiler();
            multiBoiler_DHW.DesignPressureDrop = 25;
            multiBoiler_DHW.Setpoint.Value = 60;
            multiBoiler_DHW.SetFuelSource(1, fuelSource_Gas);
            multiBoiler_DHW.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
            multiBoiler_DHW.Duty.SizeFraction = 1.0;
            multiBoiler_DHW.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
            multiBoiler_DHW.SetPosition(offset.X, offset.Y);

            dynamic pump_DHW = plantRoom.AddPump();
            pump_DHW.Name = "DHW Circuit Pump";
            pump_DHW.Description = "DHW Circuit Pump";
            pump_DHW.DesignFlowRate = 1;
            pump_DHW.Capacity = 1;
            pump_DHW.OverallEfficiency.Value = 1;
            pump_DHW.SetFuelSource(1, fuelSource_Electrical);
            pump_DHW.Pressure = (multiBoiler_DHW.DesignPressureDrop + dHWGroup.DesignPressureDrop) / 0.712;
            pump_DHW.SetPosition(offset.X + 100, offset.Y);

            dynamic junction_DHW_In = plantRoom.AddJunction();
            junction_DHW_In.Name = "DHW Junction In";
            junction_DHW_In.Description = "DHW Junction In";
            junction_DHW_In.SetPosition(offset.X + 100, offset.Y + 70);
            junction_DHW_In.SetDirection(TPD.tpdDirection.tpdRightLeft);

            dynamic junction_DHW_Out = plantRoom.AddJunction();
            junction_DHW_Out.Name = "DHW Junction Out";
            junction_DHW_Out.Description = "DHW Junction Out";
            junction_DHW_Out.SetPosition(offset.X + 140, offset.Y + 70);
            junction_DHW_Out.SetDirection(TPD.tpdDirection.tpdRightLeft);

            plantRoom.AddPipe(junction_DHW_In, 1, multiBoiler_DHW, 1);
            plantRoom.AddPipe(multiBoiler_DHW, 1, pump_DHW, 1);
            plantRoom.AddPipe(pump_DHW, 1, dHWGroup, 1);
            
            TPD.Pipe pipe = plantRoom.AddPipe(dHWGroup, 1, junction_DHW_Out, 1);

            dynamic plantController_Load = plantRoom.AddController();
            plantController_Load.SetPosition(offset.X + 180, offset.Y + 110);
            
            dynamic plantSensorArc_Load = plantController_Load.AddSensorArcToComponent(dHWGroup, 1);
            plantController_Load.SensorArc1 = plantSensorArc_Load;

            SetWaterSideController(plantController_Load, WaterSideControllerSetup.Load, 0.1, 0.1);

            dynamic plantController_Temperature = plantRoom.AddController();
            plantController_Temperature.SetPosition(offset.X + 170, offset.Y + 140);

            SetWaterSideController(plantController_Temperature, WaterSideControllerSetup.TemperatureLowZero, 10, 0);

            dynamic plantSensorArc_Temperature = plantController_Temperature.AddSensorArc(pipe);
            plantController_Temperature.SensorArc1 = plantSensorArc_Temperature;

            dynamic plantController_Max = plantRoom.AddController();
            plantController_Max.SetPosition(offset.X + 140, offset.Y + 110);
            plantController_Max.ControlType = TPD.tpdControlType.tpdControlMin;
            plantController_Max.AddControlArc(pump_DHW);
            plantController_Max.AddChainArc(plantController_Load);
            plantController_Max.AddChainArc(plantController_Temperature);

            TPD.System system = plantRoom.AddSystem();
            system.Name = "AHU";
            system.Multiplicity = zoneLoads.Count();

            dynamic junction_FreshAir = system.AddJunction();
            junction_FreshAir.Name = "Junction Fresh Air";
            junction_FreshAir.Description = "Junction Fresh Air";
            junction_FreshAir.SetPosition(0, 100);

            dynamic junction_ExhaustAir = system.AddJunction();
            junction_ExhaustAir.Name = "Junction Exhaust Air";
            junction_ExhaustAir.Description = "Junction Exhaust Air";
            junction_ExhaustAir.SetDirection(TPD.tpdDirection.tpdRightLeft);
            junction_ExhaustAir.SetPosition(0, 200);

            dynamic exchanger = system.AddExchanger();
            exchanger.ExchLatType = TPD.tpdExchangerLatentType.tpdExchangerLatentEnthalpy;
            exchanger.LatentEfficiency.Value = 0.0;
            exchanger.SensibleEfficiency.Value = 0.7;
            exchanger.Setpoint.Value = 14;
            exchanger.Flags = TPD.tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;
            exchanger.SetPosition(160, 100);

            dynamic fan_FreashAir = system.AddFan();
            fan_FreashAir.name = "Fresh Air Fan";
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
            fan_Return.name = "Return Fan";
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
            for (int i = 1; i <= plantRoom.GetEnergyCentre().GetCalendar().GetDayTypeCount(); i++)
            {
                // Air Side
                plantDayType = energyCentre.GetCalendar().GetDayType(i);
                controller_HeatingGroupCombiner.AddDayType(plantDayType);
                controller_HeatingGroup.AddDayType(plantDayType);
                controller_CoolingGroupCombiner.AddDayType(plantDayType);
                controller_CoolingGroup.AddDayType(plantDayType);
                controller_PassThroughExchanger.AddDayType(plantDayType);
                controller_Optimiser.AddDayType(plantDayType);

                // Water Side
                plantController_Heating.AddDayType(plantDayType);
                plantController_Cooling.AddDayType(plantDayType);
                plantController_Max.AddDayType(plantDayType);
                plantController_Load.AddDayType(plantDayType);
                plantController_Temperature.AddDayType(plantDayType);
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

                dynamic radiatior_Group = systemZone_Group.AddRadiator();
                radiatior_Group.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                radiatior_Group.Duty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                radiatior_Group.Duty.SizeFraction = 1;

                radiatior_Group.SetHeatingGroup(heatingGroup);
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    radiatior_Group.Duty.AddDesignCondition(energyCentre.GetDesignCondition(i));
                }

                index++;
            }

        }
    }
}