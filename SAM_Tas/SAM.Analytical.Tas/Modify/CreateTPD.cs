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

            dynamic designConditionLoad = energyCentre.AddDesignCondition();
            designConditionLoad.Name = "Annual Design Condition";

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
            multiBoiler.Duty.AddDesignCondition(designConditionLoad);
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
    }
}