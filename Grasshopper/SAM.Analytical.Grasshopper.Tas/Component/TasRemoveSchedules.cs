using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasRemoveSchedules : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ee40e528-1840-4c9c-8ecc-eed0ca0fc9f2");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasRemoveSchedules()
          : base("Tas.RemoveSchedules", "Tas.RemoveSchedules",
              "Removes Schedules from TBD file by given name sufix",
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

            inputParamManager.AddTextParameter("_path_TasTBD", "pathTasTBD", "string path to TasTBD file", GH_ParamAccess.item);
            inputParamManager.AddTextParameter("_sufix", "_sufix", "Schedule name sufix", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("_caseSensitive_", "_caseSensitive_", "Case Sensitive", GH_ParamAccess.item, false);
            inputParamManager.AddBooleanParameter("_trim_", "_trim_", "Trim", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("run_", "run_", "Connect Bool Toggle to run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddTextParameter("Names", "Names", "Names of schedules have been removed", GH_ParamAccess.list);
            outputParamManager.AddBooleanParameter("Successful", "Successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(1, false);

            bool run = false;
            if (!dataAccess.GetData(4, ref run))
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

            string sufix = null;
            if (!dataAccess.GetData(1, ref sufix))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool caseSensitive = false;
            if (!dataAccess.GetData(2, ref caseSensitive))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool trim = false;
            if (!dataAccess.GetData(3, ref trim))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<string> result = Analytical.Tas.Modify.RemoveSchedules(path_TBD, sufix, caseSensitive, trim);

            dataAccess.SetData(0, result);
            dataAccess.SetData(1, result != null);
        }
    }
}