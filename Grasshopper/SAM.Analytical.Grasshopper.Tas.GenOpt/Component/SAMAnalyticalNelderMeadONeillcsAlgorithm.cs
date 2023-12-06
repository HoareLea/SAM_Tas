using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class SAMAnalyticalNelderMeadONeillcsAlgorithm : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6a70e82b-d5b2-4350-89e6-b5c4ecd9bea7");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_GenOpt;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalNelderMeadONeillcsAlgorithm()
          : base("SAMAnalytical.NelderMeadONeillcsAlgorithm", "SAMAnalytical.NelderMeadONeillcsAlgorithm",
              "SAM Analytical GenOpt Nelder Mead ONeillcs Algorithm",
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

                NelderMeadONeillcsAlgorithm nelderMeadONeillcsAlgorithm = new NelderMeadONeillcsAlgorithm();

                Param_Boolean @boolean = null;
                Param_Number number = null;

                number = new Param_Number() { Name = "_accuracy_", NickName = "_accuracy_", Description = "Accuracy", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(nelderMeadONeillcsAlgorithm.Accuracy);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_stepSizeFactor_", NickName = "_stepSizeFactor_", Description = "Step Size Factor", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(nelderMeadONeillcsAlgorithm.StepSizeFactor);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_blockRestartCheck_", NickName = "_blockRestartCheck_", Description = "Block Restart Check", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(nelderMeadONeillcsAlgorithm.BlockRestartCheck);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                @boolean = new Param_Boolean() { Name = "_modifyStoppingCriterion_", NickName = "_modifyStoppingCriterion_", Description = "Modify Stopping Criterion", Optional = true, Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(nelderMeadONeillcsAlgorithm.ModifyStoppingCriterion);
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
                result.Add(new GH_SAMParam(new GooAlgorithmParam() { Name = "algorithm", NickName = "algorithm", Description = "Algorithm", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            double value;
            bool @bool;

            NelderMeadONeillcsAlgorithm nelderMeadONeillcsAlgorithm = new NelderMeadONeillcsAlgorithm();

            value = double.NaN;
            index = Params.IndexOfInputParam("_accuracy_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                nelderMeadONeillcsAlgorithm.Accuracy = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_stepSizeFactor_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                nelderMeadONeillcsAlgorithm.StepSizeFactor = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_blockRestartCheck_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                nelderMeadONeillcsAlgorithm.BlockRestartCheck = value;
            }

            @bool = false;
            index = Params.IndexOfInputParam("_modifyStoppingCriterion_");
            if (index != -1 && dataAccess.GetData(index, ref @bool))
            {
                nelderMeadONeillcsAlgorithm.ModifyStoppingCriterion = @bool;
            }

            index = Params.IndexOfOutputParam("algorithm");
            if (index != -1)
            {
                dataAccess.SetData(index, nelderMeadONeillcsAlgorithm);
            }
        }
    }
}