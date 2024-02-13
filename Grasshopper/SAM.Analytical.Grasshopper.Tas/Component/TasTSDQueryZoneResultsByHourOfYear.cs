using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper.Tas.Obsolete
{
    public class TasTSDQueryZoneResultsByHourOfYear : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c2d37631-f69b-4aa9-b0cc-1e7de659e819");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDQueryZoneResultsByHourOfYear()
          : base("Tas.TSDQueryZoneResultsByHourOfYear", "Tas.TSDQueryZoneResultsByHourOfYear",
              "Query Zone Results by HourOfYear.",
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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item, Optional = false }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "_zones_", NickName = "_zones_", Description = "SAM Analytical Zones. In nothing connected all spaces from TSD will be used.\nYou need to connect SAM.Analytcial Zones if you need access to area and volume", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "spaceDataType_", NickName = "spaceDataType_", Description = "Use Enum spaceDataType with variable selection.", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_hourOfYear_", NickName = "_hourOfYear_", Description = "Hour of year indexes", Access = GH_ParamAccess.tree, Optional = true }, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "variables", NickName = "variables", Description = "Tree with values for selected spaceDataType\nUse Tree statistic to get Paths index to help you filter spaces with result", Access = GH_ParamAccess.tree }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "hours", NickName = "hours", Description = "hours", Access = GH_ParamAccess.tree }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "names", NickName = "names", Description = "names", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "zones", NickName = "zones", Description = "SAM Analytical Zones", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

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

            List<Zone> zones = null;
            index = Params.IndexOfInputParam("_zones_");
            if (index != -1)
            {
                zones = new List<Zone>();
                if(!dataAccess.GetDataList(index, zones))
                {
                    zones = null;
                }
            }

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index != -1)
            {
                zones = new List<Zone>();
                if (!dataAccess.GetData(index, ref analyticalModel))
                {
                    analyticalModel = null;
                }
            }

            if(analyticalModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            GH_Structure<GH_Integer> indexes = null;
            index = Params.IndexOfInputParam("_hourOfYear_");
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
                adjacencyCluster = sAMTSDDocument.ToSAM_AdjacencyCluster(new SpaceDataType[] { spaceDataType });
                sAMTSDDocument.Close();
            }

            List<string> names_Result = new List<string>();
            List<Zone> zones_Result = new List<Zone>();

            DataTree<double> dataTree_Values = new DataTree<double>();
            DataTree<int> dataTree_Hours = new DataTree<int>();

            List<Space> spaces_AdjacencyCluster = adjacencyCluster?.GetObjects<Space>();
            if (spaces_AdjacencyCluster != null)
            {
                for (int i = 0; i < zones.Count; i++)
                {
                    Zone zone = zones[i];

                    zones_Result.Add(zone);
                    names_Result.Add(zone.Name);

                    List<Space> spaces = analyticalModel.GetSpaces(zone);
                    if(spaces == null || spaces.Count == 0)
                    {
                        continue;
                    }

                    List<string> names = spaces.ConvertAll(x => x.Name).Distinct().ToList();

                    List<Space> spaces_Zone = spaces_AdjacencyCluster.FindAll(x => names.Contains(x.Name));
                    if (spaces_Zone == null || spaces_Zone.Count == 0)
                    {
                        continue;
                    }

                    Dictionary<int, double> dictionary = new Dictionary<int, double>();
                    foreach(Space space in spaces_Zone)
                    {
                        if(space == null)
                        {
                            continue;
                        }

                        if (!space.TryGetValue(spaceDataType.Text(), out JArray jArray) || jArray == null)
                        {
                            continue;
                        }

                        int index_Temp = i;
                        if (indexes.PathCount <= index_Temp)
                        {
                            index_Temp = indexes.PathCount - 1;
                        }

                        for (int j = 0; j < indexes[index_Temp].Count; j++)
                        {
                            int index_Current = indexes[index_Temp][j].Value;
                            if (index_Current >= jArray.Count)
                            {
                                continue;
                            }

                            double value = (double)jArray[index_Current];
                            if (dictionary.ContainsKey(index_Current))
                            {
                                dictionary[index_Current] += value;
                            }
                            else
                            {
                                dictionary[index_Current] = value;
                            }

                        }
                    }

                    GH_Path path_Temp = new GH_Path(i);
                    foreach (KeyValuePair<int, double> keyValuePair in dictionary)
                    {
                        dataTree_Values.Add(keyValuePair.Value, path_Temp);
                        dataTree_Hours.Add(keyValuePair.Key, path_Temp);
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
                dataAccess.SetDataList(index, names_Result);
            }

            index = Params.IndexOfOutputParam("zones");
            if (index != -1)
            {
                dataAccess.SetDataList(index, zones_Result.ConvertAll(x => new GooGroup(x)));
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }
    }
}