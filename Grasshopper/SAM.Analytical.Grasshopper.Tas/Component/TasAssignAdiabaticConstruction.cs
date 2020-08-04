using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasAssignAdiabaticConstruction : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("d1f6458f-bdd0-497b-82b5-fb454d4e5326");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasAssignAdiabaticConstruction()
          : base("Tas.AssignAdiabaticConstruction", "Tas.AssignAdiabaticConstruction",
              "Assign Adiabatic Construction to Builing Element in TBD File",
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
            inputParamManager.AddTextParameter("_adiabaticConstructionName_", "_adiabaticConstructionName_", "Name of Adiabatic Construction to be assigned", GH_ParamAccess.item, "Adiabatic");
            inputParamManager.AddTextParameter("_constructionNameSufixes_", "_constructionNameSufixes_", "Sufixes for builidng Element Construction names to be replaced", GH_ParamAccess.list, new string[] { "-unzoned", "-internal" });
            inputParamManager.AddBooleanParameter("_caseSensitive_", "_caseSensitive_", "Case Sensitive", GH_ParamAccess.item, false);
            inputParamManager.AddBooleanParameter("_trim_", "_trim_", "Trim", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("run_", "run_", "Connect Bool Toggle to run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddTextParameter("Guids", "Guids", "Guids of Builidng Elements construction has been changed", GH_ParamAccess.list);
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
            if (!dataAccess.GetData(5, ref run))
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

            string constructionName_Adiabatic = null;
            if (!dataAccess.GetData(1, ref constructionName_Adiabatic) || string.IsNullOrWhiteSpace(constructionName_Adiabatic))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<string> constructionNames_Sufixes = new List<string>();
            if (!dataAccess.GetDataList(2, constructionNames_Sufixes))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool caseSensitive = false;
            if (!dataAccess.GetData(3, ref caseSensitive))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool trim = false;
            if (!dataAccess.GetData(4, ref trim))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Guid> result = Analytical.Tas.Modify.AssignAdiabaticConstruction(path_TBD, constructionName_Adiabatic, constructionNames_Sufixes, caseSensitive, trim);

            dataAccess.SetData(0, result?.ConvertAll(x => x.ToString()));
            dataAccess.SetData(1, result != null);
        }
    }
}