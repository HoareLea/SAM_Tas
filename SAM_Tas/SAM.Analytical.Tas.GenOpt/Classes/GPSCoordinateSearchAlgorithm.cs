namespace SAM.Analytical.Tas.GenOpt
{
    public class GPSCoordinateSearchAlgorithm : Algorithm
    {
        [Attributes.Name("Seed")]
        public double Seed { get; set; } = 0;

        [Attributes.Name("NumberOfInitialPoint")]
        public int NumberOfInitialPoint { get; set; } = 0;

        [Attributes.Name("MeshSizeDivider")]
        public double MeshSizeDivider { get; set; } = 0;

        [Attributes.Name("InitialMeshSizeExponent")]
        public double InitialMeshSizeExponent { get; set; } = 0;

        [Attributes.Name("MeshSizeExponentIncrement")]
        public double MeshSizeExponentIncrement { get; set; } = 0;

        [Attributes.Name("NumberOfStepReduction")]
        public double NumberOfStepReduction { get; set; } = 0;

        [Attributes.Name("Main")]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.GPSCoordinateSearch;
    }
}
