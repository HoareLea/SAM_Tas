namespace SAM.Analytical.Tas.GenOpt
{
    public class GoldenSectionAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.GoldenSection;
        
        [Attributes.Name("AbsDiffFunction"), Attributes.Index(1)]
        public double AbsDiffFunction { get; set; } = 0.1;
    }
}
