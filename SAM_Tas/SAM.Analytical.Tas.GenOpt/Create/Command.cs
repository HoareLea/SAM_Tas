namespace SAM.Analytical.Tas.GenOpt
{
    public static partial class Create
    {
        public static string Command(string directory)
        {
            if(string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            string path = Query.TasGenOptExecutePath();
            if(string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string directory_Temp = directory.Replace("\\", "\\\\");

            path = path.Replace("\\", "\\\\");

            return string.Format("cmd /c \\\"start  /WAIT /MIN \\\"\\\" \\\"{0}\\\" \\\"{1}\\\" ", path, directory_Temp);

        }
    }
}