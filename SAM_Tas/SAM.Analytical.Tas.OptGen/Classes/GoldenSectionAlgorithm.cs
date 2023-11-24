﻿namespace SAM.Analytical.Tas.OptGen
{
    public class GoldenSectionAlgorithm : Algorithm
    {
        [Attributes.Name("AbsDiffFunction")]
        public double AbsDiffFunction { get; set; } = 0.1;

        [Attributes.Name("Main")]
        public override AlgorithmType AlgorithmType { get; } = AlgorithmType.GoldenSection;
    }
}
