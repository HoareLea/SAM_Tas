namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("Files")]
    public class Files : OptGenEnumerable<File>
    {
        public Files(string template, string input, string log, string output, string configuration)
        {
            if (template != null)
            {
                values.Add(new File(FileType.Template, template));
            }

            if (input != null)
            {
                values.Add(new File(FileType.Input, input));
            }

            if (log != null)
            {
                values.Add(new File(FileType.Log, log));
            }

            if (output != null)
            {
                values.Add(new File(FileType.Output, output));
            }

            if (configuration != null)
            {
                values.Add(new File(FileType.Configuration, configuration));
            }
        }

        public Files(string command)
        {
            if(command != null)
            {
                values.Add(new File(FileType.Command, command));
            }
        }
    }
}
