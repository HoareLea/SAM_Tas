using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Core.Tas
{
    public static class ObjectUtils
    {
        public static void ClearCOMObject(object comObject)
        {
            if (comObject == null) return;
            int intrefcount = 0;
            do
            {
                intrefcount = Marshal.FinalReleaseComObject(comObject);
            } while (intrefcount > 0);
            comObject = null;
        }
    }
}
