namespace SAM.Analytical.Tas.GenOpt
{
    public class ParametricAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.Parametric;

        [Attributes.Name("StopAtError"), Attributes.Index(1)]
        public bool StopAtError { get; set; } = false;
    }
}
