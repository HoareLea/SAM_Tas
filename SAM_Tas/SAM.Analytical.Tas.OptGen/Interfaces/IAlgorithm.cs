namespace SAM.Analytical.Tas.OptGen
{
    public interface IAlgorithm : IOptGenObject
    {
        AlgorithmType AlgorithmType { get; }
    }
}
