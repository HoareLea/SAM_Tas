// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class SAMSystemsTASTPDModifyTPDByAirflowsBySpaces : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMSystemsTASTPDModifyTPDByAirflowsBySpaces()
          : base("TasTPD.ModifyTPDByAirflowsBySpaces", "TasTPD.ModifyTPDByAirflowsBySpaces",
              "Modifies TPD By Airflows.",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new ("69b0df94-84b4-40e2-abee-3adb0dc3b5df");

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTPD3;
        
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                Param_Boolean param_Boolean = new () { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                param_Boolean.SetPersistentData(false);

                return
                [
                    new GH_SAMParam(new Param_FilePath() { Name = "_path_TPD", NickName = "_path_TPD", Description = "A file path to TAS TPD", Access = GH_ParamAccess.item }, ParamVisibility.Binding),
                    new GH_SAMParam(new GooSpaceParam() { Name = "_spaces", NickName = "_spaces", Description = "Spaces", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(param_Boolean)
                ];
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                return
                [
                    new GH_SAMParam(new Param_FilePath() { Name = "path_TPD", NickName = "path_TPD", Description = "Path TPD", Access = GH_ParamAccess.item }, ParamVisibility.Binding),
                    new GH_SAMParam(new Param_String() { Name = "spaceNames", NickName = "spaceNames", Description = "Space Names", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(new Param_Boolean() { Name = "Successful", NickName = "successful", Description = "Correctly imported?", Access = GH_ParamAccess.item }, ParamVisibility.Binding)
                ];
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
            if (gH_SAMComponent is not GH_Component gH_Component)
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

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_successful = Params.IndexOfOutputParam("Successful");
            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
            {
                run = false;
            }

            if (!run)
            {
                return;
            }

            string path = null;
            index = Params.IndexOfInputParam("_path_TPD");
            if (index == -1 || !dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Space> spaces = [];
            index = Params.IndexOfInputParam("_spaces");
            if (index == -1 || !dataAccess.GetDataList(index, spaces) || spaces is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Dictionary<string, Tuple<double?, double?>> dictionary = [];
            foreach(Space space in spaces)
            {
                string name= space?.Name;
                if(name is null)
                {
                    continue;
                }

                PartFSpaceData partFSpaceData = space?.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData);
                if(partFSpaceData is null)
                {
                    continue;
                }

                if(partFSpaceData.CalculatedFlowRate_Lps == null || double.IsNaN(partFSpaceData.CalculatedFlowRate_Lps.Value))
                {
                    continue;
                }

                dictionary[space.Name] = new Tuple<double?, double?>(partFSpaceData.CalculatedFlowRate_Lps.Value, double.NaN);
            }

            List<string> spaceNames_Updated = Analytical.Tas.TPD.Modify.UpdateSpaceAirflows(path, dictionary);

            index = Params.IndexOfOutputParam("path_TPD");
            if (index != -1)
            {
                dataAccess.SetData(index, path);
            }

            index = Params.IndexOfOutputParam("spaceNames");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaceNames_Updated);
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, spaceNames_Updated != null && spaceNames_Updated.Count != 0);
            }

        }
        
        private void OnOpenTPDComponentClick(object sender, EventArgs e)
        {
            if (Params.Input == null || Params.Input.Count == 0)
            {
                return;
            }

            IEnumerable<object> paths =  Params.Input[0]?.VolatileData?.AllData(true);
            if(paths == null || paths.Count() == 0)
            {
                return;
            }

            string path = null;

            foreach(object path_Temp in paths)
            {
                string value = path_Temp?.ToString();
                if(path_Temp is IGH_Goo)
                {
                    value = (path_Temp as dynamic)?.Value;
                }

                if(string.IsNullOrWhiteSpace(value) || !System.IO.File.Exists(value))
                {
                    continue;
                }

                path = value;
                break;
            }

            if(string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Core.Query.StartProcess(path);
        }
    }
}