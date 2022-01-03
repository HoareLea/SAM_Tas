using System;

namespace SAM.Core.Tas
{
    public class SAMTSDDocument : IDisposable
    {
        private bool disposed = false;
        private TSD.TSDDocument tSDDocument;
        private bool readOnly = false;

        public SAMTSDDocument()
        {

        }

        public SAMTSDDocument(string path, bool readOnly = false)
        {
            this.readOnly = readOnly;
            
            if (readOnly)
            {
                TSDDocument.openReadOnly(path);
            }
            else
            {
                TSDDocument.open(path);
            }
        }

        public TSD.TSDDocument TSDDocument
        {
            get
            {
                if (tSDDocument == null)
                    tSDDocument = Query.TSDDocument();

                return tSDDocument;
            }
        }

        public bool Save()
        {
            if(readOnly)
            {
                return false;
            }

            return tSDDocument.save();
        }

        public void Close()
        {
            tSDDocument.close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (tSDDocument != null)
                    {
                        Core.Modify.ReleaseCOMObject(tSDDocument);
                        tSDDocument = null;
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
