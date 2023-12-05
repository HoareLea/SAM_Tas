namespace SAM.Analytical.Tas.GenOpt
{
    public class NelderMeadONeillcsAlgorithm : Algorithm
    {
        [Attributes.Name("Main"), Attributes.Index(0)]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.NelderMeadONeill;

        [Attributes.Name("Accuracy"), Attributes.Index(1)]
        public double Accuracy { get; set; } = 0.0;

        [Attributes.Name("StepSizeFactor"), Attributes.Index(2)]
        public double StepSizeFactor { get; set; } = 0.0;

        [Attributes.Name("BlockRestartCheck"), Attributes.Index(3)]
        public double BlockRestartCheck { get; set; } = 0.0;

        [Attributes.Name("ModifyStoppingCriterion"), Attributes.Index(4)]
        public bool ModifyStoppingCriterion { get; set; } = false;
    }
}
