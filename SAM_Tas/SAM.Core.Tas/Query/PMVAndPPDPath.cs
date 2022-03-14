namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string PMVAndPPDPath()
        {
            string tasDirectory = TasDirectory();
            if(string.IsNullOrWhiteSpace(tasDirectory))
            {
                return null;
            }

            return System.IO.Path.Combine(tasDirectory, "PMV PPD.exe");
        }
    }
}