using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public static partial class Convert
    {
        public static bool ToFile(this SAPData sAPData, string path)
        {
            if(sAPData == null || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            List<string> strings = sAPData.ToStrings();
            if(strings == null)
            {
                return false;
            }

            System.IO.File.WriteAllLines(path, strings.ToArray());
            return true;
        }
    }
}
