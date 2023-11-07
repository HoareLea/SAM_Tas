using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class FromTBD : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("9962a735-34b0-4e5c-8647-285d5baaab62");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public FromTBD()
          : base("SAMAnalytical.FromTBD", "SAMAnalytical.FromTBD",
              "Create AnalyticalModel From TBD File",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            //int aIndex = -1;
            //Param_Boolean booleanParameter = null;

            inputParamManager.AddTextParameter("_pathTasTBD", "_pathTasTBD", "The string path to a TasTBD file.", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("_importUnused_", "_importUnused_", "Import Unused IC", GH_ParamAccess.item, false);
            inputParamManager.AddBooleanParameter("_run", "_run", "Connect a boolean toggle to run.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new GooAnalyticalModelParam(), "analyticalModel", "analyticalModel", "SAM AnalyticalModel", GH_ParamAccess.list);
            outputParamManager.AddBooleanParameter("successful", "successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(1, false);

            bool run = false;
            if (!dataAccess.GetData(2, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            if (!run)
                return;

            string path_TBD = null;
            if (!dataAccess.GetData(0, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool importUnused = false;
            if (!dataAccess.GetData(1, ref importUnused))
            {
                importUnused = false;
            }

            AnalyticalModel analyticalModel = Analytical.Tas.Convert.ToSAM(path_TBD, importUnused);

            dataAccess.SetData(0, analyticalModel);
            dataAccess.SetData(1, analyticalModel != null);
        }
    }
}