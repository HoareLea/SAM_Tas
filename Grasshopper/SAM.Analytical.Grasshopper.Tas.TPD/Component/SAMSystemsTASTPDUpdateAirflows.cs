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
    public class SAMSystemsTASTPDUpdateAirflows : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMSystemsTASTPDUpdateAirflows()
          : base("TasTPD.ModifyTPDByAirflows", "TasTPD.ModifyTPDByAirflows",
              "Modifies TPD By Airflows.",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("38802d30-dc1e-4cd0-b4cb-4731d536f0cf");

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
                return
                [
                    new GH_SAMParam(new Param_FilePath() { Name = "_path_TPD", NickName = "_path_TPD", Description = "A file path to TAS TPD", Access = GH_ParamAccess.item }, ParamVisibility.Binding),
                    new GH_SAMParam(new Param_String() { Name = "_spaceNames", NickName = "_spaceNames", Description = "Space Names", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(new Param_Number() { Name = "_airflowFlowRates", NickName = "_airflowFlowRates", Description = "Airflow flow rates [l/s]", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding),
                    new GH_SAMParam(new Param_Number() { Name = "_airflowFreshAirRates", NickName = "_airflowFreshAirRates", Description = "Airflow fresh air rate [l/s]", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding),
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
            int index;

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

            List<string> spaceNames_Updated = [];

            if (!((airflows is null || airflows.Count == 0) && (freshAirs is null || freshAirs.Count == 0)))
            {
                Dictionary<string, Tuple<double, double>> dictionary = [];
                for (int i = 0; i < spacesNames.Count; i++)
                {
                    string name = spacesNames[i];
                    if (name == null)
                    {
                        continue;
                    }

                    double airflow = double.NaN;
                    double freshAir = double.NaN;

                    if(airflows is not null && airflows.Count != 0)
                    {
                        airflow = airflows[Core.Query.Clamp(i, 0, airflows.Count - 1)];
                    }

                    if (freshAirs is not null && freshAirs.Count != 0)
                    {
                        freshAir = freshAirs[Core.Query.Clamp(i, 0, freshAirs.Count - 1)];
                    }

                    dictionary[spacesNames[i]] = new Tuple<double, double>(airflow, freshAir);
                }

                spaceNames_Updated = Analytical.Tas.TPD.Modify.UpdateAirflows(path, dictionary);
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