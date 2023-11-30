namespace SAM.Analytical.Tas.GenOpt
{
    public class ParametricAlgorithm : Algorithm
    {
        [Attributes.Name("StopAtError")]
        public bool StopAtError { get; set; } = false;

        [Attributes.Name("Main")]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.Parametric;
    }
}
