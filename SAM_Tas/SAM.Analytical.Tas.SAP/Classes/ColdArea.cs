using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class ColdArea : GuidCollection, INamedSAP
    {
        private string name;

        public ColdArea(string name, IEnumerable<Guid> guids)
        {
            this.name = name;
            if(guids != null)
            {
                foreach(Guid guid in guids)
                {
                    this.Add(guid);
                }
            }
        }

        public ColdArea(string name)
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

        public override List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add(string.Format("COLDAREA = {0}", name == null ? string.Empty : name));
            result.AddRange(base.ToStrings());
            return result;
        }
    }
}
