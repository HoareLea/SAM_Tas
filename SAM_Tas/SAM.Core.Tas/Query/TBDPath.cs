namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string TBDPath()
        {
            string tasDirectory = TasDirectory();
            if(string.IsNullOrWhiteSpace(tasDirectory))
            {
                return null;
            }

            return System.IO.Path.Combine(tasDirectory, "TBD.exe");
        }
    }
}