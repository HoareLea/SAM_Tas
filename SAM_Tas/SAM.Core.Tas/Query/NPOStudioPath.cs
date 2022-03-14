namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string NPOStudioPath()
        {
            string tasDirectory = TasDirectory();
            if(string.IsNullOrWhiteSpace(tasDirectory))
            {
                return null;
            }

            return System.IO.Path.Combine(tasDirectory, "NPOStudio.exe");
        }
    }
}