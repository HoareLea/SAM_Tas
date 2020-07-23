using Grasshopper.Kernel;
using Grasshopper;

using SAM.Core.Grasshopper.Tas.Properties;

namespace SAM.Core.Grasshopper.Tas
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