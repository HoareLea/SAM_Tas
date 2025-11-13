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
    public class TasTSDQueryTM59Results : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("d9f84a00-275e-401c-9c69-ad30b4ccb403");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.7";

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        /// <summary>
        /// Initializes a new instance of the TasTSDQueryTM59Results class.
        /// </summary>
        public TasTSDQueryTM59Results()
          : base("Tas.TSDQueryTM59Results", "Tas.TSDQueryTM59Results",
              "Query TSD for TM59Results" +
               "this node will query results for summer 01 May to 30 September for a given space or zone and output when inspect results",
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
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
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

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

                return [.. result];
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "spaces", NickName = "spaces", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59MechanicalVentilationResults", NickName = "tM59MechanicalVentilationResults", Description = "SAM TM59 Mechanical Ventilation Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59NaturalVentilationResults", NickName = "tM59NaturalVentilationResults", Description = "SAM TM59 Natural Ventilation Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59CorridorResults", NickName = "tM59CorridorResults", Description = "SAM TM59 Corridor Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortUpperLimitTemperatures", NickName = "indoorComfortULTemperatures Tupp", Description = "Indoor Comfort Upper Limit Temperatures Tupp \nTcomf = 0.33 Trm + 18.8  where TuppCatII =0.33 Trm + 18.8+3 ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortLowerLimitTemperatures", NickName = "indoorComfortLLTemperatures Tll", Description = "Indoor Comfort Lower Limit Temperatures Tll \nTcomf = 0.33 Trm + 18.8  where TuppCatII =0.33 Trm + 18.8-4 ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly extracted?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                return [.. result];
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
            {
                dataAccess.SetData(index_Successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref run))
                {
                    run = false;
                }
            }

            if (!run)
            {
                return;
            }

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
                List<IAnalyticalObject> analyticalObjects = [];
                if (!dataAccess.GetDataList(index, analyticalObjects))
                {
                    analyticalObjects = null;
                }

                spaces = analyticalObjects?.FindAll(x => x is Space).ConvertAll(x => x as Space);
                if (spaces != null && spaces.Count == 0)
                {
                    spaces = null;
                }

                zones = analyticalObjects?.FindAll(x => x is Zone).ConvertAll(x => x as Zone);
                if (zones != null && zones.Count == 0)
                {
                    zones = null;
                }
            }

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
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

            bool extended = false;
            index = Params.IndexOfInputParam("_extended_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref extended))
                {
                    extended = false;
                }
            }

            TSDConversionSettings tSDConversionSettings = new ()
            {
                SpaceDataTypes = new HashSet<SpaceDataType>() { SpaceDataType.ResultantTemperature, SpaceDataType.OccupantSensibleGain },
                SpaceNames = spaces == null ? null : [.. spaces.ConvertAll(x => x?.Name)],
                ZoneNames = zones == null ? null : [.. zones.ConvertAll(x => x?.Name)],
                ConvertWeaterData = true,
                ConvertZones = true
            };

            AnalyticalModel analyticalModel_TSD = Analytical.Tas.Convert.ToSAM(path, tSDConversionSettings);
            AdjacencyCluster adjacencyCluster_TSD = analyticalModel_TSD?.AdjacencyCluster;
            if(adjacencyCluster_TSD != null)
            {
                List<Space> spaces_AnalyticalModel = analyticalModel?.GetSpaces();
                if(spaces_AnalyticalModel != null)
                {
                    List<Space> spaces_TSD = adjacencyCluster_TSD.GetSpaces();
                    if(spaces_TSD != null)
                    {
                        foreach(Space space_TSD in spaces_TSD)
                        {
                            Space space_AnalyticalModel = spaces_AnalyticalModel.Find(x => x.Name == space_TSD.Name);
                            if(space_AnalyticalModel != null)
                            {
                                space_TSD.InternalCondition = space_AnalyticalModel.InternalCondition;
                                adjacencyCluster_TSD.AddObject(space_TSD);
                            }
                        }

                        analyticalModel_TSD = new AnalyticalModel(analyticalModel_TSD, adjacencyCluster_TSD);
                    }
                }
            }

            OverheatingCalculator overheatingCalculator = new (analyticalModel_TSD)
            {
                TM52BuildingCategory = tM52BuildingCategory,
            };

            List<Space> spaces_Result = null;
            if (spaces == null)
            {
                spaces_Result = analyticalModel_TSD.GetSpaces();
            }
            else
            {
                spaces_Result = [];
                foreach (Space space in spaces)
                {
                    Space space_Result = analyticalModel_TSD.GetSpaces()?.Find(x => x.Name == space.Name);
                    if (space_Result == null)
                    {
                        continue;
                    }

                    spaces_Result.Add(space_Result);
                }
            }

            if (zones != null)
            {
                if (spaces_Result == null)
                {
                    spaces_Result = [];
                }

                foreach (Zone zone in zones)
                {
                    Zone zone_Temp = analyticalModel_TSD.GetZones()?.Find(x => x.Name == zone.Name);
                    if (zone_Temp == null)
                    {
                        continue;
                    }

                    List<Space> spaces_Temp = analyticalModel_TSD.AdjacencyCluster.GetRelatedObjects<Space>(zone_Temp);
                    if (spaces_Temp == null)
                    {
                        continue;
                    }

                    foreach (Space space_Temp in spaces_Temp)
                    {
                        if (spaces_Result.Find(x => x.Name == space_Temp.Name) != null)
                        {
                            continue;
                        }

                        spaces_Result.Add(space_Temp);
                    }
                }
            }

            List<TM59ExtendedResult> tM59ExtendedResults = overheatingCalculator.Calculate_TM59(spaces_Result);

            List<TMResult> tM59MechanicalVentilationResults = tM59ExtendedResults.FindAll(x => x is TM59MechanicalVentilationExtendedResult)?.ConvertAll(x => (TMResult)x);
            List<TMResult> tM59NaturalVentilationResults = tM59ExtendedResults.FindAll(x => x is TM59NaturalVentilationExtendedResult)?.ConvertAll(x => (TMResult)x);
            List<TMResult> tM59CorridorResults = tM59ExtendedResults.FindAll(x => x is TM59CorridorExtendedResult)?.ConvertAll(x => (TMResult)x);

            if(!extended)
            {
                tM59MechanicalVentilationResults = tM59MechanicalVentilationResults?.ConvertAll(x => (x as TM59ExtendedResult)?.Simplify());
                tM59NaturalVentilationResults = tM59NaturalVentilationResults.ConvertAll(x => (x as TM59ExtendedResult)?.Simplify());
                tM59CorridorResults = tM59CorridorResults?.ConvertAll(x => (x as TM59ExtendedResult)?.Simplify());
            }

            IndexedDoubles maxIndoorComfortTemperatures = overheatingCalculator.GetMaxIndoorComfortTemperatures(0, 364);
            IndexedDoubles minIndoorComfortTemperatures = overheatingCalculator.GetMinIndoorComfortTemperatures(0, 364);

            index = Params.IndexOfOutputParam("spaces");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces_Result.ConvertAll(x => new GooSpace(x)));
            }

            index = Params.IndexOfOutputParam("tM59MechanicalVentilationResults");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59MechanicalVentilationResults.ConvertAll(x => new GooResult(x)));
            }

            index = Params.IndexOfOutputParam("tM59NaturalVentilationResults");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59NaturalVentilationResults.ConvertAll(x => new GooResult(x)));
            }

            index = Params.IndexOfOutputParam("tM59CorridorResults");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59CorridorResults.ConvertAll(x => new GooResult(x)));
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
            Menu_AppendItem(menu, "Open TSD", Menu_OpenTSD, Resources.SAM_TasTSD3, true, false);
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