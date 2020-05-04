using System.Runtime.InteropServices;

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
