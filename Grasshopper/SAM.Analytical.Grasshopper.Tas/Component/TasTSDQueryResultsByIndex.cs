using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper.Tas.Obsolete
{
    public class TasTSDQueryResultsByIndex : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("5adaee54-3bf5-4e45-85e8-2acd5bcad126");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDQueryResultsByIndex()
          : base("Tas.TSDQueryResultsByIndex", "Tas.TSDQueryResultsByIndex",
              "Query Results by Index.",
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
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "_spaces_", NickName = "_spaces_", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "spaceDataType_", NickName = "spaceDataType_", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_indexes_", NickName = "Indexes", Description = "Hour indexes", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));


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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "variables", NickName = "variables", Description = "Variables", Access = GH_ParamAccess.tree }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "names", NickName = "names", Description = "names", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "hours", NickName = "hours", Description = "hours", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
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

            List<Space> spaces = null;
            index = Params.IndexOfInputParam("_spaces_");
            if (index != -1)
            {
                spaces = new List<Space>();
                if(!dataAccess.GetDataList(index, spaces))
                {
                    spaces = null;
                }
            }

            List<int> indexes = null;
            index = Params.IndexOfInputParam("_indexes_");
            if (index != -1)
            {
                indexes = new List<int>();
                if (!dataAccess.GetDataList(index, indexes))
                {
                    indexes = null;
                }
            }

            if(indexes == null)
            {
                for(int i = 0; i < 8760; i++)
                {
                    indexes.Add(i);
                }
            }

            string path = null;
            if (!dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            SpaceDataType spaceDataType = SpaceDataType.Undefined;
            index = Params.IndexOfInputParam("spaceDataType_");
            if(index != -1)
            {
                string spaceDataTypeName = null;
                if (dataAccess.GetData(index, ref spaceDataTypeName) && !string.IsNullOrWhiteSpace(spaceDataTypeName))
                {
                    if(Core.Query.TryGetEnum(spaceDataTypeName, out SpaceDataType spaceDataType_Temp))
                    {
                        spaceDataType = spaceDataType_Temp;
                    }

                }
            }

            AdjacencyCluster adjacencyCluster = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path, true))
            {
                adjacencyCluster = sAMTSDDocument.ToSAM(new SpaceDataType[] { spaceDataType });
                sAMTSDDocument.Close();
            }

            IEnumerable<string> names = spaces?.FindAll(x => x?.Name != null)?.ConvertAll(x => x.Name)?.Distinct();

            List<string> names_result = new List<string>();

            DataTree<double> dataTree_Values = new DataTree<double>();

            List<Space> spaces_AdjacencyCluster = adjacencyCluster?.GetSpaces();
            if (spaces_AdjacencyCluster != null || spaces_AdjacencyCluster.Count != 0)
            {
                int i = 0;
                foreach(Space space_AdjacencyCluster in spaces_AdjacencyCluster)
                {
                    if (names != null && !names.Contains(space_AdjacencyCluster?.Name))
                    {
                        continue;
                    }

                    if(!space_AdjacencyCluster.TryGetValue(spaceDataType.Text(), out JArray jArray) || jArray == null)
                    {
                        continue;
                    }

                    GH_Path path_Temp = new GH_Path(i);
                    i++;

                    names_result.Add(space_AdjacencyCluster.Name);

                    foreach(int index_Temp in indexes)
                    {
                        if(index_Temp >= jArray.Count)
                        {
                            continue;
                        }

                        dataTree_Values.Add((double)jArray[index_Temp], path_Temp);
                    }

                }
            }

            index = Params.IndexOfOutputParam("variables");
            if(index != -1) 
            {
                dataAccess.SetDataTree(index, dataTree_Values);
            }

            index = Params.IndexOfOutputParam("names");
            if (index != -1)
            {
                dataAccess.SetDataList(index, names_result);
            }

            index = Params.IndexOfOutputParam("hours");
            if (index != -1)
            {
                dataAccess.SetDataList(index, indexes);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }
    }
}