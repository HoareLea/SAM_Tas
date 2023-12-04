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

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\EDSL\TasManager\CurrentProject", true);
            if (registryKey == null)
            {
                return false;
            }

            registryKey.SetValue("Path", directory);
            return true;

        }
    }
}
