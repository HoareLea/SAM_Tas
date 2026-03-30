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
    public class SAMSystemsTASTPDModifyTPDByAirflows : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMSystemsTASTPDModifyTPDByAirflows()
            : base("TasTPD.ModifyTPDByAirflows", "TasTPD.ModifyTPDByAirflows",
          "Updates airflow and/or fresh air settings for selected spaces in a Tas TPD file.\n\nUse this component to modify space airflow values by name. For each space, you can keep the current value, apply a new value, or reset it.\n\nThe component runs only when _run is true and returns the names of spaces that were successfully updated.",
          "SAM", "Tas")
        {
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new ("38802d30-dc1e-4cd0-b4cb-4731d536f0cf");

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.3";

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
                Param_Boolean param_Boolean = new ()
                {
                    Name = "_run",
                    NickName = "_run",
                    Description = "Set to true to run the update. If false, the component does nothing.",
                    Access = GH_ParamAccess.item
                };
                param_Boolean.SetPersistentData(false);

                return
                [
                    new GH_SAMParam(new Param_FilePath()
            {
                Name = "_path_TPD",
                NickName = "_path_TPD",
                Description = "Full file path to the Tas TPD file to update.",
                Access = GH_ParamAccess.item
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_String()
            {
                Name = "_spaceNames",
                NickName = "_spaceNames",
                Description = "Names of the spaces to update in the TPD file.",
                Access = GH_ParamAccess.list
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_Number()
            {
                Name = "_airflowFlowRates",
                NickName = "_airflowFlowRates",
                Description = "Airflow flow rates for the listed spaces [l/s]. Used when _airflowModifies = 1.",
                Access = GH_ParamAccess.list,
                Optional = true
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_Number()
            {
                Name = "_airflowModifies",
                NickName = "_airflowModifies",
                Description = "Airflow update mode for each space: 0 = do not change, 1 = set value, 2 = reset.\nDefault is 0.",
                Access = GH_ParamAccess.list,
                Optional = true
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_Number()
            {
                Name = "_airflowFreshAirRates",
                NickName = "_airflowFreshAirRates",
                Description = "Fresh air flow rates for the listed spaces [l/s]. Used when _airflowFreshAirModifies = 1.",
                Access = GH_ParamAccess.list,
                Optional = true
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_Number()
            {
                Name = "_airflowFreshAirModifies",
                NickName = "_airflowFreshAirModifies",
                Description = "Fresh air update mode for each space: 0 = do not change, 1 = set value, 2 = reset.\nDefault is 0.",
                Access = GH_ParamAccess.list,
                Optional = true
            }, ParamVisibility.Binding),

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
                    new GH_SAMParam(new Param_FilePath()
            {
                Name = "path_TPD",
                NickName = "path_TPD",
                Description = "File path to the processed Tas TPD file.",
                Access = GH_ParamAccess.item
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_String()
            {
                Name = "spaceNames",
                NickName = "spaceNames",
                Description = "Names of the spaces that were successfully updated.",
                Access = GH_ParamAccess.list
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_Boolean()
            {
                Name = "Successful",
                NickName = "successful",
                Description = "True if one or more spaces were successfully updated; otherwise false.",
                Access = GH_ParamAccess.item
            }, ParamVisibility.Binding)
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

            List<string> spacesNames = [];
            index = Params.IndexOfInputParam("_spaceNames");
            if (index == -1 || !dataAccess.GetDataList(index, spacesNames) || spacesNames is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<double> airflows = [];
            index = Params.IndexOfInputParam("_airflowFlowRates");
            if (index != -1)
            {
                if(!dataAccess.GetDataList(index, airflows) || airflows is null)
                {
                    airflows = [];
                }
            }

            List<double> freshAirs = [];
            index = Params.IndexOfInputParam("_airflowFreshAirRates");
            if (index != -1)
            {
                if(!dataAccess.GetDataList(index, freshAirs) || freshAirs is null)
                {
                    freshAirs = [];
                }
            }

            List<double> airflows_Code = [];
            index = Params.IndexOfInputParam("_airflowModifies");
            if (index != -1)
            {
                if (!dataAccess.GetDataList(index, airflows_Code) || airflows_Code is null)
                {
                    airflows_Code = [];
                }
            }

            List<double> freshAirs_Code = [];
            index = Params.IndexOfInputParam("_airflowFreshAirModifies");
            if (index != -1)
            {
                if (!dataAccess.GetDataList(index, freshAirs_Code) || freshAirs_Code is null)
                {
                    freshAirs_Code = [];
                }
            }

            List<string> spaceNames_Updated = [];

            if (!((airflows is null || airflows.Count == 0) && (freshAirs is null || freshAirs.Count == 0)))
            {
                Dictionary<string, Tuple<double?, double?>> dictionary = [];
                for (int i = 0; i < spacesNames.Count; i++)
                {
                    string name = spacesNames[i];
                    if (name == null)
                    {
                        continue;
                    }

                    int code;

                    //Air Flow
                    code = 0;
                    if(airflows_Code.Count != 0)
                    {
                        code = System.Convert.ToInt32(airflows_Code[Core.Query.Clamp(i, 0, airflows_Code.Count - 1)]);
                    }

                    double? airflow = null;
                    if (code == 1 && airflows.Count != 0)
                    {
                        airflow = airflows[Core.Query.Clamp(i, 0, airflows.Count - 1)];
                    }
                    else if(code  == 2)
                    {
                        airflow = double.NaN;
                    }

                    //Fresh Air
                    code = 0;
                    if (freshAirs_Code.Count != 0)
                    {
                        code = System.Convert.ToInt32(freshAirs_Code[Core.Query.Clamp(i, 0, freshAirs_Code.Count - 1)]);
                    }

                    double? freshAir = null;
                    if (code == 1 && freshAirs.Count != 0)
                    {
                        freshAir = freshAirs[Core.Query.Clamp(i, 0, freshAirs.Count - 1)];
                    }
                    else if (code == 2)
                    {
                        freshAir = double.NaN;
                    }

                    dictionary[spacesNames[i]] = new Tuple<double?, double?>(airflow, freshAir);
                }

                spaceNames_Updated = Analytical.Tas.TPD.Modify.UpdateSpaceAirflows(path, dictionary);
            }

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