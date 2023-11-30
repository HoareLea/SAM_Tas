namespace SAM.Analytical.Tas.GenOpt
{
    public static partial class Create
    {
        public static Files Files()
        {
            Files result = new Files();
            result.Add(FileType.Template, "Template.txt");
            result.Add(FileType.Input, "Variables.txt");
            result.Add(FileType.Log, "Error.txt");
            result.Add(FileType.Output, "Output.txt");
            result.Add(FileType.Configuration, "config.txt");
            result.Add(FileType.Command, "Command.txt");

            return result;
        }
    }
}