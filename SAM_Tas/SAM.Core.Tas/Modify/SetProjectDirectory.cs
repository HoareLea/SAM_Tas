using Microsoft.Win32;

namespace SAM.Core.Tas
{
    public static partial class Modify
    {
        public static bool SetProjectDirectory(string directory)
        {
            if(string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            RegistryKey registryKey_CurrentProject = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\EDSL\TasManager\CurrentProject", true);
            if (registryKey_CurrentProject == null)
            {
                return false;
            }

            RegistryKey registryKey_TasData = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\EDSL\TasManager\TasData", true);
            if (registryKey_TasData == null)
            {
                return false;
            }

            registryKey_CurrentProject.SetValue("Path", directory);
            registryKey_TasData.SetValue("Path", directory);
            return true;

        }
    }
}
