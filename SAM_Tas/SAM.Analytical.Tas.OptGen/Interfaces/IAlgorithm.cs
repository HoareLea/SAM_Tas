namespace SAM.Analytical.Tas.OptGen
{
    internal interface IAlgorithm : IOptGenObject
    {
        AlgorithmType AlgorithmType { get; }
    }
}
