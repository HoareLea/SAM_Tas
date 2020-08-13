﻿using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasOpenT3DDocument : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("520dbeae-e091-43f4-925f-851a6d2a2592");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasOpenT3DDocument()
          : base("Tas.OpenT3DDocument", "Tas.OpenT3DDocument",
              "Open T3D Document",
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

            inputParamManager.AddTextParameter("_path_T3D", "_path_T3D", "string path to Tas T3D file", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("run_", "run_", "Connect Bool Toggle to run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new GooSAMT3DDocumentParam(), "T3DDocument", "T3DDocument", "T3DDocument", GH_ParamAccess.item);
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
            if (!dataAccess.GetData(1, ref run))
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

            Core.Tas.SAMT3DDocument sAMT3DDocument = new Core.Tas.SAMT3DDocument(path_T3D);

            GH_Document.SolutionEndEventHandler endHandler = null;

            OnPingDocument().SolutionEnd += endHandler = (sender, args) =>
            {
                (sender as GH_Document).SolutionEnd -= endHandler;
                sAMT3DDocument.Dispose();
            };

            dataAccess.SetData(0, new GooSAMT3DDocument(sAMT3DDocument));
            dataAccess.SetData(1, true);
        }
    }
}