using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasCreateTPD : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1308af3f-69c3-4fa7-b4ad-c4240074ce93");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasCreateTPD()
          : base("Tas.CreateTPD", "Tas.CreateTPD",
              "Tas Create TPD",
              "SAM WIP", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                //result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_path_TSD", NickName = "_path_TSD", Description = "A file path to a Tas file TSD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                //result.Add(new GH_SAMParam(new Weather.Grasshopper.GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                //result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                //result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                //result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "analyticalModel", NickName = "analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "totalConsumption", NickName = "totalConsumption", Description = "Total Consumption [kWh]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly imported?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_successful = Params.IndexOfOutputParam("successful");
            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
                return;

            string path_TSD = null;
            index = Params.IndexOfInputParam("_path_TSD");
            if (index == -1 || !dataAccess.GetData(index, ref path_TSD) || string.IsNullOrWhiteSpace(path_TSD) || !System.IO.File.Exists(path_TSD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }


            string path_TPD = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path_TSD), string.Format("{0}.{1}", System.IO.Path.GetFileNameWithoutExtension(path_TSD), "tpd"));
            if (System.IO.File.Exists(path_TPD))
            {
                System.IO.File.Delete(path_TPD);
            }

            double total = 0;

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {
                TPD.TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                if(tPDDoc != null)
                {
                    TPD.PlantRoom plantRoom = tPDDoc.EnergyCentre.AddPlantRoom();
                    tPDDoc.EnergyCentre.AddTSDData(path_TSD, 0);

                    TPD.TSDData tSDData = tPDDoc.EnergyCentre.GetTSDData(1);

                    List<TPD.ZoneLoad> zoneLoads = new List<TPD.ZoneLoad>();
                    for (int j = 1; j <= tSDData.GetZoneLoadCount(); j++)
                    {
                        TPD.ZoneLoad zoneLoad = tSDData.GetZoneLoad(j);
                        if(zoneLoad == null)
                        {
                            continue;
                        }
                        zoneLoads.Add(zoneLoad);
                    }

                    Point offset = new Point(0, 0);

                    dynamic designConditionLoad = tPDDoc.EnergyCentre.AddDesignCondition();
                    designConditionLoad.Name = "Annual Design Condition";

                    dynamic fuelSource_Electrical = tPDDoc.EnergyCentre.AddFuelSource();
                    fuelSource_Electrical.Name = "Grid Supplied Electricity";
                    fuelSource_Electrical.Description = "";
                    fuelSource_Electrical.CO2Factor = 0.519;
                    fuelSource_Electrical.Electrical = 1;
                    fuelSource_Electrical.TimeOfUseType = TPD.tpdTimeOfUseType.tpdTimeOfUseValue;
                    fuelSource_Electrical.PeakCost = 0.13;

                    dynamic fuelSource_Gas = tPDDoc.EnergyCentre.AddFuelSource();
                    fuelSource_Gas.Name = "Natural Gas";
                    fuelSource_Gas.Description = "";
                    fuelSource_Gas.CO2Factor = 0.216;
                    fuelSource_Gas.Electrical = 0;
                    fuelSource_Gas.TimeOfUseType = TPD.tpdTimeOfUseType.tpdTimeOfUseValue;
                    fuelSource_Gas.PeakCost = 0.05;

                    double circuitLength = 10;

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
                    Analytical.Tas.Modify.SetWaterSideController(plantController, Analytical.Tas.WaterSideControllerSetup.Load, 0.1, 0.1);
                    offset.X += 300;

                    for (int j = 1; j <= plantRoom.GetEnergyCentre().GetCalendar().GetDayTypeCount(); j++)
                    {
                        TPD.PlantDayType plantDayType = plantRoom.GetEnergyCentre().GetCalendar().GetDayType(j);
                        plantController.AddDayType(plantDayType);
                    }

                    TPD.System system = plantRoom.AddSystem();
                    system.Name = "Radiant Heating";
                    system.Multiplicity = zoneLoads.Count;

                    dynamic zone = system.AddSystemZone();
                    zone.SetPosition(630, 80);

                    TPD.SystemComponent[] systemComponents = new TPD.SystemComponent[1];
                    systemComponents[0] = (TPD.SystemComponent)zone;

                    TPD.Controller[] controllers = new TPD.Controller[0];

                    TPD.ComponentGroup componentGroup = system.AddGroup(systemComponents, controllers);
                    componentGroup.SetMultiplicity(zoneLoads.Count);

                    int i = 0;
                    foreach (TPD.ZoneLoad zoneLoad in zoneLoads)
                    {
                        dynamic systemZone;
                        systemZone = componentGroup.GetComponent(i + 1);
                        systemZone.AddZoneLoad(zoneLoad);
                        systemZone.FlowRate.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                        systemZone.FlowRate.Method = TPD.tpdSizeFlowMethod.tpdSizeFlowDeltaT;
                        for (int j = 1; j <= tPDDoc.EnergyCentre.GetDesignConditionCount(); j++)
                        {
                            systemZone.FlowRate.AddDesignCondition(tPDDoc.EnergyCentre.GetDesignCondition(j));
                        }

                        dynamic radiator = systemZone.AddRadiator();
                        radiator.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                        radiator.Duty.AddDesignCondition(tPDDoc.EnergyCentre.GetDesignCondition(2));
                        radiator.Duty.SizeFraction = 1;

                        radiator.SetHeatingGroup(heatingGroup);
                        for (int j = 1; j <= tPDDoc.EnergyCentre.GetDesignConditionCount(); j++)
                        {
                            radiator.Duty.AddDesignCondition(tPDDoc.EnergyCentre.GetDesignCondition(j));
                        }

                        i++;
                    }

                    plantRoom.SimulateEx(1, 8760, 0, tPDDoc.EnergyCentre.ExternalPollutant.Value, 10.0, (int)TPD.tpdSimulationData.tpdSimulationDataLoad + (int)TPD.tpdSimulationData.tpdSimulationDataPipe, 0, 0);

                    TPD.WrResultSet wrResultSet = (TPD.WrResultSet)tPDDoc.EnergyCentre.GetResultSet(TPD.tpdResultsPeriod.tpdResultsPeriodAnnual, 0, 0, 0, null);
                    int count = wrResultSet.GetVectorSize(TPD.tpdResultVectorType.tpdConsumption);
                    for (int j = 1; j <= count; j++)
                    {
                        TPD.WrResultItem wrResultItem = (TPD.WrResultItem)wrResultSet.GetResultItem(TPD.tpdResultVectorType.tpdConsumption, j);
                        if(wrResultItem != null)
                        {
                            Array array = (Array)wrResultItem.GetValues();
                            if(array != null && array.Length != 0)
                            {
                                total += (double)array.GetValue(0);
                            }
                        }

                    }
                    wrResultSet.Dispose();

                    tPDDoc.Save();
                }
            }

            index = Params.IndexOfOutputParam("totalConsumption");
            if (index != -1)
            {
                dataAccess.SetData(index, total);
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, true);
            }
        }
    }
}