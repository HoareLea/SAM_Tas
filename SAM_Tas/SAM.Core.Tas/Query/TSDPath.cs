namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string TSDPath()
        {
            string tasDirectory = TasDirectory();
            if(string.IsNullOrWhiteSpace(tasDirectory))
            {
                return null;
            }

            return System.IO.Path.Combine(tasDirectory, "TSD.exe");
        }
    }
}