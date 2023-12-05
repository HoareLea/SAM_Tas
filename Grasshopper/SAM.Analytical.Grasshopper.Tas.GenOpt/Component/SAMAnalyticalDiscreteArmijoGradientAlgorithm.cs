using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class SAMAnalyticalDiscreteArmijoGradientAlgorithm : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("64fb19e6-dc89-4dc6-8e81-5408ce995d45");

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
        public SAMAnalyticalDiscreteArmijoGradientAlgorithm()
          : base("SAMAnalytical.DiscreteArmijoGradientAlgorithm", "SAMAnalytical.DiscreteArmijoGradientAlgorithm",
              "SAM Analytical GenOpt Discrete Armijo Gradient Algorithm",
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

                Param_Number number = null;

                DiscreteArmijoGradientAlgorithm discreteArmijoGradientAlgorithm = new DiscreteArmijoGradientAlgorithm();

                number = new Param_Number() { Name = "_alpha_", NickName = "_alpha_", Description = "Alpha", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.Alpha);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_beta_", NickName = "_beta_", Description = "Beta", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.Beta);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_gamma_", NickName = "_gamma_", Description = "Gamma", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.Gamma);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_k0_", NickName = "_k0_", Description = "K0", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.K0);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_kStar_", NickName = "_kStar_", Description = "KStar", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.KStar);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_lMax_", NickName = "_lMax_", Description = "LMax", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.LMax);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_kappa_", NickName = "_kappa_", Description = "Kappa", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.Kappa);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_epsilonM_", NickName = "_epsilonM_", Description = "EpsilonM", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.EpsilonM);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_epsilonX_", NickName = "_epsilonX_", Description = "EpsilonX", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(discreteArmijoGradientAlgorithm.EpsilonX);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

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

            DiscreteArmijoGradientAlgorithm discreteArmijoGradientAlgorithm = new DiscreteArmijoGradientAlgorithm();

            value = double.NaN;
            index = Params.IndexOfInputParam("_alpha_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.Alpha = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_beta_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.Beta = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_gamma_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.Gamma = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_k0_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.K0 = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_kStar_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.KStar = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_lMax_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.LMax = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_kappa_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.Kappa = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_epsilonM_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.EpsilonM = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_epsilonX_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                discreteArmijoGradientAlgorithm.EpsilonX = value;
            }

            index = Params.IndexOfOutputParam("algorithm");
            if (index != -1)
            {
                dataAccess.SetData(index, discreteArmijoGradientAlgorithm);
            }
        }
    }
}