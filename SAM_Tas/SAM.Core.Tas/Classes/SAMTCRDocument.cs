using System;
using System.IO;

namespace SAM.Core.Tas
{
    public class SAMTCRDocument : IDisposable
    {
        private bool disposed = false;
        private TCR.Document document;
        private bool readOnly = false;

        public SAMTCRDocument()
        {
        }

        public SAMTCRDocument(string path, bool readOnly = false)
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

        public TCR.Document Document
        {
            get
            {
                if (document == null)
                    document = Query.TCRDocument();

                return document;
            }
        }

        public bool Save()
        {
            if(readOnly)
            {
                return false;
            }

            return document.save();
        }

        public void Close()
        {
            document.close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (document != null)
                    {
                        document.close();
                        Core.Modify.ReleaseCOMObject(document);
                        document = null;
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

        ~SAMTCRDocument() { Dispose(false); }
    }
}
