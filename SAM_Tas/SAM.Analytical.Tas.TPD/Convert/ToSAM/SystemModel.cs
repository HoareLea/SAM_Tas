using TPD;
using System.Collections.Generic;
using SAM.Core.Tas;
using SAM.Core.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEnergyCentre ToSAM(string path_TPD, SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = null)
        {
            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                return null;
            }

            SystemEnergyCentre result = null;
            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {

                result = ToSAM(sAMTPDDocument, systemEnergyCentreConversionSettings);
            }

            return result;
        }

        public static SystemEnergyCentre ToSAM(this SAMTPDDocument sAMTPDDocument, SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = null)
        {
            if (sAMTPDDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTPDDocument.TPDDocument, systemEnergyCentreConversionSettings);
        }

        public static SystemEnergyCentre ToSAM(this TPDDoc tPDDoc, SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = null)
        {
            EnergyCentre energyCentre = tPDDoc?.EnergyCentre;
            if(energyCentre == null)
            {
                return null;
            }

            SystemEnergyCentre result = new SystemEnergyCentre(energyCentre.Name);

            List<PlantRoom> plantRooms = tPDDoc.PlantRooms();
            if (plantRooms == null)
            {
                return result;
            }

            if(systemEnergyCentreConversionSettings == null)
            {
                systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings();
            }

            ComponentConversionSettings componentConversionSettings = systemEnergyCentreConversionSettings.GetComponentConversionSettings();

            foreach (PlantRoom plantRoom in plantRooms)
            {
                if (systemEnergyCentreConversionSettings.Simulate)
                {
                    plantRoom.SimulateEx(systemEnergyCentreConversionSettings.StartHour + 1, systemEnergyCentreConversionSettings.EndHour + 1, 0, energyCentre.ExternalPollutant.Value, 10.0, (int)tpdSimulationData.tpdSimulationDataLoad + (int)tpdSimulationData.tpdSimulationDataPipe + (int)tpdSimulationData.tpdSimulationDataDuct + (int)tpdSimulationData.tpdSimulationDataSimEvents, 0, 0);
                }

                SystemPlantRoom systemPlantRoom = plantRoom.ToSAM();

                List<global::TPD.System> systems = plantRoom?.Systems();
                if (systems == null)
                {
                    continue;
                }

                foreach (global::TPD.System system in systems)
                {
                    systemPlantRoom.Add(system, tPDDoc, componentConversionSettings);
                }

                result.Add(systemPlantRoom);
            }

            return result;
        }
    }
}
