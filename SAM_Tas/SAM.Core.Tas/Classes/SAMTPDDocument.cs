using System;

namespace SAM.Core.Tas
{
    public class SAMTPDDocument : IDisposable
    {
        private bool disposed = false;
        private TPD.TPDDoc tPDDocument;

        public SAMTPDDocument()
        {

        }

        public SAMTPDDocument(string path, bool readOnly = false)
        {
            if(System.IO.File.Exists(path))
            {
                if(readOnly)
                {
                    TPDDocument.OpenReadOnly(path);
                }
                else
                {
                    TPDDocument.Open(path);
                }
            }
            else
            {
                TPDDocument.Create(path);
            }
            
            
        }

        public TPD.TPDDoc TPDDocument
        {
            get
            {
                if (tPDDocument == null)
                    tPDDocument = new TPD.TPDDoc();

                return tPDDocument;
            }
        }

        public void Close()
        {
            tPDDocument.Close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (tPDDocument != null)
                    {
                        tPDDocument.Close();
                        Core.Modify.ReleaseCOMObject(tPDDocument);
                        tPDDocument = null;
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

        ~SAMTPDDocument() { Dispose(false); }
    }
}
