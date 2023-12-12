using TPD;
using System.Collections.Generic;
using SAM.Core.Tas;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemModel ToSAM(string path_TPD, SystemModelConversionSettings systemModelConversionSettings  = null)
        {
            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                return null;
            }

            SystemModel result = null;
            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {

                result = ToSAM(sAMTPDDocument, systemModelConversionSettings);
            }

            return result;
        }

        public static SystemModel ToSAM(this SAMTPDDocument sAMTPDDocument, SystemModelConversionSettings systemModelConversionSettings = null)
        {
            if (sAMTPDDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTPDDocument.TPDDocument, systemModelConversionSettings);
        }

        public static SystemModel ToSAM(this TPDDoc tPDDoc, SystemModelConversionSettings systemModelConversionSettings = null)
        {
            EnergyCentre energyCentre = tPDDoc?.EnergyCentre;
            if(energyCentre == null)
            {
                return null;
            }

            SystemModel result = new SystemModel(energyCentre.Name);

            List<PlantRoom> plantRooms = tPDDoc.PlantRooms();
            if (plantRooms == null)
            {
                return result;
            }

            if(systemModelConversionSettings == null)
            {
                systemModelConversionSettings = new SystemModelConversionSettings();
            }

            int start = tPDDoc.StartHour();
            int end = tPDDoc.EndHour();

            List<SystemZone> systemZones = new List<SystemZone>();
            foreach (PlantRoom plantRoom in plantRooms)
            {
                if (systemModelConversionSettings.Simulate)
                {
                    plantRoom.SimulateEx(systemModelConversionSettings.StartHour + 1, systemModelConversionSettings.EndHour + 1, 0, energyCentre.ExternalPollutant.Value, 10.0, (int)global::TPD.tpdSimulationData.tpdSimulationDataLoad + (int)global::TPD.tpdSimulationData.tpdSimulationDataPipe + (int)global::TPD.tpdSimulationData.tpdSimulationDataDuct + (int)global::TPD.tpdSimulationData.tpdSimulationDataSimEvents, 0, 0);
                }

                List<global::TPD.System> systems = plantRoom?.Systems();
                if (systems == null)
                {
                    continue;
                }

                foreach (global::TPD.System system in systems)
                {
                    if (system == null)
                    {
                        continue;
                    }

                    List<SystemZone> systemZones_Temp = system.SystemZones();
                    if (systemZones_Temp == null || systemZones_Temp.Count == 0)
                    {
                        continue;
                    }

                    systemZones.AddRange(systemZones_Temp);
                }
            }

            if (systemZones == null || systemZones.Count == 0)
            {
                return result;
            }

            foreach(SystemZone systemZone in systemZones)
            {
                SystemSpace systemSpace = systemZone.ToSAM();
                if(systemSpace == null)
                {
                    continue;
                }

                result.Add(systemSpace);

                List<ZoneComponent> zoneComponents = Query.ZoneComponents<ZoneComponent>(systemZone);
                foreach(ZoneComponent zoneComponent in zoneComponents)
                {
                    SystemEquipment systemEquipment = zoneComponent.ToSAM();
                    if(systemEquipment == null)
                    {
                        continue;
                    }

                    result.Add(systemEquipment, systemSpace);

                    ISystemEquipmentResult systemEquipmentResult = zoneComponent.ToSAM_SystemEquipmentResult(start, end);
                    if (systemEquipmentResult == null)
                    {
                        continue;
                    }

                    result.Add(systemEquipmentResult, systemEquipment);
                }

                SystemSpaceResult systemSpaceResult = systemZone.ToSAM_SpaceSystemResult(result, start, end);
                if(systemSpaceResult != null)
                {
                    result.Add(systemSpaceResult, systemSpace);
                }

            }

            return result;
        }
    }
}
