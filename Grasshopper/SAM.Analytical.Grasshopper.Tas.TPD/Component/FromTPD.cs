using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Grasshopper;
using SAM.Core.Grasshopper.Systems;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class FromTPD : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c88b33fb-5c8f-442e-bd43-d9a2809c2bc2");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTPD;

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public FromTPD()
          : base("SAMAnalytical.FromTPD", "SAMAnalytical.FromTPD",
              "Converts from TPD to SystemEnergyCentre",
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

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings();

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_simulate_", NickName = "_simulate_", Description = "Simulate before collecting data", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(systemEnergyCentreConversionSettings.Simulate);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Integer integer = null;

                integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_startHour_", NickName = "_startHour_", Description = "Simulation start hour", Access = GH_ParamAccess.item };
                integer.SetPersistentData(systemEnergyCentreConversionSettings.StartHour);
                result.Add(new GH_SAMParam(integer, ParamVisibility.Voluntary));

                integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_endHour_", NickName = "_endHour_", Description = "Simulation end hour", Access = GH_ParamAccess.item };
                integer.SetPersistentData(systemEnergyCentreConversionSettings.EndHour);
                result.Add(new GH_SAMParam(integer, ParamVisibility.Voluntary));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
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
                result.Add(new GH_SAMParam(new GooSystemEnergyCentreParam() { Name = "systemEnergyCentre", NickName = "systemEnergyCentre", Description = "SAM Core Systems SystemEnergyCentre", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
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

            SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings();

            bool simulate = systemEnergyCentreConversionSettings.Simulate;
            index = Params.IndexOfInputParam("_simulate_");
            if (index != -1 && dataAccess.GetData(index, ref simulate))
            {
                systemEnergyCentreConversionSettings.Simulate = simulate;
            }

            int startHour = systemEnergyCentreConversionSettings.StartHour;
            index = Params.IndexOfInputParam("_startHour_");
            if (index != -1 && dataAccess.GetData(index, ref startHour))
            {
                systemEnergyCentreConversionSettings.StartHour = startHour;
            }

            int endHour = systemEnergyCentreConversionSettings.EndHour;
            index = Params.IndexOfInputParam("_endHour_");
            if (index != -1 && dataAccess.GetData(index, ref endHour))
            {
                systemEnergyCentreConversionSettings.EndHour = endHour;
            }

            string path = null;
            index = Params.IndexOfInputParam("_path_TPD");
            if (index == -1 || !dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            SystemEnergyCentre systemEnergyCentre = Analytical.Tas.TPD.Convert.ToSAM(path, systemEnergyCentreConversionSettings);

            index = Params.IndexOfOutputParam("systemEnergyCentre");
            if (index != -1)
                dataAccess.SetData(index, new GooSystemEnergyCentre(systemEnergyCentre));

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, systemEnergyCentre != null);
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

            ToolStripMenuItem toolStripMenuItem = Menu_AppendItem(menu, "Open TPD", OnOpenTPDComponentClick, Resources.SAM_Small);
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