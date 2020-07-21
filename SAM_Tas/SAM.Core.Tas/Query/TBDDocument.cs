using System.Runtime.InteropServices;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TBD.TBDDocument TBDDocument()
        {
            TBD.TBDDocument tBDDocument = null;

            try
            {
                object aObject = Marshal.GetActiveObject("Document");

                if (aObject != null)
                {
                    tBDDocument = aObject as TBD.TBDDocument;
                    Modify.ReleaseCOMObject(aObject);
                    tBDDocument = null;
                }
            }
            catch
            {

            }

            tBDDocument = new TBD.TBDDocument();

            return tBDDocument;
        }
    }
}