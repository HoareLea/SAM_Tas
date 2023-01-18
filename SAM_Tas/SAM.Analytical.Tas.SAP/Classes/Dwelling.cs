using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class Dwelling : GuidCollection, INamedSAP
    {
        private string name;

        public Dwelling(string name, IEnumerable<Guid> guids)
        {
            this.name = name;
            if(guids != null)
            {
                foreach(Guid guid in guids)
                {
                    Add(guid);
                }
            }
        }

        public Dwelling(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add(string.Format("DWELLING = {0}", name == null ? string.Empty : name));
            result.AddRange(base.ToStrings());
            return result;
        }
    }
}
