using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasSizing : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("704f954c-7ee1-41b8-ac85-3bf776ca4f36");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasSizing()
          : base("Tas.Sizing", "Tas.Sizing",
              "Sizing TAS TBD File",
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

            int index = -1;

            inputParamManager.AddTextParameter("_path_TasTBD", "pathTasTBD", "string path to TasTBD file", GH_ParamAccess.item);
            index = inputParamManager.AddParameter(new GooAnalyticalModelParam(), "_analyticalModel_", "_analyticalModel_", "SAM AnalyticalModel", GH_ParamAccess.item);
            inputParamManager[index].Optional = true;

            inputParamManager.AddBooleanParameter("_excludeOutdoorAir_", "_excludeOutdoorAir_", "Exclude Outdoor Air and set TBD ventilation factor to zero", GH_ParamAccess.item, false);
            inputParamManager.AddBooleanParameter("_excludePositiveInternalGains_", "_excludePositiveInternalGains_", "This will re-run few times sizing simulation for each temperature and therefore internal conduction gains from adj rooms will not offset heating load", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("run_", "run_", "Connect Bool Toggle to run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddBooleanParameter("Successful", "Successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(0, false);

            bool run = false;
            if (!dataAccess.GetData(4, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            if (!run)
                return;

            AnalyticalModel analyticalModel = null;
            if (!dataAccess.GetData(1, ref analyticalModel))
                analyticalModel = null;

            string path_TBD = null;
            if (!dataAccess.GetData(0, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool excludeOutdoorAir = false;
            if (!dataAccess.GetData(2, ref excludeOutdoorAir))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool excludePositiveInternalGains = true;
            if (!dataAccess.GetData(3, ref excludePositiveInternalGains))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool result = Analytical.Tas.Query.Sizing(path_TBD, analyticalModel, excludeOutdoorAir, excludePositiveInternalGains);

            dataAccess.SetData(0, result);
        }
    }
}