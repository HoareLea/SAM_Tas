namespace SAM.Analytical.Tas.GenOpt
{
    public abstract class Algorithm : GenOptObject, IAlgorithm
    {
        [Attributes.Name("Main")]
        public abstract AlgorithmType AlgorithmType { get; }
    }
}
