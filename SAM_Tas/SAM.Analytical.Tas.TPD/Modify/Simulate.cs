using SAM.Core.Tas;
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

                if (tPDDoc?.EnergyCentre != null)
                {
                    EnergyCentre energyCentre = tPDDoc.EnergyCentre;

                    int count = energyCentre.GetPlantRoomCount();
                    if(count == 1)
                    {
                        for (int i = 1; i <= count; i++)
                        {
                            PlantRoom plantRoom = energyCentre.GetPlantRoom(i);
                            string value = plantRoom.SimulateEx(
                                startHour + 1,
                                endHour + 1,
                                0,
                                tPDDoc.EnergyCentre.ExternalPollutant.Value,
                                10.0,
                                (int)tpdSimulationData.tpdSimulationDataLoad + (int)tpdSimulationData.tpdSimulationDataPipe + (int)tpdSimulationData.tpdSimulationDataDuct + (int)tpdSimulationData.tpdSimulationDataSimEvents + (int)tpdSimulationData.tpdSimulationDataCont,
                                1,
                                0);

                            //string result = plantRoom.SimulateEx(1, 8760, 0, tPDDoc.EnergyCentre.ExternalPollutant.Value, 10.0, (int)tpdSimulationData.tpdSimulationDataLoad, 1, 0);
                        }
                    }
                    else
                    {
                        tPDDoc.Simulate(startHour + 1, endHour + 1, 0);
                    }

                    tPDDoc.Save();
                }
            }


            return true;
        }

    }
}