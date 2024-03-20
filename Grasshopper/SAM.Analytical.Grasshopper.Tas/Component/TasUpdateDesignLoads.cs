using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasUpdateDesignLoads : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("b33d5f4a-785d-4acc-86d3-49034badb357");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasUpdateDesignLoads()
          : base("Tas.UpdateDesignLoads", "Tas.UpdateDesignLoads",
              "Updates the design heating and cooling loads for spaces in an analytical model from a TBD zone maxCoolingLoad and maxHeatingLoad.",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            //int aIndex = -1;
            //Param_Boolean booleanParameter = null;

            inputParamManager.AddTextParameter("_pathTasTBD", "_pathTasTBD", "The string path to a TasTBD file.", GH_ParamAccess.item);
            inputParamManager.AddParameter(new GooAnalyticalObjectParam(), "_analyticalModel", "_analyticalModel", "A SAM analytical model", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("_run", "_run", "Connect a boolean toggle to run.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new GooAnalyticalObjectParam(), "analyticalModel", "analyticalModel", "A SAM analytical model", GH_ParamAccess.item);
            outputParamManager.AddParameter(new GooSpaceParam(), "spaces", "spaces", "SAM Analytcial Spaces", GH_ParamAccess.list);
            outputParamManager.AddParameter(new GooResultParam(), "spaceSimulationResultsCooling", "spaceSimulationResultsCooling", "Cooling SpaceSimulationResults", GH_ParamAccess.list);
            outputParamManager.AddParameter(new GooResultParam(), "spaceSimulationResultsHeating", "spaceSimulationResultsHeating", "Heating SpaceSimulationResults", GH_ParamAccess.list);
            outputParamManager.AddBooleanParameter("successful", "successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(4, false);

            bool run = false;
            if (!dataAccess.GetData(2, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            if (!run)
                return;

            string path_TBD = null;
            if (!dataAccess.GetData(0, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            IAnalyticalObject analyticalObject = null;
            if (!dataAccess.GetData(1, ref analyticalObject) || analyticalObject == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<SpaceSimulationResult> spaceSimulationResults_Cooling = null;
            List<SpaceSimulationResult> spaceSimulationResults_Heating = null;
            List<Space> spaces = null;
            bool result = false;

            if (analyticalObject is AnalyticalModel)
            {
                analyticalObject = Analytical.Tas.Modify.UpdateDesignLoads(path_TBD, (AnalyticalModel)analyticalObject);
                AdjacencyCluster adjacencyCluster = ((AnalyticalModel)analyticalObject).AdjacencyCluster;
                if (adjacencyCluster != null)
                {
                    spaces = adjacencyCluster.GetSpaces();
                    if (spaces != null)
                    {
                        spaceSimulationResults_Cooling = new List<SpaceSimulationResult>();
                        spaceSimulationResults_Heating = new List<SpaceSimulationResult>();

                        foreach (Space space in spaces)
                        {
                            List<SpaceSimulationResult> spaceSimulationResults = adjacencyCluster.GetResults<SpaceSimulationResult>(space, Analytical.Tas.Query.Source());
                            if (spaceSimulationResults == null)
                            {
                                continue;
                            }

                            spaceSimulationResults_Cooling.AddRange(spaceSimulationResults.FindAll(x => x.LoadType() == LoadType.Cooling));
                            spaceSimulationResults_Cooling.AddRange(spaceSimulationResults.FindAll(x => x.LoadType() == LoadType.Heating));
                        }
                        result = true;
                    }

                }
            }
            else if(analyticalObject is BuildingModel)
            {
                BuildingModel buildingModel = new BuildingModel((BuildingModel)analyticalObject);
                spaces = Analytical.Tas.Modify.UpdateDesignLoads(buildingModel, path_TBD);
                if(spaces != null)
                {
                    spaceSimulationResults_Cooling = new List<SpaceSimulationResult>();
                    spaceSimulationResults_Heating = new List<SpaceSimulationResult>();

                    foreach (Space space in spaces)
                    {
                        List<SpaceSimulationResult> spaceSimulationResults = buildingModel.GetResults<SpaceSimulationResult>(space, Analytical.Tas.Query.Source());
                        if (spaceSimulationResults == null)
                        {
                            continue;
                        }

                        spaceSimulationResults_Cooling.AddRange(spaceSimulationResults.FindAll(x => x.LoadType() == LoadType.Cooling));
                        spaceSimulationResults_Cooling.AddRange(spaceSimulationResults.FindAll(x => x.LoadType() == LoadType.Heating));
                    }

                    result = true;
                }

                analyticalObject = buildingModel;
            }

            dataAccess.SetData(0, new GooAnalyticalObject(analyticalObject));
            dataAccess.SetDataList(1, spaces?.ConvertAll(x => new GooSpace(x)));
            dataAccess.SetDataList(2, spaceSimulationResults_Cooling?.ConvertAll(x => new GooResult(x)));
            dataAccess.SetDataList(3, spaceSimulationResults_Heating?.ConvertAll(x => new GooResult(x)));
            dataAccess.SetData(4, result);
        }
    }
}