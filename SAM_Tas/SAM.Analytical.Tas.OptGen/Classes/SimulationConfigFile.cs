namespace SAM.Analytical.Tas.OptGen
{
    public class SimulationConfigFile : OptGenFile
    {
        [Attributes.Name("SimulationError")]
        public SimulationError SimulationError { get; set; }

        [Attributes.Name("IO")]
        public IO IO { get; set; }

        [Attributes.Name("SimulationStart")]
        public SimulationStart SimulationStart { get; set; }
    }
}
