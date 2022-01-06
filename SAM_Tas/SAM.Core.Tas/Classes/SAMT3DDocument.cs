using System;

namespace SAM.Core.Tas
{
    public class SAMT3DDocument : IDisposable
    {
        private bool disposed = false;
        private TAS3D.T3DDocument t3DDocument;

        public SAMT3DDocument()
        {

        }

        public SAMT3DDocument(string path)
        {
            T3DDocument.Open(path);
        }

        public TAS3D.T3DDocument T3DDocument
        {
            get
            {
                if (t3DDocument == null)
                    t3DDocument = Query.T3DDocument();

                return t3DDocument;
            }
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (t3DDocument != null)
                    {
                        t3DDocument.Close();
                        Core.Modify.ReleaseCOMObject(t3DDocument);
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

        ~SAMT3DDocument() { Dispose(false); }
    }
}
