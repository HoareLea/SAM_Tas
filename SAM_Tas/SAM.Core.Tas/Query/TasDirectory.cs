using Microsoft.Win32;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static string TasDirectory()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\EDSL\TasManager\TasFiles", "Path", null) as string;
        }
    }
}