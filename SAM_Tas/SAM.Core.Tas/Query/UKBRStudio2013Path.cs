namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string UKBRStudio2013Path()
        {
            string tasDirectory = TasDirectory();
            if(string.IsNullOrWhiteSpace(tasDirectory))
            {
                return null;
            }

            return System.IO.Path.Combine(tasDirectory, "UKBRStudio2013.exe");
        }
    }
}