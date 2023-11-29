namespace SAM.Analytical.Tas.GenOpt
{
    public class MeshAlgorithm : Algorithm
    {
        [Attributes.Name("StopAtError")]
        public bool StopAtError { get; set; } = false;

        [Attributes.Name("Main")]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.Mesh;
    }
}
