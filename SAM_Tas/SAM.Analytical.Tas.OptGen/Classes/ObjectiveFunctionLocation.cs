using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("ObjectiveFunctionLocation")]
    public class ObjectiveFunctionLocation : OptGenObject
    {
        private List<string> names = new List<string>();
        private List<string> delimiters = new List<string>();


        public ObjectiveFunctionLocation()
        {

        }

        public ObjectiveFunctionLocation(IEnumerable<string> names, IEnumerable<string>delimiters)
        {
            if(names != null && delimiters != null)
            {
                int count = System.Math.Min(names.Count(), delimiters.Count());
                for(int i = 0; i < count; i++)
                {
                    Add(names.ElementAt(i), delimiters.ElementAt(i));
                }
            }
        }

        public ObjectiveFunctionLocation(IEnumerable<string> names)
        {
            if(names != null)
            {
                foreach(string name in names)
                {
                    Add(name);
                }
            }
        }

        public bool Add(string name, string delimiter)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(delimiter))
            {
                return false;
            }

            int index = names.IndexOf(name);
            if(index != -1)
            {
                delimiters[index] = delimiter;
                return true;
            }

            names.Add(name);
            delimiters.Add(delimiter);
            return true;
        }

        public bool Add(string name)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return Add(name, string.Format("{0}::", name));
        }

        protected override string GetText()
        {
            if(names == null || delimiters == null)
            {
                return null;
            }

            int count = Math.Min(names.Count, delimiters.Count);

            List<string> texts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string name = names[i];
                if(string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string delimiter = delimiters[i];
                if (string.IsNullOrWhiteSpace(delimiter))
                {
                    continue;
                }

                texts.Add(string.Format("Name{0} = {1};", i + 1, name));
                texts.Add(string.Format("Delimiter{0} = \"{1}\";", i + 1, delimiter));
            }

            return string.Join("\n", texts);
        }
    }
}
