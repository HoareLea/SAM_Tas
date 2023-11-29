using Grasshopper.Kernel;
using Grasshopper;

using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class AssemblyPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("SAM", Resources.SAM_Small);
            Instances.ComponentServer.AddCategorySymbolName("SAM", 'S');
            return GH_LoadingInstruction.Proceed;
        }
    }

}