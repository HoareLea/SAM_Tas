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
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasRemoveSchedules()
          : base("Tas.RemoveSchedules", "Tas.RemoveSchedules",
              "Removes the schedules from a TBD file by the given name suffix.",
              "SAM WIP", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            //int aIndex = -1;
            //Param_Boolean booleanParameter = null;

            inputParamManager.AddTextParameter("_pathTasTBD", "_pathTasTBD", "The string path of a TasTBD file.", GH_ParamAccess.item);
            inputParamManager.AddTextParameter("_suffix", "_suffix", "The schedule name suffix.", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("_caseSensitive_", "_caseSensitive_", "Should the capitalsation be considered?", GH_ParamAccess.item, false);
            inputParamManager.AddBooleanParameter("_trim_", "_trim_", "Do you want to trim the TasTBD file?", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("_run", "_run", "Connect a boolean Toggle to run.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddTextParameter("names", "names", "The names of the schedules that have been removed.", GH_ParamAccess.list);
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