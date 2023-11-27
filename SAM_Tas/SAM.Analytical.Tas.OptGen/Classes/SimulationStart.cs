namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("SimulationStart")]
    public class SimulationStart : OptGenObject
    {
        [Attributes.Name("Command"), Attributes.QuotedValue()]
        public string Command { get; set; } = "cmd /c \\\"start  /WAIT /MIN \\\"\\\" \\\"C:\\\\Program Files\\\\Environmental Design Solutions Ltd\\\\Tas\\\\TasGenOpt\\\\TasGenExecute.exe\\\" \\\"C:\\\\Users\\\\DengusiakM\\\\Desktop\\\\SAM\\\\2023-11-21_GenOpt1\\\" ";

        [Attributes.Name("WriteInputFileExtension")]
        public bool WriteInputFileExtension { get; set; } = true;
    }
}
