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

            return string.Format("cmd /c \\\"start  /WAIT /MIN \\\"\\\" \\\"{0}\\\" \\\"{1}\\\" ", Query.TasGenOptExecutePath(), directory);

        }
    }
}