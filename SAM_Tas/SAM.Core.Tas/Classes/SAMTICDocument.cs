using System;
using System.IO;

namespace SAM.Core.Tas
{
    public class SAMTICDocument : IDisposable
    {
        private bool disposed = false;
        private TIC.Document ticDocument;
        private bool readOnly = false;

        public SAMTICDocument()
        {
        }

        public SAMTICDocument(string path, bool readOnly = false)
        {
            this.readOnly = readOnly;

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (File.Exists(path))
            {
                FileInfo fileInfo = new FileInfo(path);

                if (Core.Query.Locked(fileInfo))
                {
                    readOnly = true;
                }
            }

            try
            {

                if (readOnly)
                {
                    Document.openReadOnly(path);
                }
                else
                {
                    Document.open(path);
                }
            }
            catch
            {

            }

        }

        public TIC.Document Document
        {
            get
            {
                if (ticDocument == null)
                    ticDocument = Query.TICDocument();

                return ticDocument;
            }
        }

        public bool Save()
        {
            if(readOnly)
            {
                return false;
            }

            return ticDocument.save();
        }

        public void Close()
        {
            ticDocument.close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (ticDocument != null)
                    {
                        ticDocument.close();
                        Core.Modify.ReleaseCOMObject(ticDocument);
                        ticDocument = null;
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

        ~SAMTICDocument() { Dispose(false); }
    }
}
