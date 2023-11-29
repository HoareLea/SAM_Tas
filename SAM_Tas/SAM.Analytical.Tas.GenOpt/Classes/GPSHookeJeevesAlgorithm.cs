namespace SAM.Analytical.Tas.GenOpt
{
    public class GPSHookeJeevesAlgorithm : Algorithm
    {
        [Attributes.Name("MeshSizeDivider")]
        public double MeshSizeDivider { get; set; } = 2;

        [Attributes.Name("InitialMeshSizeExponent")]
        public double InitialMeshSizeExponent { get; set; } = 0;

        [Attributes.Name("MeshSizeExponentIncrement")]
        public double MeshSizeExponentIncrement { get; set; } = 1;

        [Attributes.Name("NumberOfStepReduction")]
        public double NumberOfStepReduction { get; set; } = 4;

        [Attributes.Name("Main")]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.GPSHookeJeeves;
    }
}
