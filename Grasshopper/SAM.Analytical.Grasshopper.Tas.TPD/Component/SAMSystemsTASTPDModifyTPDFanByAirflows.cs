// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class SAMSystemsTASTPDModifyTPDFanByAirflows : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMSystemsTASTPDModifyTPDFanByAirflows()
          : base("TasTPD.ModifyTPDFanByAirflows", "TasTPD.ModifyTPDFanByAirflows",
              "Updates design flow settings for selected fans in a Tas TPD file.\n\nUse this component to modify the design flow source and design flow rate for selected system fans.\n\nThe design flow source can be set using the enum name or an integer value from 0 to 7. The component runs only when _run is true and returns the updated fans.",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new ("a6879fb3-7877-4f5e-b6c8-edd5db181358");

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

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

            new GH_SAMParam(new Systems.GooSystemComponentParam()
            {
                Name = "_displaySystemFans",
                NickName = "_displaySystemFans",
                Description = "System fans to update in the TPD file.",
                Access = GH_ParamAccess.list
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_String()
            {
                Name = "_designFlowSources",
                NickName = "_designFlowSources",
                //Description = "Design flow source for each fan.\nYou can use the enum name or integer value.\n0 = None\n1 = Value\n2 = All Attached Zones Flow Rate\n3 = All Attached Zones Fresh Air\n4 = Nearest Zone Flow Rate\n5 = Nearest Zone Fresh Air\n6 = Sized\n7 = All Attached Zones Sized",
                Description = "Defines the design flow source for each fan.\nYou can use the enum name or integer value.\n0 = None\n1 = Value\n2 = All Attached Zones Flow Rate\n3 = All Attached Zones Fresh Air\n4 = Nearest Zone Flow Rate\n5 = Nearest Zone Fresh Air\n6 = Sized\n7 = All Attached Zones Sized",
                Access = GH_ParamAccess.list,
                Optional = true
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_Number()
            {
                Name = "_designFlowRates",
                NickName = "_designFlowRates",
                Description = "Design flow rates for the listed fans.\nUsed to update the fan design flow value.",
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

            new GH_SAMParam(new Systems.GooSystemComponentParam()
            {
                Name = "displaySystemFans",
                NickName = "displaySystemFans",
                Description = "Updated system fans returned from the TPD file.",
                Access = GH_ParamAccess.list
            }, ParamVisibility.Binding),

            new GH_SAMParam(new Param_Boolean()
            {
                Name = "successful",
                NickName = "successful",
                Description = "True if one or more fans were successfully updated; otherwise false.",
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
            int index_successful = Params.IndexOfOutputParam("successful");
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

            List<ISystemComponent> systemComponents = [];
            index = Params.IndexOfInputParam("_displaySystemFans");
            if (index == -1 || !dataAccess.GetDataList(index, systemComponents) || systemComponents is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<SystemFan> systemFans = [.. systemComponents.FindAll(x => x is SystemFan).Cast<SystemFan>()];

            List<string> designFlowSourceNames = [];
            index = Params.IndexOfInputParam("_designFlowSources");
            if (index != -1)
            {
                if(!dataAccess.GetDataList(index, designFlowSourceNames) || designFlowSourceNames is null)
                {
                    designFlowSourceNames = [];
                }
            }

            List<double> designFlowRates = [];
            index = Params.IndexOfInputParam("_designFlowRates");
            if (index != -1)
            {
                if (!dataAccess.GetDataList(index, designFlowRates) || designFlowRates is null)
                {
                    designFlowRates = [];
                }
            }

            if(systemFans != null && systemFans.Count > 0)
            {
                for (int i = 0; i < systemFans.Count; i++)
                {
                    SystemFan systemFan = systemFans[i];
                    if(systemFan is null)
                    {
                        continue;
                    }

                    systemFan = systemFan.Clone();

                    if(designFlowSourceNames.Count != 0)
                    {
                        string designFlowSourceName = designFlowSourceNames[Core.Query.Clamp(i, 0, designFlowSourceNames.Count - 1)];
                        if(Core.Query.TryGetEnum(designFlowSourceName, out FlowRateType flowRateType))
                        {
                            systemFan.DesignFlowType = flowRateType;
                        }
                        else if(Core.Query.TryConvert(designFlowSourceName, out int id))
                        {
                            systemFan.DesignFlowType = (FlowRateType)id;
                        }
                    }

                    if(designFlowRates.Count != 0)
                    {
                        double designFlowRate = designFlowRates[Core.Query.Clamp(i, 0, designFlowRates.Count - 1)];

                        if(systemFan.DesignFlowRate is DesignConditionSizedFlowValue designConditionSizedFlowValue)
                        {
                            systemFan.DesignFlowRate = new DesignConditionSizedFlowValue(
                                designFlowRate, 
                                designConditionSizedFlowValue.SizeFranction,
                                designConditionSizedFlowValue.SizingType,
                                designConditionSizedFlowValue.SizeValue1,
                                designConditionSizedFlowValue.SizeValue2,
                                designConditionSizedFlowValue.SizedFlowMethod,
                                designConditionSizedFlowValue.DesignConditionNames);
                        }
                        else if (systemFan.DesignFlowRate is SizedFlowValue sizedFlowValue)
                        {
                            systemFan.DesignFlowRate = new SizedFlowValue(designFlowRate, sizedFlowValue.SizeFranction);
                        }
                    }

                    systemFans[i] = systemFan;
                }

                systemFans = Analytical.Tas.TPD.Modify.UpdateFanAirflows(path, systemFans);
            }

            index = Params.IndexOfOutputParam("path_TPD");
            if (index != -1)
            {
                dataAccess.SetData(index, path);
            }

            index = Params.IndexOfOutputParam("displaySystemFans");
            if (index != -1)
            {
                dataAccess.SetDataList(index, systemFans);
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, systemFans != null && systemFans.Count != 0);
            }

        }
        
        private void OnOpenTPDComponentClick(object sender, EventArgs e)
        {
            if (Params.Input == null || Params.Input.Count == 0)
            {
                return;
            }

            IEnumerable<object> paths =  Params.Input[0]?.VolatileData?.AllData(true);
            if(paths == null || !paths.Any())
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