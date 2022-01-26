using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void CreateTPD(this string path_TPD, string path_TSD, out double totalConsumption)
        {
            totalConsumption = double.NaN;

            if(string.IsNullOrWhiteSpace(path_TPD) || string.IsNullOrWhiteSpace(path_TSD))
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

                    TPD.PlantRoom plantRoom = energyCentre.AddPlantRoom();
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

                    foreach(KeyValuePair<string, List<TPD.ZoneLoad>> keyValuePair in dictionary)
                    {
                        CreateTPD(energyCentre, keyValuePair.Key, keyValuePair.Value);
                    }

                    plantRoom.SimulateEx(1, 8760, 0, tPDDoc.EnergyCentre.ExternalPollutant.Value, 10.0, (int)TPD.tpdSimulationData.tpdSimulationDataLoad + (int)TPD.tpdSimulationData.tpdSimulationDataPipe, 0, 0);

                    totalConsumption = 0;

                    TPD.WrResultSet wrResultSet = (TPD.WrResultSet)tPDDoc.EnergyCentre.GetResultSet(TPD.tpdResultsPeriod.tpdResultsPeriodAnnual, 0, 0, 0, null);
                    int count = wrResultSet.GetVectorSize(TPD.tpdResultVectorType.tpdConsumption);
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
                    wrResultSet.Dispose();

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
            Point offset = new Point(0, 0);
            double circuitLength = 10;

            TPD.PlantRoom plantRoom = energyCentre.GetPlantRoom(0);

            dynamic heatingGroup = plantRoom.AddHeatingGroup();
            heatingGroup.Name = "Heating Circuit Group";
            heatingGroup.DesignPressureDrop = 17 + (circuitLength / 4);
            heatingGroup.SetPosition(offset.X + 200, offset.Y);

            TPD.System system = plantRoom.AddSystem();
            system.Name = "Unventilated";
            system.Multiplicity = zoneLoads.Count();


            dynamic junction_In = system.AddJunction();
            junction_In.SetPosition(100, 110);

            dynamic junction_Out = system.AddJunction();
            junction_Out.SetPosition(100, 190);

            junction_In.SetDirection(TPD.tpdDirection.tpdRightLeft);
            junction_Out.SetDirection(TPD.tpdDirection.tpdRightLeft);


            dynamic zone = system.AddSystemZone();
            zone.SetPosition(630, 80);

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[1];
            systemComponents[0] = (TPD.SystemComponent)zone;
            systemComponents[1] = (TPD.SystemComponent)junction_In;
            systemComponents[2] = (TPD.SystemComponent)junction_Out;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int i = 1;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic systemZone;
                systemZone = componentGroup.GetComponent(i);
                systemZone.AddZoneLoad(zoneLoad);
                systemZone.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowDeltaT;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
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

                i += 3;
            }

        }

        private static void CreateTPD_EOL(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads)
        {

        }

        private static void CreateTPD_EOC(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads)
        {

        }

        private static void CreateTPD_NV(this TPD.EnergyCentre energyCentre, IEnumerable<TPD.ZoneLoad> zoneLoads)
        {
            Point offset = new Point(0, 0);
            double circuitLength = 10;

            dynamic designConditionLoad = energyCentre.AddDesignCondition();
            designConditionLoad.Name = "Annual Design Condition";

            dynamic fuelSource_Electrical = energyCentre.AddFuelSource();
            fuelSource_Electrical.Name = "Grid Supplied Electricity";
            fuelSource_Electrical.Description = "";
            fuelSource_Electrical.CO2Factor = 0.519;
            fuelSource_Electrical.Electrical = 1;
            fuelSource_Electrical.TimeOfUseType = TPD.tpdTimeOfUseType.tpdTimeOfUseValue;
            fuelSource_Electrical.PeakCost = 0.13;

            dynamic fuelSource_Gas = energyCentre.AddFuelSource();
            fuelSource_Gas.Name = "Natural Gas";
            fuelSource_Gas.Description = "";
            fuelSource_Gas.CO2Factor = 0.216;
            fuelSource_Gas.Electrical = 0;
            fuelSource_Gas.TimeOfUseType = TPD.tpdTimeOfUseType.tpdTimeOfUseValue;
            fuelSource_Gas.PeakCost = 0.05;

            TPD.PlantRoom plantRoom = energyCentre.GetPlantRoom(0);

            dynamic heatingGroup = plantRoom.AddHeatingGroup();
            heatingGroup.Name = "Heating Circuit Group";
            heatingGroup.DesignPressureDrop = 17 + (circuitLength / 4);
            heatingGroup.SetPosition(offset.X + 200, offset.Y);

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
            system.Name = "Natural Ventilation";
            system.Multiplicity = zoneLoads.Count();

            dynamic zone = system.AddSystemZone();
            zone.SetPosition(630, 80);

            TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[3];
            systemComponents[0] = (TPD.SystemComponent)zone;

            TPD.Controller[] controllers = new TPD.Controller[0];

            TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
            componentGroup.SetMultiplicity(zoneLoads.Count());

            int i = 1;
            foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
            {
                dynamic systemZone = componentGroup.GetComponent(i);
                systemZone.AddZoneLoad(zoneLoad);
                systemZone.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                systemZone.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowDeltaT;
                for (int j = 1; j <= energyCentre.GetDesignConditionCount(); j++)
                {
                    systemZone.FlowRate.AddDesignCondition(energyCentre.GetDesignCondition(j));
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

                i += 3;
            }
        }
    }
}