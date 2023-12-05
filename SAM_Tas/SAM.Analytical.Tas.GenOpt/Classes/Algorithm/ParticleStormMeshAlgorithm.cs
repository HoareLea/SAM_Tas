namespace SAM.Analytical.Tas.GenOpt
{
    public class ParticleStormMeshAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.PSOCCMesh;

        [Attributes.Name("ConstrictionGain"), Attributes.Index(1)]
        public double ConstrictionGain { get; set; } = 0;

        [Attributes.Name("MeshSizeDivider"), Attributes.Index(2)]
        public double MeshSizeDivider { get; set; } = 0;

        [Attributes.Name("InitialMeshSizeExponent"), Attributes.Index(3)]
        public double InitialMeshSizeExponent { get; set; } = 0;

        [Attributes.Name("NeighborhoodTopology"), Attributes.Index(4)]
        public double NeighborhoodTopology { get; set; } = 0;

        [Attributes.Name("NeighborhoodSize"), Attributes.Index(5)]
        public double NeighborhoodSize { get; set; } = 0;

        [Attributes.Name("NumberOfParticle"), Attributes.Index(6)]
        public double NumberOfParticle { get; set; } = 0;

        [Attributes.Name("NumberOfGeneration"), Attributes.Index(7)]
        public double NumberOfGeneration { get; set; } = 0;

        [Attributes.Name("Seed"), Attributes.Index(8)]
        public double Seed { get; set; } = 0;

        [Attributes.Name("CognitiveAcceleration"), Attributes.Index(9)]
        public double CognitiveAcceleration { get; set; } = 0;

        [Attributes.Name("SocialAcceleration"), Attributes.Index(10)]
        public double SocialAcceleration { get; set; } = 0;

        [Attributes.Name("MaxVelocityGainContinuous"), Attributes.Index(11)]
        public double MaxVelocityGainContinuous { get; set; } = 0;

        [Attributes.Name("MaxVelocityDiscrete"), Attributes.Index(12)]
        public double MaxVelocityDiscrete { get; set; } = 0;
    }
}
