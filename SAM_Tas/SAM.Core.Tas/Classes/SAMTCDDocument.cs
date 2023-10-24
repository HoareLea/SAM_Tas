using System;
using System.IO;

namespace SAM.Core.Tas
{
    public class SAMTCDDocument : IDisposable
    {
        private bool disposed = false;
        private TCD.Document tcdDocument;
        private bool readOnly = false;

        public SAMTCDDocument()
        {
        }

        public SAMTCDDocument(string path, bool readOnly = false)
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

        public TCD.Document Document
        {
            get
            {
                if (tcdDocument == null)
                    tcdDocument = Query.TCDDocument();

                return tcdDocument;
            }
        }

        public bool Create(string path)
        {
            if(string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if(!Directory.Exists(Path.GetDirectoryName(path)))
            {
                return false;
            }

            return Document.create(path);
        }

        public bool Save()
        {
            if(readOnly)
            {
                return false;
            }

            return tcdDocument.save();
        }

        public void Close()
        {
            tcdDocument.close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (tcdDocument != null)
                    {
                        tcdDocument.close();
                        Core.Modify.ReleaseCOMObject(tcdDocument);
                        tcdDocument = null;
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

        ~SAMTCDDocument() { Dispose(false); }
    }
}
