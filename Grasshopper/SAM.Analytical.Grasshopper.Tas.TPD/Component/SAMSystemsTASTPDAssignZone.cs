using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Systems;
using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Grasshopper;
using SAM.Core.Systems;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using TPD;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class SAMSystemsTASTPDAssignZone : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0d4e8c4d-bef6-4204-b48c-acbae657407c");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTPD3;

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMSystemsTASTPDAssignZone()
          : base("TasTPD.AssignZone", "TasTPD.AssignZone",
              "Tas TPD Assign Zone",
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
                result.Add(new GH_SAMParam(new Param_FilePath() { Name = "_path_TPD", NickName = "_path_TPD", Description = "A file path to TAS TPD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new Param_FilePath() { Name = "_path_TSD", NickName = "_path_TSD", Description = "A file path to TAS TSD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooSystemGroupParam() { Name = "_airSystemGroup", NickName = "_airSystemGroup", Description = "Air System Groups", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "_zonesOrSpaces", NickName = "_zonesOrSpaces", Description = "Zones or spaces", Access = GH_ParamAccess.tree }, ParamVisibility.Binding));

                Param_Boolean @boolean = null;

                @boolean = new Param_Boolean() { Name = "_simulate_", NickName = "_simulate_", Description = "Simulate", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                @boolean = new Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new Param_FilePath() { Name = "path_TPD", NickName = "path_TPD", Description = "Path TPD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly imported?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
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
            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
            {
                return;
            }

            string path_TPD = null;
            index = Params.IndexOfInputParam("_path_TPD");
            if (index == -1 || !dataAccess.GetData(index, ref path_TPD) || string.IsNullOrWhiteSpace(path_TPD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TSD = null;
            index = Params.IndexOfInputParam("_path_TSD");
            if (index == -1 || !dataAccess.GetData(index, ref path_TSD) || string.IsNullOrWhiteSpace(path_TSD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<ISystemGroup> systemGroups = new List<ISystemGroup>();
            index = Params.IndexOfInputParam("_airSystemGroup");
            if (index == -1 || !dataAccess.GetDataList(index, systemGroups) || systemGroups == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            GH_Structure<GooAnalyticalObject> analyticalObjects = null;
            index = Params.IndexOfInputParam("_zonesOrSpaces");
            if (index != -1)
            {
                if (!dataAccess.GetDataTree(index, out analyticalObjects))
                {
                    analyticalObjects = null;
                }
            }

            if(analyticalObjects == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Tuple<AirSystemGroup, List<Space>>> tuples = new List<Tuple<AirSystemGroup, List<Space>>>();
            for (int i = 0; i <= systemGroups.Count; i++)
            {
                AirSystemGroup airSystemGroup = systemGroups[i] as AirSystemGroup;
                if(airSystemGroup == null)
                {
                    continue;
                }

                List<GooAnalyticalObject> gooAnalyticalObjects = analyticalObjects[new GH_Path(i)];
                if(gooAnalyticalObjects == null)
                {
                    continue;
                }

                List<Space> spaces = new List<Space>();

                foreach(GooAnalyticalObject gooAnalyticalObject in gooAnalyticalObjects)
                {
                    IAnalyticalObject analyticalObject = gooAnalyticalObject.Value;
                    if(analyticalObject is Space)
                    {
                        spaces.Add((Space)analyticalObject);
                        continue;
                    }
                    
                    if(analyticalObject is Zone)
                    {
                        Zone zone = (Zone)analyticalObject;
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Zone not implemented");
                    }
                }

                tuples.Add(new Tuple<AirSystemGroup, List<Space>>(airSystemGroup, spaces));
            }

            bool succedded = false;

            if (tuples != null && tuples.Count != 0)
            {
                using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
                {
                    global::TPD.TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                    if (tPDDoc != null)
                    {
                        EnergyCentre energyCentre = tPDDoc.EnergyCentre;
                        energyCentre.AddTSDData(path_TSD, 1);

                        TSDData tSDData = energyCentre.GetTSDData(1);

                        foreach (Tuple<AirSystemGroup, List<Space>> tuple in tuples)
                        {
                            bool zonesAssigned = Analytical.Tas.TPD.Modify.AssignZones(tPDDoc, tuple.Item1, tuple.Item2);
                            if(zonesAssigned)
                            {
                                succedded = true;
                            }
                        }
                    }
                }
            }

            index = Params.IndexOfOutputParam("path_TPD");
            if (index != -1)
            {
                dataAccess.SetData(index, path_TPD);
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, succedded);
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

            Process.Start(path);
        }
    }
}