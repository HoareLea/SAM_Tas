namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string TAS3DPath()
        {
            string tasDirectory = TasDirectory();
            if(string.IsNullOrWhiteSpace(tasDirectory))
            {
                return null;
            }

            return System.IO.Path.Combine(tasDirectory, "TAS3D.exe");
        }
    }
}