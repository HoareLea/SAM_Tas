using SAM.Core.Tas;
using System.Collections.Generic;
using System.IO;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool Simulate(string path_TPD, int startHour, int endHour)
        {
            if(string.IsNullOrWhiteSpace(path_TPD) || !File.Exists(path_TPD))
            {
                return false;
            }

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {
                TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                if (tPDDoc != null)
                {
                    List<PlantRoom> plantRooms = tPDDoc?.EnergyCentre?.PlantRooms();
                    if (plantRooms != null)
                    {
                        foreach (PlantRoom plantRoom in plantRooms)
                        {
                            plantRoom.SimulateEx(startHour + 1, endHour + 1, 0, tPDDoc.EnergyCentre.ExternalPollutant.Value, 10.0, (int)tpdSimulationData.tpdSimulationDataLoad + (int)tpdSimulationData.tpdSimulationDataPipe + (int)tpdSimulationData.tpdSimulationDataDuct + (int)tpdSimulationData.tpdSimulationDataSimEvents + (int)tpdSimulationData.tpdSimulationDataCont, 1, 0);
                        }
                    }

                    tPDDoc.Save();
                }
            }


            return true;
        }

    }
}