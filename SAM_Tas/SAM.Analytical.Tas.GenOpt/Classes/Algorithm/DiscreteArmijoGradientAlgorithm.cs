namespace SAM.Analytical.Tas.GenOpt
{
    public class DiscreteArmijoGradientAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.DiscreteArmijoGradient;

        [Attributes.Name("Alpha"), Attributes.Index(1)]
        public double Alpha { get; set; } = 0;

        [Attributes.Name("Beta"), Attributes.Index(2)]
        public double Beta { get; set; } = 0;

        [Attributes.Name("Gamma"), Attributes.Index(3)]
        public double Gamma { get; set; } = 0;

        [Attributes.Name("K0"), Attributes.Index(4)]
        public double K0 { get; set; } = 0;

        [Attributes.Name("KStar"), Attributes.Index(5)]
        public double KStar { get; set; } = 0;

        [Attributes.Name("LMax"), Attributes.Index(6)]
        public double LMax { get; set; } = 0;

        [Attributes.Name("Kappa"), Attributes.Index(7)]
        public double Kappa { get; set; } = 0;

        [Attributes.Name("EpsilonM"), Attributes.Index(8)]
        public double EpsilonM { get; set; } = 0;

        [Attributes.Name("EpsilonX"), Attributes.Index(9)]
        public double EpsilonX { get; set; } = 0;
    }
}
