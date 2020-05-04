using System;
using System.Runtime.InteropServices;

namespace SAM.Core.Tas
{
    public class SAMT3DDocument : IDisposable
    {
        private bool disposed = false;
        private TAS3D.T3DDocument t3DDocument;

        public SAMT3DDocument()
        {
            t3DDocument = GetT3DDocument();
        }

        public SAMT3DDocument(string path)
        {
            t3DDocument = GetT3DDocument();
            t3DDocument.Open(path);
        }

        public bool ImportgbXML(string path, bool @override, bool fixNormals, bool zonesFromSpaces)
        {
            int overrideInt = 0;
            if (@override)
                overrideInt = 1;

            int fixNormalsInt = 0;
            if (fixNormals)
                fixNormalsInt = 1;

            int zonesFromSpacesInt = 0;
            if (zonesFromSpaces)
                zonesFromSpacesInt = 1;

            return t3DDocument.ImportGBXML(path, overrideInt, fixNormalsInt, zonesFromSpacesInt);
        }

        public bool Save(string path = null)
        {
            if (path == null)
                path = t3DDocument.filePath;

            return t3DDocument.Save(path);
        }

        public void Close()
        {
            t3DDocument.Close();
        }

        public static TAS3D.T3DDocument GetT3DDocument()
        {
            TAS3D.T3DDocument t3DDocument = null;

            try
            {
                object aObject = Marshal.GetActiveObject("T3D.Document");

                if (aObject != null)
                {
                    t3DDocument = aObject as TAS3D.T3DDocument;
                    ObjectUtils.ClearCOMObject(aObject);
                    t3DDocument = null;
                }
            }
            catch
            {

            }

            t3DDocument = new TAS3D.T3DDocument();

            return t3DDocument;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (t3DDocument != null)
                    {
                        ObjectUtils.ClearCOMObject(t3DDocument);
                        t3DDocument = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
    }
}
