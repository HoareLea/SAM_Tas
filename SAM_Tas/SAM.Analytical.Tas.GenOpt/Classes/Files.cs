using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.GenOpt
{
    [Attributes.Name("Files")]
    public class Files : GenOptEnumerable<File>
    {
        public Files()
        {

        }
        
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

        public Files(File template, File input, File log, File output, File configuration)
        {
            if (template != null)
            {
                values.Add(template);
            }

            if (input != null)
            {
                values.Add(input);
            }

            if (log != null)
            {
                values.Add(log);
            }

            if (output != null)
            {
                values.Add(output);
            }

            if (configuration != null)
            {
                values.Add(configuration);
            }
        }

        public Files(string command)
        {
            if(command != null)
            {
                values.Add(new File(FileType.Command, command));
            }
        }

        public Files(File command)
        {
            if (command != null)
            {
                values.Add(command);
            }
        }

        public File GetFile(FileType fileType)
        {
            return values.Find(x => x.FileType == fileType);
        }

        public File this[FileType fileType]
        {
            get
            {
                return GetFile(fileType);
            }
        }

        public void AddRange(FileType fileType, IEnumerable<string> names)
        {
            if(names == null || names.Count() == 0)
            {
                return;
            }

            File file = values.Find(x => x.FileType == fileType);
            if (file == null)
            {
                values.Add(new File(fileType, names));
            }
            else
            {
                file.AddRange(names);
            }
        }

        public bool Add(FileType fileType, string name)
        {
            if (string.IsNullOrEmpty( name))
            {
                return false;
            }

            File file = values.Find(x => x.FileType == fileType);
            if (file == null)
            {
                values.Add(new File(fileType, name));
                return true;
            }
            else
            {
                return file.Add(name);
            }
        }

        public bool Clear(FileType fileType)
        {
            File file = GetFile(fileType);
            if(file == null)
            {
                return false;
            }

            return file.Clear();
        }



    }
}
