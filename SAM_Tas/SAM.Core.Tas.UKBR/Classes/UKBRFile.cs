using System;
using System.Xml.Linq;
using System.Linq;
using System.IO.Compression;

namespace SAM.Core.Tas.UKBR
{
    public class UKBRFile : IDisposable
    {
        private bool disposed = false;
        
        private static string widnowsTemporaryDirectory = global::System.IO.Path.GetTempPath();
        private static string fileTemporaryDirectoryName = Guid.NewGuid().ToString();
        private string path = null;
        private XDocument xDocument = null;

        public UKBRFile(string path)
        {
            this.path = path;
        }

        public void Open()
        {
            GetData();
        }

        public void Close(bool save = true)
        {
            if (save)
                SaveAs(path);

            if (global::System.IO.Directory.Exists(FileTemporaryDirectory))
                global::System.IO.Directory.Delete(FileTemporaryDirectory, true);
        }

        public void SaveAs(string path)
        {
            string aTemporaryFilePath = GetTemporaryFilePath();
            xDocument.Save(aTemporaryFilePath);
            if (global::System.IO.File.Exists(path))
                global::System.IO.File.Delete(path);
            ZipFile.CreateFromDirectory(FileTemporaryDirectory, path);
        }

        private void GetData()
        {
            ClearData();
            
            ZipFile.ExtractToDirectory(path, FileTemporaryDirectory);
            xDocument = XDocument.Load(GetTemporaryFilePath());
        }

        private void ClearData()
        {
            if (global::System.IO.Directory.Exists(FileTemporaryDirectory))
                global::System.IO.Directory.Delete(FileTemporaryDirectory, true);
            
            xDocument = null;
        }

        public string FileTemporaryDirectory
        {
            get
            {
                return global::System.IO.Path.Combine(widnowsTemporaryDirectory, fileTemporaryDirectoryName);
            }
        }

        public string GetTemporaryFilePath()
        {
            if(!global::System.IO.Directory.Exists(FileTemporaryDirectory))
            {
                return null;
            }

            string[] files = global::System.IO.Directory.GetFiles(FileTemporaryDirectory);
            return files.First();
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
