namespace SAM.Analytical.Tas.TM59
{
    public static partial class Modify
    {
        public static bool TryCreatePath(string path_TBD, out string path_TM59)
        {
            path_TM59 = null;

            if(string.IsNullOrWhiteSpace(path_TBD))
            {
                return false;
            }

            string directory_TM59 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path_TBD), "Report XMLs");
            if (!System.IO.Directory.Exists(directory_TM59))
            {
                System.IO.Directory.CreateDirectory(directory_TM59);
            }

            path_TM59 = System.IO.Path.Combine(directory_TM59, System.IO.Path.GetFileNameWithoutExtension(path_TBD) + "DomOv.xml");
            return true;
        }
    }
}
