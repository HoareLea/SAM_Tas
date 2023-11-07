using System;
using System.Xml.Linq;
using System.IO.Compression;

namespace SAM.Core.Tas.UKBR
{
    public class UKBRFile : IDisposable
    {
        private bool disposed = false;
        
        private string path = null;
        private XDocument xDocument = null;

        public UKBRFile(string path)
        {
            this.path = path;
        }

        public UKBRFile(XDocument xDocument)
        {
            this.xDocument = xDocument;
        }

        public string Path
        {
            get
            {
                return path;
            }

            set
            {
                path = value;
            }
        }

        public bool Open()
        {
            return GetData();
        }

        public void Close(bool save = true)
        {
            if (save)
                SaveAs(path);

            xDocument = null;
        }

        public bool SaveAs(string path)
        {
            if(xDocument == null || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            
            string temporaryDirectory = GetFileTemporaryDirectory();
            if(!global::System.IO.Directory.Exists(temporaryDirectory))
            {
                global::System.IO.Directory.CreateDirectory(temporaryDirectory);
            }
            
            string temporaryFilePath = GetTemporaryFilePath(temporaryDirectory);
            xDocument.Save(temporaryFilePath);
            if (global::System.IO.File.Exists(path))
            {
                global::System.IO.File.Delete(path);
            }

            
            ZipFile.CreateFromDirectory(temporaryDirectory, path);

            if (global::System.IO.Directory.Exists(temporaryDirectory))
            {
                global::System.IO.Directory.Delete(temporaryDirectory, true);
            }

            return true;
        }

        private bool GetData()
        {
            if(string.IsNullOrWhiteSpace(path) || !global::System.IO.File.Exists(path))
            {
                return false;
            }
            
            string temporaryDirectory = GetFileTemporaryDirectory();

            string temporaryFilePath = GetTemporaryFilePath(temporaryDirectory);

            ZipFile.ExtractToDirectory(path, temporaryDirectory);
            xDocument = XDocument.Load(temporaryFilePath);

            if (global::System.IO.Directory.Exists(temporaryDirectory))
            {
                global::System.IO.Directory.Delete(temporaryDirectory, true);
            }

            return true;
        }

        private void ClearData()
        {
            xDocument = null;
        }

        private string GetFileTemporaryDirectory()
        {
            return global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        private string GetTemporaryFilePath(string fileTemporaryDirectory)
        {
            return global::System.IO.Path.Combine(fileTemporaryDirectory, "database");
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    ClearData();
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
        }

        public UKBRData UKBRData
        {
            get
            {
                if(xDocument == null)
                {
                    return null;
                }

                return new UKBRData(xDocument);
            }
        }

        ~UKBRFile() { Dispose(false); }
    }
}
