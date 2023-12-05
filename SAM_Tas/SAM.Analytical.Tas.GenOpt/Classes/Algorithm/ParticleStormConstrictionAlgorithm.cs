namespace SAM.Analytical.Tas.GenOpt
{
    public class ParticleStormConstrictionAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.PSOCC;

        [Attributes.Name("ConstrictionGain"), Attributes.Index(1)]
        public double ConstrictionGain { get; set; } = 0;

        [Attributes.Name("NeighborhoodTopology"), Attributes.Index(2)]
        public double NeighborhoodTopology { get; set; } = 0;

        [Attributes.Name("NeighborhoodSize"), Attributes.Index(3)]
        public double NeighborhoodSize { get; set; } = 0;

        [Attributes.Name("NumberOfParticle"), Attributes.Index(4)]
        public double NumberOfParticle { get; set; } = 0;

        [Attributes.Name("NumberOfGeneration"), Attributes.Index(5)]
        public double NumberOfGeneration { get; set; } = 0;

        [Attributes.Name("Seed"), Attributes.Index(6)]
        public double Seed { get; set; } = 0;

        [Attributes.Name("CognitiveAcceleration"), Attributes.Index(7)]
        public double CognitiveAcceleration { get; set; } = 0;

        [Attributes.Name("SocialAcceleration"), Attributes.Index(8)]
        public double SocialAcceleration { get; set; } = 0;

        [Attributes.Name("MaxVelocityGainContinuous"), Attributes.Index(9)]
        public double MaxVelocityGainContinuous { get; set; } = 0;

        [Attributes.Name("MaxVelocityDiscrete"), Attributes.Index(10)]
        public double MaxVelocityDiscrete { get; set; } = 0;
    }
}
