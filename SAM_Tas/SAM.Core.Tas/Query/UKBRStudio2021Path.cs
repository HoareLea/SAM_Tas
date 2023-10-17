namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string UKBRStudio2021Path()
        {
            string tasDirectory = TasDirectory();
            if(string.IsNullOrWhiteSpace(tasDirectory))
            {
                return null;
            }

            return System.IO.Path.Combine(tasDirectory, "UKBRStudio2021.exe");
        }
    }
}