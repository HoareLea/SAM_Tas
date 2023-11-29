namespace SAM.Analytical.Tas.GenOpt
{
    public interface IAlgorithm : IGenOptObject
    {
        AlgorithmType AlgorithmType { get; }
    }
}
