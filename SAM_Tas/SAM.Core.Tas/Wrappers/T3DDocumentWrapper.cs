using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Core.Tas.Wrappers
{
    public class T3DDocumentWrapper : IDisposable
    {
        private bool disposed = false;
        private 

        public T3DDocumentWrapper()
        {
            try
            {
                object aObject = Marshal.GetActiveObject("T3D.Document");

                if (aObject != null)
                {
                    aT3DDocument = aObject as TAS3D.T3DDocument;
                    ClearCOMObject(aObject);
                    aT3DDocument = null;
                }
            }
            catch
            {

            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
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
