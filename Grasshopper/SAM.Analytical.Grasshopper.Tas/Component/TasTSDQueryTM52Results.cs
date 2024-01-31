using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.Obsolete
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
        public override string LatestComponentVersion => "1.0.0";

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDQueryTM52Results()
          : base("Tas.TSDQueryTM52Results", "Tas.TSDQueryTM52Results",
              "Query TSD for TM52Results",
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
                
                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String { Name = "_tM52BuildingCategory", NickName = "_tM52BuildingCategory", Description = "Category of Buildings I, II, III or IV", Access = GH_ParamAccess.item, Optional = true };
                @string.SetPersistentData(TM52BuildingCategory.CategoryII.ToString());
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Integer @integer;

                @integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_startHourOfYear_", NickName = "_startHourOfYear_", Description = "Start Hour of Year Index", Access = GH_ParamAccess.item, Optional = true };
                @integer.SetPersistentData(2880);
                result.Add(new GH_SAMParam(@integer, ParamVisibility.Binding));

                @integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_endHourOfYear_", NickName = "_endHourOfYear_", Description = "End Hour of Year Index", Access = GH_ParamAccess.item, Optional = true };
                @integer.SetPersistentData(6528);
                result.Add(new GH_SAMParam(@integer, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "spaces", NickName = "spaces", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "spaceTM52Results", NickName = "spaceTM52Results", Description = "SAM Space TM52 Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortTemperatures", NickName = "indoorComfortTemperatures", Description = "Indoor Comfort Temperatures", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

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
            int startHourOfYear = 2880;
            if (index != -1)
            {
                if(!dataAccess.GetData(index, ref startHourOfYear))
                {
                    startHourOfYear = 2880;
                }
            }

            index = Params.IndexOfInputParam("_endHourOfYear_");
            int endHourOfYear = 6528;
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref endHourOfYear))
                {
                    endHourOfYear = 6528;
                }
            }


            TSDConversionSettings tSDConversionSettings = new TSDConversionSettings()
            {
                SpaceDataTypes = new HashSet<SpaceDataType>() { SpaceDataType.ResultantTemperature, SpaceDataType.OccupantSensibleGain },
                SpaceNames = spaces == null ? null : new HashSet<string>(spaces.ConvertAll(x => x?.Name)),
                ConvertWeaterData = true,
            };

            AnalyticalModel analyticalModel = Analytical.Tas.Convert.ToSAM(path, tSDConversionSettings);

            OverheatingCalculator overheatingCalculator = new OverheatingCalculator(analyticalModel)
            {
                TM52BuildingCategory = tM52BuildingCategory,
                StartHourOfYear = startHourOfYear,
                EndHourOfYear = endHourOfYear,
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

            List<SpaceTM52Result> spaceTM52Results = overheatingCalculator.Calculate(spaces_Result);

            List<double> indoorComfortTemperatures = overheatingCalculator.GetIndoorComfortTemperatures();

            index = Params.IndexOfOutputParam("spaces");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces_Result.ConvertAll(x => new GooSpace(x)));
            }

            index = Params.IndexOfOutputParam("spaceTM52Results");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaceTM52Results.ConvertAll(x => new GooResult(x)));
            }

            index = Params.IndexOfOutputParam("indoorComfortTemperatures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, indoorComfortTemperatures);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }
    }
}