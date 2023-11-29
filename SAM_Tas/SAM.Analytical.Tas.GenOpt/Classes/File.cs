using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.GenOpt
{
    public class File : GenOptObject
    {
       public FileType FileType { get; }

       private HashSet<string> names = new HashSet<string>();

        public File(FileType fileType)
        {
            FileType = fileType;
        }
        
        public File(FileType fileType, string name)
        {
            FileType = fileType;
            if(name != null)
            {
                names.Add(name);
            }
        }

        public File(FileType fileType, IEnumerable<string> names) 
            : this(fileType)
        {
            AddRange(names);
        }

        public bool Add(string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                return false;
            }

            if(names == null)
            {
                names = new HashSet<string>();
            }

            return names.Add(name);
        }

        public void AddRange(IEnumerable<string> names)
        {
            if(names == null)
            {
                return;
            }

            foreach(string name in names)
            {
                Add(name);
            }
        }

        public bool Clear()
        {
            if(names == null || names.Count == 0)
            {
                return false;
            }

            names.Clear();
            return true;
        }

        public HashSet<string> Names
        {
            get
            {
                return names == null ? null : new HashSet<string>(names);
            }
        }

        protected override string GetText()
        {
            List<string> texts = new List<string>();
            if(names != null)
            {
                for(int i =0; i < names.Count; i++)
                {
                    if (names.ElementAt(i) == null)
                    {
                        continue;
                    }

                    texts.Add(string.Format("File{0} = {1};", i + 1, names.ElementAt(i)));
                }
            }

            return string.Format("{0} {{\n{1}\n}}", FileType.ToString(), string.Join("\n", texts));
        }
    }
}
