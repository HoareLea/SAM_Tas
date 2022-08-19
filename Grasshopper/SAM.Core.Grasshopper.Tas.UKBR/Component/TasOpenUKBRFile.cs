using Grasshopper.Kernel;
using SAM.Core.Grasshopper.Tas.UKBR.Properties;
using System;

namespace SAM.Core.Grasshopper.Tas.UKBR
{
    public class TasOpenUKBRFile : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("9918d0e0-0a46-4e1f-934e-dfcf99c3dc05");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasOpenUKBRFile()
          : base("Tas.OpenUKBRFile", "Tas.OpenUKBRFile",
              "Opens a UKBRFile.",
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

            inputParamManager.AddTextParameter("_path", "_path", "The string path to a Tas UKBR file.", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("_run", "_run", "Connect a boolean toggle to run.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new GooUKBRFileParam(), "UKBRFile", "UKBRFile", "A UKBRFile", GH_ParamAccess.item);
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
            if (!dataAccess.GetData(1, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            if (!run)
                return;

            string path = null;
            if (!dataAccess.GetData(0, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if(!System.IO.File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File does not exists");
                return;
            }

            Core.Tas.UKBR.UKBRFile uKBRFile = new Core.Tas.UKBR.UKBRFile(path);
            uKBRFile.Open();

            //GH_Document.SolutionEndEventHandler endHandler = null;

            //OnPingDocument().SolutionEnd += endHandler = (sender, args) =>
            //{
            //    (sender as GH_Document).SolutionEnd -= endHandler;
            //    uKBRFile.Dispose();
            //    this.Phase = GH_SolutionPhase.Blank;
            //};

            dataAccess.SetData(0, new GooUKBRFile(uKBRFile));
            dataAccess.SetData(1, true);
        }
    }
}