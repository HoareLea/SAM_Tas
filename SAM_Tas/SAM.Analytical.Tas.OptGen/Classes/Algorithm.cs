namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("Algorithm")]
    public class Algorithm : OptGenObject
    {
        [Attributes.Name("AbsDiffFunction")]
        public double AbsDiffFunction { get; set; } = 0.1;

        [Attributes.Name("Main")]
        public AlgorithmType AlgorithmType { get; set; } = AlgorithmType.GoldenSection;
    }
}
