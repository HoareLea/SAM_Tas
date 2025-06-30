using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Analytical.Systems;
using SAM.Core.Grasshopper;
using SAM.Core.Tas.TPD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class TasSystemResults : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("fd882916-69fd-43f5-aa19-cb96c993daa3");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.3";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTPD3;


        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasSystemResults()
          : base("Tas.SystemResults", "Tas.SystemResults",
              "Converts SAM Analytical to System Results",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_FilePath() { Name = "_path_TPD", NickName = "_path_TPD", Description = "A file path to TAS TPD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "plantRoomIndexes_", NickName = "plantRoomIndexes", Description = "Indexes of the plant pooms starting from 0", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_systemEnergyCentreDataTypes", NickName = "_systemEnergyCentreDataTypes", Description = "System Energy Centre Data Types", Access = GH_ParamAccess.list}, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String param_String = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_resultPeriod_", NickName = "_resultPeriod_", Description = "Result Period", Access = GH_ParamAccess.item, Optional = true };
                param_String.SetPersistentData(ResultPeriod.Annual.ToString());
                result.Add(new GH_SAMParam(param_String, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean param_Boolean = null;

                param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_detailedCategoryView_", NickName = "_detailedCategoryView_", Description = "Detailed Category View", Access = GH_ParamAccess.item, Optional = true };
                param_String.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Binding));

                param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_regulatedEnergyOnly_", NickName = "_regulatedEnergyOnly_", Description = "Regulated Energy Only", Access = GH_ParamAccess.item, Optional = true };
                param_String.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Binding));

                param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_perUnitArea_", NickName = "_perUnitArea_", Description = "Results per Unit Area", Access = GH_ParamAccess.item, Optional = true };
                param_String.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Binding));

                param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                param_Boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "systemEnergyCentreResults", NickName = "systemEnergyCentreResults", Description = "SAM SystemEnergyCentreResults", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly imported?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_successful = Params.IndexOfOutputParam("successful");
            if(index_successful != -1)
            {
                dataAccess.SetData(index_successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
                return;

            string path = null;
            index = Params.IndexOfInputParam("_path_TPD");
            if (index == -1 || !dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<int> plantRoomIndexes = new List<int>();
            index = Params.IndexOfInputParam("plantRoomIndexes_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, plantRoomIndexes);
            }

            if(plantRoomIndexes != null && plantRoomIndexes.Count == 0)
            {
                plantRoomIndexes = null;
            }

            List<string> systemEnergyCentreDataTypeStrings = new List<string>();
            index = Params.IndexOfInputParam("_systemEnergyCentreDataTypes");
            if (index == -1 || !dataAccess.GetDataList(index, systemEnergyCentreDataTypeStrings) || systemEnergyCentreDataTypeStrings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<SystemEnergyCentreDataType> systemEnergyCentreDataTypes = systemEnergyCentreDataTypeStrings.ConvertAll(x => Core.Query.Enum<SystemEnergyCentreDataType>(x));
            systemEnergyCentreDataTypes.RemoveAll(x => x == SystemEnergyCentreDataType.Undefined);

            if(systemEnergyCentreDataTypes == null || systemEnergyCentreDataTypes.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid ResultDataTypes");
                return;
            }

            bool detailedCategoryView = false;
            index = Params.IndexOfInputParam("_detailedCategoryView_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref detailedCategoryView);
            }

            bool regulatedEnergyOnly = false;
            index = Params.IndexOfInputParam("_regulatedEnergyOnly_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref regulatedEnergyOnly);
            }

            bool perUnitArea = false;
            index = Params.IndexOfInputParam("_perUnitArea_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref perUnitArea);
            }


            ResultPeriod resultPeriod = ResultPeriod.Annual;
            index = Params.IndexOfInputParam("_resultPeriod_");
            if (index != -1)
            {
                string resultPeriodString = null;

                if(dataAccess.GetData(index, ref resultPeriodString))
                {
                    resultPeriod = Core.Query.Enum<ResultPeriod>(resultPeriodString);
                }
            }

            List<SystemEnergyCentreResult> SystemEnergyCentreResults = Core.Tas.TPD.Query.SystemEnergyCentreResults(path, resultPeriod, systemEnergyCentreDataTypes, plantRoomIndexes, detailedCategoryView, regulatedEnergyOnly, perUnitArea);

            index = Params.IndexOfOutputParam("systemEnergyCentreResults");
            if (index != -1)
                dataAccess.SetDataList(index, SystemEnergyCentreResults);

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, SystemEnergyCentreResults != null);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);
            AppendOpenTPDAdditionalMenuItem(this, menu);
        }

        public ToolStripMenuItem AppendOpenTPDAdditionalMenuItem(IGH_SAMComponent gH_SAMComponent, ToolStripDropDown menu)
        {
            if (!(gH_SAMComponent is GH_Component gH_Component))
            {
                return null;
            }

            ToolStripMenuItem toolStripMenuItem = Menu_AppendItem(menu, "Open TPD", OnOpenTPDComponentClick, Resources.SAM_TasTPD3);
            if (toolStripMenuItem != null)
            {
                toolStripMenuItem.Tag = gH_Component.InstanceGuid;
            }

            return toolStripMenuItem;
        }

        private void OnOpenTPDComponentClick(object sender, EventArgs e)
        {
            if (Params.Input == null || Params.Input.Count == 0)
            {
                return;
            }

            IEnumerable<object> paths = Params.Input[0]?.VolatileData?.AllData(true);
            if (paths == null || paths.Count() == 0)
            {
                return;
            }

            string path = null;

            foreach (object path_Temp in paths)
            {
                string value = path_Temp?.ToString();
                if (path_Temp is IGH_Goo)
                {
                    value = (path_Temp as dynamic)?.Value;
                }

                if (string.IsNullOrWhiteSpace(value) || !System.IO.File.Exists(value))
                {
                    continue;
                }

                path = value;
                break;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Core.Query.StartProcess(path);
        }
    }
}