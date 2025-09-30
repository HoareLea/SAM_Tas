using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasTSDQueryTM52Results : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("5cfb0ba8-adb9-4d82-a518-85931e2b760a");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.10";

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        /// <summary>
        /// Initializes a new instance of TSDQueryTM52Results
        /// </summary>
        public TasTSDQueryTM52Results()
          : base("Tas.TSDQueryTM52Results", "Tas.TSDQueryTM52Results",
              "Query TSD for TM52Results from Space or Zone" +
               "this node will query results for given space or zone and output when inspect results",
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
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "_spaces_", NickName = "_spaces_", Description = "SAM Analytical Spaces or Zone", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean boolean;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_extended_", NickName = "_extended_", Description = "Return extended results", Access = GH_ParamAccess.item, Optional = true };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String { Name = "_tM52BuildingCategory", NickName = "_tM52BuildingCategory", Description = "Category of Buildings I, II, III or IV", Access = GH_ParamAccess.item, Optional = true };
                @string.SetPersistentData(TM52BuildingCategory.CategoryII.ToString());
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Integer @integer;

                @integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_startHourOfYear_", NickName = "_startHourOfYear_", Description = "Start Hour of Year Index \nDefault start summer 01 May", Access = GH_ParamAccess.item, Optional = true };
                @integer.SetPersistentData(HourOfYear.SummerStartIndex);
                result.Add(new GH_SAMParam(@integer, ParamVisibility.Binding));

                @integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_endHourOfYear_", NickName = "_endHourOfYear_", Description = "End Hour of Year Index \nDefault end summer 30 September", Access = GH_ParamAccess.item, Optional = true };
                @integer.SetPersistentData(HourOfYear.SummerEndIndex);
                result.Add(new GH_SAMParam(@integer, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "spaces", NickName = "spaces", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM52Results", NickName = "tM52Results", Description = "SAM TM52 Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortUpperLimitTemperatures", NickName = "indoorComfortULTemperatures Tupp", Description = "Indoor Comfort Upper Limit Temperatures Tupp \nTcomf = 0.33 Trm + 18.8  where TuppCatII =0.33 Trm + 18.8+3 ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortLowerLimitTemperatures", NickName = "indoorComfortLLTemperatures Tll", Description = "Indoor Comfort Lower Limit Temperatures Tll \nTcomf = 0.33 Trm + 18.8  where TllCatII =0.33 Trm + 18.8-4 ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

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
            List<Zone> zones = null;
            index = Params.IndexOfInputParam("_spaces_");
            if (index != -1)
            {
                List<IAnalyticalObject> analyticalObjects = new List<IAnalyticalObject>();
                if(!dataAccess.GetDataList(index, analyticalObjects))
                {
                    analyticalObjects = null;
                }

                spaces = analyticalObjects?.FindAll(x => x is Space).ConvertAll(x => x as Space);
                if(spaces != null && spaces.Count == 0)
                {
                    spaces = null;
                }

                zones = analyticalObjects?.FindAll(x => x is Zone).ConvertAll(x => x as Zone);
                if (zones != null && zones.Count == 0)
                {
                    zones = null;
                }
            }

            index = Params.IndexOfInputParam("_tM52BuildingCategory");
            string @string = null;
            if (index == -1 || !dataAccess.GetData(index, ref @string) || string.IsNullOrEmpty(@string))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if (!Core.Query.TryGetEnum(@string, out TM52BuildingCategory tM52BuildingCategory) || tM52BuildingCategory == TM52BuildingCategory.Undefined)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_startHourOfYear_");
            int startHourOfYear = HourOfYear.SummerStartIndex;
            if (index != -1)
            {
                if(!dataAccess.GetData(index, ref startHourOfYear))
                {
                    startHourOfYear = HourOfYear.SummerStartIndex;
                }
            }

            index = Params.IndexOfInputParam("_endHourOfYear_");
            int endHourOfYear = HourOfYear.SummerEndIndex;
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref endHourOfYear))
                {
                    endHourOfYear = HourOfYear.SummerEndIndex;
                }
            }

            bool extended = false;
            index = Params.IndexOfInputParam("_extended_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref extended))
                {
                    extended = false;
                }
            }

            TSDConversionSettings tSDConversionSettings = new()
            {
                SpaceDataTypes = new HashSet<SpaceDataType>() { SpaceDataType.ResultantTemperature, SpaceDataType.OccupantSensibleGain },
                SpaceNames = spaces == null ? null : [.. spaces.ConvertAll(x => x?.Name)],
                ZoneNames = zones == null ? null : [.. zones.ConvertAll(x => x?.Name)],
                ConvertWeaterData = true,
            };

            AnalyticalModel analyticalModel = Analytical.Tas.Convert.ToSAM(path, tSDConversionSettings);

            OverheatingCalculator overheatingCalculator = new (analyticalModel)
            {
                TM52BuildingCategory = tM52BuildingCategory,
            };

            List<Space> spaces_Result = null;
            if (spaces == null)
            {
                spaces_Result = analyticalModel.GetSpaces();
            }
            else
            {
                spaces_Result = new List<Space>();
                foreach (Space space in spaces)
                {
                    Space space_Result = analyticalModel.GetSpaces()?.Find(x => x.Name == space.Name);
                    if(space_Result == null)
                    {
                        continue;
                    }

                    spaces_Result.Add(space_Result);
                }
            }

            if (zones != null)
            {
                if(spaces_Result == null)
                {
                    spaces_Result = [];
                }

                foreach (Zone zone in zones)
                {
                    Zone zone_Temp = analyticalModel.GetZones()?.Find(x => x.Name == zone.Name);
                    if (zone_Temp == null)
                    {
                        continue;
                    }

                    List<Space> spaces_Temp = analyticalModel.AdjacencyCluster.GetRelatedObjects<Space>(zone_Temp);
                    if(spaces_Temp == null)
                    {
                        continue;
                    }

                    foreach(Space space_Temp in spaces_Temp)
                    {
                        if(spaces_Result.Find(x => x.Name == space_Temp.Name) != null)
                        {
                            continue;
                        }

                        spaces_Result.Add(space_Temp);
                    }
                }
            }

            List<TM52ExtendedResult> tM52ExtendedResults = overheatingCalculator.Calculate_TM52(spaces_Result, startHourOfYear, endHourOfYear);
            List<IResult> results = extended ? tM52ExtendedResults?.ConvertAll(x => x as IResult) : tM52ExtendedResults?.ConvertAll(x => x?.Simplify() as IResult);

            IndexedDoubles maxIndoorComfortTemperatures = overheatingCalculator.GetMaxIndoorComfortTemperatures(0, 364);
            IndexedDoubles minIndoorComfortTemperatures = overheatingCalculator.GetMinIndoorComfortTemperatures(0, 364);

            index = Params.IndexOfOutputParam("spaces");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces_Result.ConvertAll(x => new GooSpace(x)));
            }

            index = Params.IndexOfOutputParam("tM52Results");
            if (index != -1)
            {
                dataAccess.SetDataList(index, results.ConvertAll(x => new GooResult(x)));
            }

            index = Params.IndexOfOutputParam("indoorComfortUpperLimitTemperatures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, maxIndoorComfortTemperatures?.Values);
            }

            index = Params.IndexOfOutputParam("indoorComfortLowerLimitTemperatures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, minIndoorComfortTemperatures?.Values);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Open TsD", Menu_OpenTSD, Resources.SAM_TasTSD3, true, false);
        }

        private void Menu_OpenTSD(object sender, EventArgs e)
        {
            int index_Path = Params.IndexOfInputParam("_pathTasTSD");
            if (index_Path == -1)
            {
                return;
            }

            string path = null;

            object @object = null;

            @object = Params.Input[index_Path].VolatileData.AllData(true)?.OfType<object>()?.ElementAt(0);
            if (@object is IGH_Goo)
            {
                path = (@object as dynamic).Value?.ToString();
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            Core.Query.StartProcess(path);
        }
    }
}