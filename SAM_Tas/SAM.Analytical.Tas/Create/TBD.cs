using SAM.Core.Tas;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static bool TBD_ByPartL(this AnalyticalModel analyticalModel, string path_TBD, out string path_TBD_Destination, string path_TCR = null, string path_TIC = null, string fileName = null, string calendarName = null)
        {
            path_TBD_Destination = null;

            if (analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD) || !System.IO.File.Exists(path_TBD))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path_TCR))
            {
                path_TCR = Query.DefaultPath(TasSettingParameter.DefaultTCRFileName);
                if (string.IsNullOrWhiteSpace(path_TCR))
                {
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(path_TIC))
            {
                path_TIC = Query.DefaultPath(TasSettingParameter.DefaultTICFileName);
            }

            if (string.IsNullOrEmpty(calendarName))
            {
                calendarName = "NCM Standard";
            }
            
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD) + "_PartL" + System.IO.Path.GetExtension(path_TBD);
            }

            path_TBD_Destination = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path_TBD), fileName);

            System.IO.File.Copy(path_TBD, path_TBD_Destination, true);

            bool result = false;
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD_Destination))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                TBD.Calendar calendar = sAMTBDDocument.TBDDocument?.Building?.GetCalendar();
                if (calendar != null)
                {
                    using (SAMTCRDocument sAMTCRDocument = new SAMTCRDocument(path_TCR, true))
                    {
                        result = Modify.CopyFrom(calendar, sAMTCRDocument.Document, calendarName);
                    }
                }

                using (SAMTICDocument sAMTICDocument = new SAMTICDocument(path_TIC, true))
                {
                    TIC.Document tICDocument = sAMTICDocument.Document;

                    result = Modify.UpdateInternalConditionByPartL(tBDDocument, tICDocument, analyticalModel);
                    if (result)
                    {
                        Modify.UpdateZoneGroupsByPartL(tBDDocument, analyticalModel);
                        Modify.UpdateZoneGroups(tBDDocument, analyticalModel);
                        sAMTBDDocument.Save();
                    }
                }
            }

            return result;
        }
    }
}