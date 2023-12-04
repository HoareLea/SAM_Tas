using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class SAMAnalyticalGenOpt : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("5259b075-7da6-4d20-8364-67225c43dc4c");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_GenOpt;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalGenOpt()
          : base("SAMAnalytical.GenOpt", "SAMAnalytical.GenOpt",
              "SAM Analytical GenOpt",
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

                GooParameterParam parameters = new GooParameterParam() { Name = "_parameters", NickName = "_parameters", Description = "Parameter", Access = GH_ParamAccess.list};
                result.Add(new GH_SAMParam(parameters, ParamVisibility.Binding));

                GooObjectiveParam objectives = new GooObjectiveParam() { Name = "_objectives", NickName = "_objectives", Description = "Objectives", Access = GH_ParamAccess.list};
                result.Add(new GH_SAMParam(objectives, ParamVisibility.Binding));

                GooAlgorithmParam algorithm = new GooAlgorithmParam() { Name = "_algorithm_", NickName = "_algorithm_", Description = "Algorithm_", Access = GH_ParamAccess.item, Optional = true };
                algorithm.SetPersistentData(new GoldenSectionAlgorithm());
                result.Add(new GH_SAMParam(algorithm, ParamVisibility.Binding));

                Param_Boolean  @boolean = new Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
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

            index = Params.IndexOfInputParam("_run");

            bool run = false;
            if (index == -1 || !dataAccess.GetData(index, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if (!run)
            {
                return;
            }

            string path = null;
            index = Params.IndexOfInputParam("_scriptPath");
            if (index == -1 || !dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Objective> objectives = new List<Objective>();
            index = Params.IndexOfInputParam("_objectives");
            if (index == -1 || !dataAccess.GetDataList(index, objectives) || objectives == null || objectives.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<IParameter> parameters = new List<IParameter>();
            index = Params.IndexOfInputParam("_parameters");
            if (index == -1 || !dataAccess.GetDataList(index, parameters) || parameters == null || parameters.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Algorithm algorithm = null;
            index = Params.IndexOfInputParam("_algorithm_");
            if (index == -1 || !dataAccess.GetData(index, ref algorithm) || algorithm == null)
            {
                algorithm = new GoldenSectionAlgorithm();
            }

            GenOptDocument genOptDocument = new GenOptDocument(System.IO.Path.GetDirectoryName(path));
            genOptDocument.Algorithm = algorithm;
            genOptDocument.AddScript(System.IO.File.ReadAllText(path));
            objectives?.ForEach(x => genOptDocument.AddObjective(x));
            parameters?.ForEach(x => genOptDocument.AddParameter(x));

            genOptDocument.Run();

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }
    }
}