using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.Obsolete
{
    [Obsolete("Obsolete since 2021-01-27")]
    public class TasTSDCreateSpaceSimulationResults : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c38e9d64-61f9-4279-854d-92175e5376cb");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDCreateSpaceSimulationResults()
          : base("Tas.TSDCreateSpaceSimulationResults", "Tas.TSDCreateSpaceSimulationResults",
              "Creates space simulation results from a TasTSD file.",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTSD", NickName = "_pathTasTSD", Description = "A file path to a TasTSD file.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "panelDataTypes_", NickName = "panelDataTypes_", Description = "Filters your chosen results for the type: panel", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "spaceDataTypes_", NickName = "Filters your chosen results for the type: space", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Boolean boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "results", NickName = "results", Description = "The SAM analytical space simulation results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAdjacencyClusterParam() { Name = "adjacencyCluster", NickName = "adjacencyCluster", Description = "A SAM analytical adjacency cluster", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly extracted?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_Successful;
            index_Successful = Params.IndexOfOutputParam("successful");
            if(index_Successful != -1)
                dataAccess.SetData(index_Successful, false);

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index != -1)
                if (!dataAccess.GetData(index, ref run))
                    run = false;

            if (!run)
                return;

            index = Params.IndexOfInputParam("_pathTasTSD");
            if(index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path = null;
            if (!dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }


            List<PanelDataType> panelDataTypes = null; 
            index = Params.IndexOfInputParam("panelDataTypes_");
            if(index != -1)
            {
                List<GH_ObjectWrapper> objectWrappers = new List<GH_ObjectWrapper>();
                if (dataAccess.GetDataList(index, objectWrappers))
                {
                    panelDataTypes = new List<PanelDataType>();
                    foreach (GH_ObjectWrapper objectWrapper in objectWrappers)
                    {
                        PanelDataType panelDataType = PanelDataType.Undefined;
                        if (objectWrapper.Value is GH_String)
                            panelDataType = Analytical.Tas.Query.PanelDataType(((GH_String)objectWrapper.Value).Value);
                        else
                            panelDataType = Analytical.Tas.Query.PanelDataType(objectWrapper.Value);

                        if (panelDataType != PanelDataType.Undefined)
                            panelDataTypes.Add(panelDataType);
                    }
                }
            }

            List<SpaceDataType> spaceDataTypes = null;
            index = Params.IndexOfInputParam("spaceDataTypes_");
            if (index != -1)
            {
                List<GH_ObjectWrapper> objectWrappers = new List<GH_ObjectWrapper>();
                if (dataAccess.GetDataList(index, objectWrappers))
                {
                    spaceDataTypes = new List<SpaceDataType>();
                    foreach (GH_ObjectWrapper objectWrapper in objectWrappers)
                    {
                        SpaceDataType spaceDataType = SpaceDataType.Undefined;
                        if (objectWrapper.Value is GH_String)
                            spaceDataType = Analytical.Tas.Query.SpaceDataType(((GH_String)objectWrapper.Value).Value);
                        else
                            spaceDataType = Analytical.Tas.Query.SpaceDataType(objectWrapper.Value);

                        if (spaceDataType != SpaceDataType.Undefined)
                            spaceDataTypes.Add(spaceDataType);
                    }
                }
            }

            AdjacencyCluster adjacencyCluster = null;
            List<Core.Result> results = new List<Core.Result>();

            int index_Result = Params.IndexOfOutputParam("results");
            int index_AdjacencyCluster = Params.IndexOfOutputParam("adjacencyCluster");
            if (index_Result != -1 || index_AdjacencyCluster != -1)
            {
                using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path, true))
                {
                    if (index_AdjacencyCluster != -1)
                    {
                        adjacencyCluster = sAMTSDDocument.ToSAM_AdjacencyCluster(spaceDataTypes, panelDataTypes);
                        dataAccess.SetData(index_AdjacencyCluster, new GooAdjacencyCluster(adjacencyCluster));
                    }
                        

                    if (index_Result != -1)
                    {
                        results = Analytical.Tas.Convert.ToSAM_Results(sAMTSDDocument);
                        dataAccess.SetDataList(index_Result, results?.ConvertAll(x => new GooResult(x)));
                    }

                    sAMTSDDocument.Close();
                }
            }

            if (index_Successful != -1)
                dataAccess.SetData(index_Successful, true);
        }
    }
}