using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class GuidCollection : HashSet<Guid>, ISAP
    {
        public virtual List<string> ToStrings()
        {
            List<string> result = new List<string>();
            foreach (Guid guid in this)
            {
                result.Add("ZONEGUID = {" + guid.ToString().ToUpper() + "}");
            }
            return result;
        }

        public List<Guid> Guids
        {
            get
            {
                return new List<Guid>(this);
            }
        }
    }
}
