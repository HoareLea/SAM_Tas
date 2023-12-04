namespace SAM.Analytical.Tas.GenOpt
{
    public class MeshAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.Mesh;

        [Attributes.Name("StopAtError"), Attributes.Index(1)]
        public bool StopAtError { get; set; } = false;
    }
}
