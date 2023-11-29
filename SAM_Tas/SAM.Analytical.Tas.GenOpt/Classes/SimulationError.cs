namespace SAM.Analytical.Tas.GenOpt
{
    [Attributes.Name("SimulationError")]
    public class SimulationError : GenOptObject
    {
        [Attributes.Name("ErrorMessage"), Attributes.QuotedValue()]
        public string ErrorMessage { get; set; } = "Error";
    }
}
