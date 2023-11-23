using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{
    public class File : OptGenObject
    {
       public FileType FileType { get; set; }

       public List<string> Names { get; set; } = new List<string>();

        public File(FileType fileType, string name)
        {
            FileType = fileType;
            if(name != null)
            {
                Names.Add(name);
            }
        }

        protected override string GetText()
        {
            List<string> texts = new List<string>();
            if(Names != null)
            {
                for(int i =0; i < Names.Count; i++)
                {
                    if (Names[i] == null)
                    {
                        continue;
                    }

                    texts.Add(string.Format("File{0} = {1};", i + 1, Names[i]));
                }
            }

            return string.Format("{0} {\n{1}\n}", FileType.ToString(), string.Join("\n", texts));
        }
    }
}
