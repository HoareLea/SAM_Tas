﻿using TPD;
using System.Collections.Generic;
using SAM.Core.Tas;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static List<SpaceSystemCalculationResult> ToSAM_SpaceSystemCalculationResults(string path_TPD)
        {
            if(string.IsNullOrWhiteSpace(path_TPD))
            {
                return null;
            }

            List<SpaceSystemCalculationResult> result = null;
            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {

                result = ToSAM_SpaceSystemCalculationResults(sAMTPDDocument);
            }

            return result;
        }

        public static List<SpaceSystemCalculationResult> ToSAM_SpaceSystemCalculationResults(this SAMTPDDocument sAMTPDDocument)
        {
            if (sAMTPDDocument == null)
            {
                return null;
            }

            return ToSAM_SpaceSystemCalculationResults(sAMTPDDocument.TPDDocument);
        }

        public static List<SpaceSystemCalculationResult> ToSAM_SpaceSystemCalculationResults(this TPDDoc tPDDoc)
        {
            if (tPDDoc == null)
            {
                return null;
            }

            List<SpaceSystemCalculationResult> result = new List<SpaceSystemCalculationResult>();

            List<PlantRoom> plantRooms = tPDDoc.PlantRooms();
            if(plantRooms == null)
            {
                return result;
            }

            List<SystemZone> systemZones = new List<SystemZone>();
            foreach(PlantRoom plantRoom in plantRooms)
            {
                List<global::TPD.System> systems = plantRoom?.Systems();
                if(systems == null)
                {
                    continue;
                }

                foreach(global::TPD.System system in systems)
                {
                    if(system == null)
                    {
                        continue;
                    }

                    List<SystemZone> systemZones_Temp = system.SystemZones();
                    if(systemZones_Temp == null || systemZones_Temp.Count == 0)
                    {
                        continue;
                    }

                    systemZones.AddRange(systemZones_Temp);
                }
            }

            if(systemZones == null || systemZones.Count == 0)
            {
                return result;
            }

            int start = tPDDoc.StartHour();
            int end = tPDDoc.EndHour();

            if(start == -1 || end == -1 || end > start)
            {
                return result;
            }

            foreach (SystemZone systemZone in systemZones)
            {
                SpaceSystemCalculationResult spaceSystemCalculationResults = systemZone?.ToSAM_SpaceSystemCalculationResult(start, end);
                result.Add(spaceSystemCalculationResults);
            }

            return result;
        }
    }
}
