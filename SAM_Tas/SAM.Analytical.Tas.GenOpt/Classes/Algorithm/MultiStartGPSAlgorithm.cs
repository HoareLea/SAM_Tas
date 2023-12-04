namespace SAM.Analytical.Tas.GenOpt
{
    public class MultiStartGPSAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.GPSCoordinateSearch;

        [Attributes.Name("Seed"), Attributes.Index(1)]
        public double Seed { get; set; } = 0;

        [Attributes.Name("NumberOfInitialPoint"), Attributes.Index(2)]
        public int NumberOfInitialPoint { get; set; } = 0;

        [Attributes.Name("MeshSizeDivider"), Attributes.Index(3)]
        public double MeshSizeDivider { get; set; } = 0;

        [Attributes.Name("InitialMeshSizeExponent"), Attributes.Index(4)]
        public double InitialMeshSizeExponent { get; set; } = 0;

        [Attributes.Name("MeshSizeExponentIncrement"), Attributes.Index(5)]
        public double MeshSizeExponentIncrement { get; set; } = 0;

        [Attributes.Name("NumberOfStepReduction"), Attributes.Index(6)]
        public int NumberOfStepReduction { get; set; } = 0;
    }
}
