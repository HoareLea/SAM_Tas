using System.Runtime.InteropServices;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TSD.TSDDocument TSDDocument()
        {
            TSD.TSDDocument tSDDocument = null;

            try
            {
                object aObject = Marshal.GetActiveObject("Document");

                if (aObject != null)
                {
                    tSDDocument = aObject as TSD.TSDDocument;
                    Core.Modify.ReleaseCOMObject(aObject);
                    tSDDocument = null;
                }
            }
            catch
            {

            }

            tSDDocument = new TSD.TSDDocument();

            return tSDDocument;
        }
    }
}