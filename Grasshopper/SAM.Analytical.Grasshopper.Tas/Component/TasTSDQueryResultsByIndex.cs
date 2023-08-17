﻿using Grasshopper;
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
        public override string LatestComponentVersion => "1.0.2";

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "spaceDataType_", NickName = "spaceDataType_", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "_spaces_", NickName = "_spaces_", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));
                
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_indexes_", NickName = "_indexes_", Description = "Hour indexes", Access = GH_ParamAccess.tree, Optional = true }, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "hours", NickName = "hours", Description = "hours", Access = GH_ParamAccess.tree }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "names", NickName = "names", Description = "names", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "spaces", NickName = "spaces", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

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

            GH_Structure<GH_Integer> indexes = null;
            index = Params.IndexOfInputParam("_indexes_");
            if (index != -1)
            {
                if (!dataAccess.GetDataTree(index, out indexes))
                {
                    indexes = null;
                }
            }

            if(indexes == null)
            {
                indexes = new GH_Structure<GH_Integer>();
                GH_Path path_Temp = new GH_Path(0);
                for (int i = 0; i < 8760; i++)
                {
                    indexes.Append(new GH_Integer(i), path_Temp);
                }
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

            List<string> names_result = new List<string>();

            DataTree<double> dataTree_Values = new DataTree<double>();
            DataTree<int> dataTree_Hours = new DataTree<int>();

            List<Space> spaces_Result = new List<Space>();

            List<Space> spaces_AdjacencyCluster = adjacencyCluster?.GetSpaces();
            if (spaces_AdjacencyCluster != null)
            {
                if (spaces == null)
                {
                    spaces = spaces_AdjacencyCluster;
                }

                if (spaces != null)
                {
                    for (int i = 0; i < spaces.Count; i++)
                    {
                        if (spaces[i] == null)
                        {
                            continue;
                        }

                        Space space_AdjacencyCluster = spaces_AdjacencyCluster.Find(x => x.Guid == spaces[i].Guid);
                        if (space_AdjacencyCluster == null)
                        {
                            continue;
                        }

                        if (!space_AdjacencyCluster.TryGetValue(spaceDataType.Text(), out JArray jArray) || jArray == null)
                        {
                            continue;
                        }

                        int index_Temp = i;
                        if (indexes.PathCount <= index_Temp)
                        {
                            index_Temp = indexes.PathCount - 1;
                        }

                        GH_Path path_Temp = new GH_Path(i);
                        for (int j = 0; j < indexes[index_Temp].Count; j++)
                        {
                            int index_Current = indexes[index_Temp][j].Value;
                            if (index_Current >= jArray.Count)
                            {
                                continue;
                            }

                            dataTree_Values.Add((double)jArray[index_Current], path_Temp);
                            dataTree_Hours.Add(index_Current, path_Temp);
                        }
                    }
                }
            }

            index = Params.IndexOfOutputParam("variables");
            if(index != -1) 
            {
                dataAccess.SetDataTree(index, dataTree_Values);
            }

            index = Params.IndexOfOutputParam("hours");
            if (index != -1)
            {
                dataAccess.SetDataTree(index, dataTree_Hours);
            }

            index = Params.IndexOfOutputParam("names");
            if (index != -1)
            {
                dataAccess.SetDataList(index, names_result);
            }

            index = Params.IndexOfOutputParam("spaces");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces_Result.ConvertAll(x => new GooSpace(x)));
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }
    }
}