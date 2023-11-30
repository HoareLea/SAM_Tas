namespace SAM.Analytical.Tas.GenOpt
{
    public abstract class Algorithm : GenOptObject, IAlgorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public abstract AlgorithmType AlgorithmType { get; }
    }
}
