namespace SAM.Analytical.Tas.GenOpt
{
    public class GPSHookeJeevesAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.GPSHookeJeeves;

        [Attributes.Name("MeshSizeDivider"), Attributes.Index(1)]
        public double MeshSizeDivider { get; set; } = 2;

        [Attributes.Name("InitialMeshSizeExponent"), Attributes.Index(2)]
        public double InitialMeshSizeExponent { get; set; } = 0;

        [Attributes.Name("MeshSizeExponentIncrement"), Attributes.Index(3)]
        public double MeshSizeExponentIncrement { get; set; } = 1;

        [Attributes.Name("NumberOfStepReduction"), Attributes.Index(4)]
        public double NumberOfStepReduction { get; set; } = 4;
    }
}
