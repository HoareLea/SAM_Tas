using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Analytical.Grasshopper.Tas.OptGen.Properties;
using SAM.Analytical.Tas.OptGen;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.OptGen
{
    public class SAMAnalyticalOptGen : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("5259b075-7da6-4d20-8364-67225c43dc4c");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalOptGen()
          : base("SAMAnalytical.OptGen", "SAMAnalytical.OptGen",
              "SAM Analytical OptGen",
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

                Param_FilePath filePath = new Param_FilePath() { Name = "_scriptPath", NickName = "_scriptPath", Description = "Script path", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(filePath, ParamVisibility.Binding));

                Param_FilePath parameters = new Param_FilePath() { Name = "_parameters", NickName = "_parameters", Description = "Parameter", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(parameters, ParamVisibility.Binding));

                Param_FilePath objectives = new Param_FilePath() { Name = "_objectives", NickName = "_objectives", Description = "Objectives", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(objectives, ParamVisibility.Binding));

                Param_FilePath algorithm = new Param_FilePath() { Name = "_algorithm", NickName = "_algorithm", Description = "Algorithm", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(algorithm, ParamVisibility.Binding));

                Param_Boolean @boolean;

                @boolean = new Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
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
                result.Add(new GH_SAMParam(new Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly imported?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_Successful = Params.IndexOfOutputParam("successful");
            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, false);
            }

            int index;

            string path = null;
            index = Params.IndexOfInputParam("_scriptPath");
            if (index == -1 || !dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }



            OptGenDocument optGenDocument = new OptGenDocument(System.IO.Path.GetDirectoryName(path));
            optGenDocument.AddScript(System.IO.File.ReadAllText(path));
            optGenDocument.AddObjective("DaylightFactor");
            optGenDocument.AddObjective("Result");
            optGenDocument.AddParameter("NorthAngle", 0, 0, 360, 12);
            optGenDocument.Run();

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }
    }
}