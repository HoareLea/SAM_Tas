namespace SAM.Analytical.Tas.OptGen
{
    public abstract class Algorithm : OptGenObject, IAlgorithm
    {
        [Attributes.Name("Main")]
        public abstract AlgorithmType AlgorithmType { get; }
    }
}
