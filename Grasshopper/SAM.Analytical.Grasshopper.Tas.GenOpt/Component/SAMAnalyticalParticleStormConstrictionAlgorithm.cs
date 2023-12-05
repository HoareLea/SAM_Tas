using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class SAMAnalyticalParticleStormConstrictionAlgorithm : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("4531c9e1-0654-4b40-8cab-12ecc50ee65d");

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
        public SAMAnalyticalParticleStormConstrictionAlgorithm()
          : base("SAMAnalytical.ParticleStormConstrictionAlgorithm", "SAMAnalytical.ParticleStormConstrictionAlgorithm",
              "SAM Analytical GenOpt Particle Storm Constriction Algorithm",
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

                ParticleStormConstrictionAlgorithm particleStormConstrictionAlgorithm = new ParticleStormConstrictionAlgorithm();

                number = new Param_Number() { Name = "_constrictionGain_", NickName = "_constrictionGain_", Description = "Constriction Gain", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.ConstrictionGain);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_neighborhoodTopology_", NickName = "_neighborhoodTopology_", Description = "Neighborhood Topology", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.NeighborhoodTopology);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_neighborhoodSize_", NickName = "_neighborhoodSize_", Description = "Neighborhood Size", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.NeighborhoodSize);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_numberOfParticle_", NickName = "_numberOfParticle_", Description = "Number Of Particle", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.NumberOfParticle);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_numberOfGeneration_", NickName = "_numberOfGeneration_", Description = "Number Of Generation", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.NumberOfGeneration);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_seed_", NickName = "_seed_", Description = "Seed", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.Seed);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_cognitiveAcceleration_", NickName = "_cognitiveAcceleration_", Description = "Cognitive Acceleration", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.CognitiveAcceleration);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_socialAcceleration_", NickName = "_socialAcceleration_", Description = "Social Acceleration", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.SocialAcceleration);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_maxVelocityGainContinuous_", NickName = "_maxVelocityGainContinuous_", Description = "Max Velocity Gain Continuous", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.MaxVelocityGainContinuous);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_maxVelocityDiscrete_", NickName = "_maxVelocityDiscrete_", Description = "Max Velocity Discrete", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(particleStormConstrictionAlgorithm.MaxVelocityDiscrete);
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

            ParticleStormConstrictionAlgorithm particleStormConstrictionAlgorithm = new ParticleStormConstrictionAlgorithm();

            value = double.NaN;
            index = Params.IndexOfInputParam("_constrictionGain_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.ConstrictionGain = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_neighborhoodTopology_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.NeighborhoodTopology = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_neighborhoodSize_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.NeighborhoodSize = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_numberOfParticle_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.NumberOfParticle = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_numberOfGeneration_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.NumberOfGeneration = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_seed_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.Seed = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_cognitiveAcceleration_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.CognitiveAcceleration = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_socialAcceleration_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.SocialAcceleration = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_maxVelocityGainContinuous_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.MaxVelocityGainContinuous = value;
            }

            value = double.NaN;
            index = Params.IndexOfInputParam("_maxVelocityDiscrete_");
            if (index != -1 && dataAccess.GetData(index, ref value) && !double.IsNaN(value))
            {
                particleStormConstrictionAlgorithm.MaxVelocityDiscrete = value;
            }

            index = Params.IndexOfOutputParam("algorithm");
            if (index != -1)
            {
                dataAccess.SetData(index, particleStormConstrictionAlgorithm);
            }
        }
    }
}