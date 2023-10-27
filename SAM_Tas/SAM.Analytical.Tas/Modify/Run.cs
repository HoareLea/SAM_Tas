using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
         public static bool Run(Action<TCD.Document> action)
        {
            if (action == null)
            {
                return false;
            }

            string directory = System.IO.Path.GetTempPath();
            if (!System.IO.Directory.Exists(directory))
            {
                return false;
            }

            directory = System.IO.Path.Combine(directory, "SAM");
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            string fileName = Guid.NewGuid().ToString("N") + ".tcd";

            string path = System.IO.Path.Combine(directory, fileName);

            bool result = false;
            try
            {
                using (SAMTCDDocument sAMTCDDocument = new SAMTCDDocument())
                {
                    if (sAMTCDDocument.Create(path))
                    {
                        TCD.Document document = sAMTCDDocument.Document;
                        action.Invoke(document);
                        result = true;

                    }
                }
            }
            catch
            {

            }
            finally
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            return result;
        }
    }
}