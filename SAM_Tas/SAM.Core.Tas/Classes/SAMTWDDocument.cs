using System;
using System.IO;

namespace SAM.Core.Tas
{
    public class SAMTWDDocument : IDisposable
    {
        private bool disposed = false;
        private TWD.Document tWDDocument;
        private bool readOnly = false;

        public SAMTWDDocument()
        {

        }

        public SAMTWDDocument(string path, bool readOnly = false)
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

        public TWD.Document Document
        {
            get
            {
                if (tWDDocument == null)
                    tWDDocument = Query.TWDDocument();

                return tWDDocument;
            }
        }

        public bool Save()
        {
            if(readOnly)
            {
                return false;
            }

            return tWDDocument.save();
        }

        public void Close()
        {
            tWDDocument.close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (tWDDocument != null)
                    {
                        tWDDocument.close();
                        Core.Modify.ReleaseCOMObject(tWDDocument);
                        tWDDocument = null;
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

        ~SAMTWDDocument() { Dispose(false); }
    }
}
