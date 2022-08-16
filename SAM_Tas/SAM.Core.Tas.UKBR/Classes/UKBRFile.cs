using System;
using System.Xml.Linq;
using System.Linq;
using System.IO.Compression;

namespace SAM.Core.Tas.UKBR
{
    public class UKBRFile : IDisposable
    {
        private static string widnowsTemporaryDirectory = System.IO.Path.GetTempPath();
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

            if (System.IO.Directory.Exists(FileTemporaryDirectory))
                System.IO.Directory.Delete(FileTemporaryDirectory, true);
        }

        public void SaveAs(string path)
        {
            string aTemporaryFilePath = GetTemporaryFilePath();
            xDocument.Save(aTemporaryFilePath);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
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
            if (System.IO.Directory.Exists(FileTemporaryDirectory))
                System.IO.Directory.Delete(FileTemporaryDirectory, true);
            xDocument = null;
        }

        public string FileTemporaryDirectory
        {
            get
            {
                return System.IO.Path.Combine(widnowsTemporaryDirectory, fileTemporaryDirectoryName);
            }
        }

        public string GetTemporaryFilePath()
        {
            string[] files = System.IO.Directory.GetFiles(FileTemporaryDirectory);
            return files.First();
        }

        void IDisposable.Dispose()
        {
            ClearData();
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
    }
}
