using System;
using System.IO;

namespace SAM.Core.Tas
{
    public class SAMTBDDocument : IDisposable
    {
        private bool disposed = false;
        private TBD.TBDDocument tBDDocument;
        private bool readOnly = false;

        public SAMTBDDocument(string path, bool readOnly = false)
        {
            this.readOnly = readOnly;

            if(string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if(File.Exists(path))
            {
                FileInfo fileInfo = new FileInfo(path);

                if(Core.Query.Locked(fileInfo))
                {
                    readOnly = true;
                }
            }

            if(File.Exists(path))
            {
                if (readOnly)
                {
                    TBDDocument.openReadOnly(path);
                }
                else
                {
                    TBDDocument.open(path);
                }
            }
            else
            {
                TBDDocument.create(path);
            }


        }

        public TBD.TBDDocument TBDDocument
        {
            get
            {
                if (tBDDocument == null)
                    tBDDocument = Query.TBDDocument();

                return tBDDocument;
            }
        }

        public bool Save()
        {
            if(readOnly)
            {
                return false;
            }
            
            return tBDDocument.save() == 1;
        }

        public bool Close()
        {
            return tBDDocument.close() == 1;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (tBDDocument != null)
                    {
                        tBDDocument.close();
                        Core.Modify.ReleaseCOMObject(tBDDocument);
                        tBDDocument = null;
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

        ~SAMTBDDocument() { Dispose(false); }
    }
}
