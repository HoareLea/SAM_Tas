﻿using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Newtonsoft.Json.Linq;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.Obsolete
{
    public class TasTSDQueryResultsByTotalPercentage : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1e73032e-5f4f-4591-ac9c-2507f4392b94");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.3";

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDQueryResultsByTotalPercentage()
          : base("Tas.TSDQueryResultsByTotalPercentage", "Tas.TSDQueryResultsByTotalPercentage",
              "Query Results by Total Percentage.\nIt will search all spaces and return ONE and hour index for this space only",
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
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "_spaces_", NickName = "_spaces_", Description = "SAM Analytical Spaces. In nothing connected all spaces from TSD will be used.\nYou need to connect SAM.Analytcial Spaces if you need access to area and volume", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "spaceDataType_", NickName = "spaceDataType_", Description = "Use Enum spaceDataType with variable selection.", Access = GH_ParamAccess.item, Optional = true };
                @string.SetPersistentData(SpaceDataType.CoolingLoad.Text());
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number number = null;

                number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_percentage_", NickName = "percentage", Description = "Percentage [0 - 100%] \n ie Cooling load 100% will return highest value of cooling load", Access = GH_ParamAccess.item, Optional = true };
                number.SetPersistentData(97);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                @string = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "numberComparisonType_", NickName = "numberComparisonType_", Description = "Number Comparison Type Enum", Access = GH_ParamAccess.item, Optional = true};
                @string.SetPersistentData(NumberComparisonType.GreaterOrEquals.Text());
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean boolean = null;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_average_", NickName = "_average_", Description = "Average Method \n*Default =True - takes max/min value and multiply by percentage to find required value. \n  If False we treat each value as point, we sort whole list find percentage from list length get value and then retrieve all depends on numberComparisonType", Optional = true, Access = GH_ParamAccess.item };
                boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "minValue_", NickName = "minValue_", Description = "Minimal Value\n*ie In case we search for max Solar and have rooms no windows results will be zero as max and min. This input allow you to protect and seting up 0.1 will not return zeros. ", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "value", NickName = "value", Description = "value", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "names", NickName = "names", Description = "Names of connected Spaces \n*If Spaces not conneted as input all spaces in TSD will be presented ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "spaces", NickName = "spaces", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "variables_In", NickName = "variables_In", Description = "In Variables Tree with values for selected spaceDataType\nUse Tree statistic to get Paths index to help you filter spaces with result", Access = GH_ParamAccess.tree }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "hours_In", NickName = "hours_In", Description = "hours_In", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "variables_Out", NickName = "variables_Out", Description = "Out Variables", Access = GH_ParamAccess.tree }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "hours_Out", NickName = "hours_Out", Description = "hours_Out", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));

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

            double percentage = 90;
            index = Params.IndexOfInputParam("_percentage_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref percentage))
                {
                    percentage = 90;
                }
            }

            double minValue = double.NaN;
            index = Params.IndexOfInputParam("minValue_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref minValue))
                {
                    minValue = double.NaN;
                }
            }

            bool average = true;
            index = Params.IndexOfInputParam("_average_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref average))
                {
                    average = true;
                }
            }

            NumberComparisonType numberComparisonType = NumberComparisonType.GreaterOrEquals;
            index = Params.IndexOfInputParam("numberComparisonType_");
            if (index != -1)
            {
                string numberComparisonTypeName = null;
                if (dataAccess.GetData(index, ref numberComparisonTypeName) && !string.IsNullOrWhiteSpace(numberComparisonTypeName))
                {
                    if (Core.Query.TryGetEnum(numberComparisonTypeName, out NumberComparisonType numberComparisonType_Temp))
                    {
                        numberComparisonType = numberComparisonType_Temp;
                    }

                }
            }

            AdjacencyCluster adjacencyCluster = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path, true))
            {
                adjacencyCluster = sAMTSDDocument.ToSAM(new SpaceDataType[] { spaceDataType });
                sAMTSDDocument.Close();
            }

            List<Space> spaces_Result = new List<Space>();
            List<string> names_Result = new List<string>();
            double value_Result = double.NaN;
            
            DataTree<double> dataTree_Values_In = new DataTree<double>();
            DataTree<int> dataTree_Indexes_In = new DataTree<int>();

            DataTree<double> dataTree_Values_Out = new DataTree<double>();
            DataTree<int> dataTree_Indexes_Out = new DataTree<int>();

            List<Space> spaces_AdjacencyCluster = adjacencyCluster?.GetSpaces();
            if (spaces_AdjacencyCluster != null || spaces_AdjacencyCluster.Count != 0)
            {
                double min = double.MaxValue;
                double max = double.MinValue;
                List<double> values = new List<double>();
                foreach (Space space_AdjacencyCluster in spaces_AdjacencyCluster)
                {
                    Space space = null;
                    if(spaces != null)
                    {
                        space = spaces.Find(x => x.Name == space_AdjacencyCluster.Name);
                        if(space == null)
                        {
                            continue;
                        }
                    }
                    
                    if(space == null)
                    {
                        space = space_AdjacencyCluster;
                    }

                    if (!space_AdjacencyCluster.TryGetValue(spaceDataType.Text(), out JArray jArray) || jArray == null)
                    {
                        continue;
                    }

                    foreach (double value_Temp in jArray)
                    {
                        if(!double.IsNaN(minValue) && value_Temp <= minValue)
                        {
                            continue;
                        }
                        
                        values.Add(value_Temp);
                        if (value_Temp > max)
                        {
                            max = value_Temp;
                        }

                        if(value_Temp < min)
                        {
                            min = value_Temp;
                        }
                    }
                }

                if(average)
                {
                    value_Result = min + ((max - min) * percentage / 100);
                }
                else
                {
                    int index_Temp = System.Convert.ToInt32(System.Convert.ToDouble(values.Count) * (percentage / 100));
                    if(index_Temp >= values.Count)
                    {
                        index_Temp = values.Count - 1;
                    }

                    values.Sort();
                    value_Result = values[index_Temp];
                }


                int i = 0; 
                foreach (Space space_AdjacencyCluster in spaces_AdjacencyCluster)
                {
                    Space space = null;
                    if (spaces != null)
                    {
                        space = spaces.Find(x => x.Name == space_AdjacencyCluster.Name);
                        if (space == null)
                        {
                            continue;
                        }
                    }

                    if (space == null)
                    {
                        space = space_AdjacencyCluster;
                    }

                    if (!space_AdjacencyCluster.TryGetValue(spaceDataType.Text(), out JArray jArray) || jArray == null)
                    {
                        continue;
                    }

                    spaces_Result.Add(space);
                    names_Result.Add(space.Name);

                    GH_Path path_Temp = new GH_Path(i);
                    i++;

                    values = jArray.ToList<double>();
                    for (int j = 0; j < values.Count; j++)
                    {
                        if (Core.Query.Compare(values[j], value_Result, numberComparisonType))
                        {
                            dataTree_Values_In.Add(values[j], path_Temp);
                            dataTree_Indexes_In.Add(j, path_Temp);
                        }
                        else
                        {
                            dataTree_Values_Out.Add(values[j], path_Temp);
                            dataTree_Indexes_Out.Add(j, path_Temp);
                        }
                    }
                }
            }

            index = Params.IndexOfOutputParam("value");
            if (index != -1)
            {
                dataAccess.SetData(index, value_Result);
            }

            index = Params.IndexOfOutputParam("names");
            if (index != -1)
            {
                dataAccess.SetDataList(index, names_Result);
            }

            index = Params.IndexOfOutputParam("spaces");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces_Result.ConvertAll(x => new GooSpace(x)));
            }

            index = Params.IndexOfOutputParam("variables_In");
            if(index != -1) 
            {
                dataAccess.SetDataTree(index, dataTree_Values_In);
            }

            index = Params.IndexOfOutputParam("hours_In");
            if (index != -1)
            {
                dataAccess.SetDataTree(index, dataTree_Indexes_In);
            }

            index = Params.IndexOfOutputParam("variables_Out");
            if (index != -1)
            {
                dataAccess.SetDataTree(index, dataTree_Values_Out);
            }

            index = Params.IndexOfOutputParam("hours_Out");
            if (index != -1)
            {
                dataAccess.SetDataTree(index, dataTree_Indexes_Out);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }
    }
}