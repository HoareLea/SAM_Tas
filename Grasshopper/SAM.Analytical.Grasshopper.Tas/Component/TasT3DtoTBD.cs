using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasT3DtoTBD : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("85b9fea2-d977-49c4-8295-8f207d10e13f");

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
        public TasT3DtoTBD()
          : base("Tas.T3DtoTBD", "Tas.T3DtoTBD",
              "Exports/Converts TasT3D to TBD.",
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

            inputParamManager.AddTextParameter("_pathTasT3D", "_pathTasT3D", "The string path to a TasT3D file.", GH_ParamAccess.item);
            inputParamManager.AddTextParameter("_pathTasTBD", "_pathTasTBD", "The string path to a TasTBD file.", GH_ParamAccess.item);
            inputParamManager.AddIntegerParameter("_dayFirst_", "_dayFirst_", "The first day", GH_ParamAccess.item, 1);
            inputParamManager.AddIntegerParameter("_dayLast_", "_dayLast_", "The last day", GH_ParamAccess.item, 365);
            inputParamManager.AddIntegerParameter("_step_", "_step_", "What should the time interval (in days) be, of the solar calculations?", GH_ParamAccess.item, 15);
            inputParamManager.AddBooleanParameter("_autoAssignConstructions_", "_autoAssignConstructions_", "Should the construction be assigned automatically?", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("_removeTBD", "_removeTBD", "If True existing TBD file will be deleted before simulation", GH_ParamAccess.item, false);
            inputParamManager.AddBooleanParameter("_run", "_run", "Connect a boolean toggle to run.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddBooleanParameter("successful", "successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(0, false);

            bool run = false;
            if (!dataAccess.GetData(7, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            if (!run)
                return;

            string path_T3D = null;
            if (!dataAccess.GetData(0, ref path_T3D) || string.IsNullOrWhiteSpace(path_T3D))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TBD = null;
            if (!dataAccess.GetData(1, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            int day_First = int.MinValue;
            if (!dataAccess.GetData(2, ref day_First))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            int day_Last = int.MinValue;
            if (!dataAccess.GetData(3, ref day_Last))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            int step = int.MinValue;
            if (!dataAccess.GetData(4, ref step))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool autoAssignConstructions = true;
            if (!dataAccess.GetData(5, ref autoAssignConstructions))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool removeExisting = true;
            if (!dataAccess.GetData(6, ref removeExisting))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }


            bool result = Analytical.Tas.Convert.ToTBD(path_T3D, path_TBD, day_First, day_Last, step, autoAssignConstructions, removeExisting);

            dataAccess.SetData(0, result);
        }
    }
}