namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string IDFWizardPath()
        {
            string tasDirectory = TasDirectory();
            if(string.IsNullOrWhiteSpace(tasDirectory))
            {
                return null;
            }

            return System.IO.Path.Combine(tasDirectory, "IDF Wizard.exe");
        }
    }
}