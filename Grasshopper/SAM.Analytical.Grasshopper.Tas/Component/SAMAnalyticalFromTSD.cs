using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalFromTSD : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ad91de58-c8d3-46dc-b648-20e787694a13");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalFromTSD()
          : base("SAMAnalytical.FromTSD", "SAMAnalytical.FromTSD",
              "From TSD",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTSD", NickName = "_pathTasTSD", Description = "Path Tas TSD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "analyticalModel", NickName = "analyticalModel", Description = "SAM Analytical AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "pathTasTBD", NickName = "pathTasTBD", Description = "Path Tas TBD", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
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
                return;

            string path_TSD = null;
            index = Params.IndexOfInputParam("_pathTasTSD");
            if (index == -1 || !dataAccess.GetData(index, ref path_TSD) || path_TSD == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            TSDConversionSettings tSDConversionSettings = new TSDConversionSettings()
            {
                ConvertWeaterData = true,
                ConvertZones = true,
            };

            string path_TBD = null;

            AnalyticalModel analyticalModel = Analytical.Tas.Convert.ToSAM(path_TSD, tSDConversionSettings);
            if(analyticalModel != null)
            {
                path_TBD = analyticalModel.GetValue<string>(Analytical.Tas.AnalyticalModelParameter.Path_TBD);
            }
            
            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
            {
                dataAccess.SetData(index, analyticalModel);
            }

            index = Params.IndexOfOutputParam("pathTasTBD");
            if (index != -1)
            {
                dataAccess.SetData(index, path_TBD);
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, analyticalModel != null);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);
            AppendOpenTSDAdditionalMenuItem(this, menu);
        }

        public ToolStripMenuItem AppendOpenTSDAdditionalMenuItem(IGH_SAMComponent gH_SAMComponent, ToolStripDropDown menu)
        {
            if (!(gH_SAMComponent is GH_Component gH_Component))
            {
                return null;
            }

            ToolStripMenuItem toolStripMenuItem = null;

            toolStripMenuItem = Menu_AppendItem(menu, "Open TSD", OnOpenTSDComponentClick, Resources.SAM_TasTSD3);
            if (toolStripMenuItem != null)
            {
                toolStripMenuItem.Tag = gH_Component.InstanceGuid;
            }

            return toolStripMenuItem;
        }

        private void OnOpenTSDComponentClick(object sender, EventArgs e)
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