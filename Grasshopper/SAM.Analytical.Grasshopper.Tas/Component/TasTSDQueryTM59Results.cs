using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

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
        public override string LatestComponentVersion => "1.0.0";

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDQueryTM59Results()
          : base("Tas.TSDQueryTM59Results", "Tas.TSDQueryTM59Results",
              "Query TSD for TM59Results" +
               "this node will query results for given space and output when inspect results",
              "SAM WIP", "Tas")
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
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "_spaces_", NickName = "_spaces_", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean boolean;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_extended_", NickName = "_extended_", Description = "Return extended results", Access = GH_ParamAccess.item, Optional = true };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String { Name = "_tM52BuildingCategory", NickName = "_tM52BuildingCategory", Description = "Category of Buildings I, II, III or IV", Access = GH_ParamAccess.item, Optional = true };
                @string.SetPersistentData(TM52BuildingCategory.CategoryII.ToString());
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59MechanicalVentilationResults", NickName = "tM59MechanicalVentilationResults", Description = "SAM TM59 Mechanical Ventilation Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59NaturalVentilationResults", NickName = "tM59NaturalVentilationResults", Description = "SAM TM59 Natural Ventilation Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59CorridorResults", NickName = "tM59CorridorResults", Description = "SAM TM59 Corridor Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortUpperLimitTemperatures", NickName = "indoorComfortULTemperatures Tupp", Description = "Indoor Comfort Upper Limit Temperatures Tupp \nTcomf = 0.33 Trm + 18.8  where TuppCatII =0.33 Trm + 18.8-4 ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortLowerLimitTemperatures", NickName = "indoorComfortLLTemperatures Tupp", Description = "Indoor Comfort Lower Limit Temperatures Tupp \nTcomf = 0.33 Trm + 18.8  where TuppCatII =0.33 Trm + 18.8-4 ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

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
            index = Params.IndexOfInputParam("_spaces_");
            if (index != -1)
            {
                spaces = new List<Space>();
                if(!dataAccess.GetDataList(index, spaces))
                {
                    spaces = null;
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

            bool extended = false;
            index = Params.IndexOfInputParam("_extended_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref extended))
                {
                    extended = false;
                }
            }

            TSDConversionSettings tSDConversionSettings = new TSDConversionSettings()
            {
                SpaceDataTypes = new HashSet<SpaceDataType>() { SpaceDataType.ResultantTemperature, SpaceDataType.OccupantSensibleGain },
                SpaceNames = spaces == null ? null : new HashSet<string>(spaces.ConvertAll(x => x?.Name)),
                ConvertWeaterData = true,
                ConvertZones = true
            };

            AnalyticalModel analyticalModel = Analytical.Tas.Convert.ToSAM(path, tSDConversionSettings);

            OverheatingCalculator overheatingCalculator = new OverheatingCalculator(analyticalModel)
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
    }
}