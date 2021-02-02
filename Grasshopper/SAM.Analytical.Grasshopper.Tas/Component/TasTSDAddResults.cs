using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasTSDAddResults : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("d2e6ed15-d7aa-4282-87fb-1c5081494df1");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.4";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD;


        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDAddResults()
          : base("Tas.TSDAddResults", "Tas.TSDAddResults",
              "Updates AdjacencyCluster from TSD file with results, \n Cooling/Heating load and all variables per Space, Zone and Model\n*click + on inputs to add UnmetHours",
              "SAM", "Tas")
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "_analytical", NickName = "_analytical", Description = "SAM Analytical Object such as AdjacencyCluster or AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_path_Tas_TSD", NickName = "_path_Tas_TSD", Description = "Path to Tas TSD file", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_path_Tas_TBD", NickName = "_path_Tas_TBD", Description = "Path to Tas TBD file", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_unmetHours_", NickName = "_unmetHours_", Description = "Calculate Unmet Hours", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Number number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_unmetHoursMargin_", NickName = "_unmetHoursMargin_", Description = "Unmet Hours Calculation Margin", Access = GH_ParamAccess.item };
                number.SetPersistentData(0.5);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run_", NickName = "_run_", Description = "Run", Access = GH_ParamAccess.item };
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "Analytical", NickName = "Analytical", Description = "SAM Analytical Object such as AdjacencyCluster or AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "SpaceSimulationResults_Cooling", NickName = "SpaceSimulationResults_Cooling", Description = "SAM Analytical SpaceSimulationResults for Cooling", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "ZoneSimulationResults_Cooling", NickName = "ZoneSimulationResults_Cooling", Description = "SAM Analytical ZoneSimulationResults for Cooling", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "AdjacencyClusterSimulationResult_Cooling", NickName = "AdjacencyClusterSimulationResult_Cooling", Description = "SAM Analytical AdjacencyClusterSimulationResult for Cooling", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "SpaceSimulationResults_Heating", NickName = "SpaceSimulationResults_Heating", Description = "SAM Analytical SpaceSimulationResults for Heating", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "ZoneSimulationResults_Heating", NickName = "ZoneSimulationResults_Heating", Description = "SAM Analytical ZoneSimulationResults for Heating", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "AdjacencyClusterSimulationResult_Heating", NickName = "AdjacencyClusterSimulationResult_Heating", Description = "SAM Analytical AdjacencyClusterSimulationResult for Heating", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "PanelSimulationResults", NickName = "PanelSimulationResults", Description = "SAM Analytical PanelSimulationResults", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run_");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
                return;

            index = Params.IndexOfInputParam("_path_Tas_TSD");
            if(index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TSD = null;
            if (!dataAccess.GetData(index, ref path_TSD) || string.IsNullOrWhiteSpace(path_TSD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_path_Tas_TBD");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TBD = null;
            if (!dataAccess.GetData(index, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_analytical");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Core.SAMObject sAMObject = null;
            if (!dataAccess.GetData(index, ref sAMObject) || sAMObject == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AdjacencyCluster adjacencyCluster = null;
            if (sAMObject is AdjacencyCluster)
                adjacencyCluster = new AdjacencyCluster((AdjacencyCluster)sAMObject);
            else if(sAMObject is AnalyticalModel)
                adjacencyCluster = ((AnalyticalModel)sAMObject).AdjacencyCluster;

            List<Core.Result> results = null;
            if(adjacencyCluster != null)
            {
                results = Analytical.Tas.Modify.AddResults(path_TSD, adjacencyCluster);

                if (sAMObject is AdjacencyCluster)
                    sAMObject = adjacencyCluster;
                else if (sAMObject is AnalyticalModel)
                    sAMObject = new AnalyticalModel((AnalyticalModel)sAMObject, adjacencyCluster);
            }

            bool unmetHours = false;
            index = Params.IndexOfInputParam("_runNnmetHours_");
            if (index != -1)
                if (!dataAccess.GetData(index, ref unmetHours))
                    unmetHours = true;

            double unmetHoursMargin = 0.5;
            index = Params.IndexOfInputParam("_unmetHoursMargin_");
            if (index != -1)
                if (!dataAccess.GetData(index, ref unmetHoursMargin))
                    unmetHoursMargin = 0.5;

            if (unmetHours)
            {
                List<Core.Result> results_UnmetHours = Analytical.Tas.Query.UnmetHours(path_TSD, path_TBD, unmetHoursMargin);
                if (results_UnmetHours != null && results_UnmetHours.Count > 0)
                {
                    foreach (Core.Result result in results_UnmetHours)
                    {
                        if (result is AdjacencyClusterSimulationResult)
                            results.Add(result);
                        else if (result is SpaceSimulationResult)
                        {
                            SpaceSimulationResult spaceSimulationResult = (SpaceSimulationResult)result;

                            SpaceSimulationResult spaceSimulationResult_Temp = Analytical.Tas.Query.SpaceSimulationResult(results, spaceSimulationResult);
                            if (spaceSimulationResult_Temp == null)
                                results.Add(spaceSimulationResult);
                            else
                                Core.Modify.Copy(spaceSimulationResult, spaceSimulationResult_Temp, SpaceSimulationResultParameter.UnmetHourFirstIndex, SpaceSimulationResultParameter.UnmetHours, SpaceSimulationResultParameter.OccupiedUnmetHours);
                        }
                    }
                }
            }
            
            index = Params.IndexOfOutputParam("Analytical");
            if (index != -1)
                dataAccess.SetData(index, sAMObject);

            index = Params.IndexOfOutputParam("ZoneSimulationResults_Heating");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is ZoneSimulationResult && ((ZoneSimulationResult)x).LoadType() == LoadType.Heating));

            index = Params.IndexOfOutputParam("SpaceSimulationResults_Heating");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is SpaceSimulationResult && ((SpaceSimulationResult)x).LoadType() == LoadType.Heating));

            index = Params.IndexOfOutputParam("ZoneSimulationResults_Cooling");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is ZoneSimulationResult && ((ZoneSimulationResult)x).LoadType() == LoadType.Cooling));

            index = Params.IndexOfOutputParam("SpaceSimulationResults_Cooling");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is SpaceSimulationResult && ((SpaceSimulationResult)x).LoadType() == LoadType.Cooling));

            index = Params.IndexOfOutputParam("PanelSimulationResults");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is PanelSimulationResult));

            index = Params.IndexOfOutputParam("AdjacencyClusterSimulationResult_Cooling");
            if (index != -1)
                dataAccess.SetData(index, results?.Find(x => x is AdjacencyClusterSimulationResult && Analytical.Query.LoadType(((AdjacencyClusterSimulationResult)x)) == LoadType.Cooling));

            index = Params.IndexOfOutputParam("AdjacencyClusterSimulationResult_Heating");
            if (index != -1)
                dataAccess.SetData(index, results?.Find(x => x is AdjacencyClusterSimulationResult && Analytical.Query.LoadType(((AdjacencyClusterSimulationResult)x)) == LoadType.Heating));
        }
    }
}