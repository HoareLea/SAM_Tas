using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasTSDCreateAdjacencyCluster : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("a219f03b-1990-4b81-9c66-74fccd3ff62a");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDCreateAdjacencyCluster()
          : base("Tas.TSDCreateAdjacencyCluster", "Tas.TSDCreateAdjacencyCluster",
              "Creates AdjacencyCluster from TSD file",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            int index = -1;
            //Param_Boolean booleanParameter = null;

            inputParamManager.AddTextParameter("_path_TasTSD", "pathTasTSD", "string path to TasTSD file", GH_ParamAccess.item);
            
            index = inputParamManager.AddGenericParameter("panelDataType_", "panelDataType_", "SAM Analytical Panel Data Type", GH_ParamAccess.list);
            inputParamManager[index].Optional = true;

            index = inputParamManager.AddGenericParameter("spaceDataType_", "spaceDataType_", "SAM Analytical Space Data Type", GH_ParamAccess.list);
            inputParamManager[index].Optional = true;

            inputParamManager.AddBooleanParameter("run_", "run_", "Connect Bool Toggle to run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new GooAdjacencyClusterParam(),  "AdjacencyCluster", "AdjacencyCluster", "SAM Analytical AdjacencyCluster", GH_ParamAccess.item);
            outputParamManager.AddBooleanParameter("Successful", "Successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(1, false);

            bool run = false;
            if (!dataAccess.GetData(3, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            if (!run)
                return;

            string path_TSD = null;
            if (!dataAccess.GetData(0, ref path_TSD) || string.IsNullOrWhiteSpace(path_TSD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<GH_ObjectWrapper> objectWrappers;

            List<PanelDataType> panelDataTypes = null;

            objectWrappers = new List<GH_ObjectWrapper>();
            if(dataAccess.GetDataList(1, objectWrappers))
            {
                panelDataTypes = new List<PanelDataType>();
                foreach(GH_ObjectWrapper objectWrapper in objectWrappers)
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

            List<SpaceDataType> spaceDataTypes = null;

            objectWrappers = new List<GH_ObjectWrapper>();
            if (dataAccess.GetDataList(2, objectWrappers))
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

            AdjacencyCluster adjacencyCluster = path_TSD.ToSAM_AdjacencyCluster(spaceDataTypes, panelDataTypes);

            dataAccess.SetData(0, adjacencyCluster);
            dataAccess.SetData(1, adjacencyCluster != null);
        }
    }
}