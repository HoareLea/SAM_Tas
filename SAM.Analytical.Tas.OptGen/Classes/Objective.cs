using SAM.Analytical.Tas.OptGen.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Analytical.Tas.OptGen
{
    public class Objective : IOptGenObject
    {
        [Attributes.Name("Name")]
        public string Name { get; set; }

        [Attributes.Name("Delimiter")]
        public string Delimiter { get; set; }
    }
}
