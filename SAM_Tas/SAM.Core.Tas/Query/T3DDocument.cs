using System.Runtime.InteropServices;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TAS3D.T3DDocument T3DDocument()
        {
            TAS3D.T3DDocument t3DDocument = null;

            try
            {
                object aObject = Marshal.GetActiveObject("T3D.Document");

                if (aObject != null)
                {
                    t3DDocument = aObject as TAS3D.T3DDocument;
                    Core.Modify.ReleaseCOMObject(aObject);
                    t3DDocument = null;
                }
            }
            catch
            {

            }

            t3DDocument = new TAS3D.T3DDocument();

            return t3DDocument;
        }
    }
}