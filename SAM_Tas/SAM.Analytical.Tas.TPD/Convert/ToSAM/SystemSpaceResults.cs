// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using TPD;
using System.Collections.Generic;
using SAM.Core.Tas;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static List<SystemSpaceResult> ToSAM_SpaceSystemResults(string path_TPD, out string path_TSD)
        {
            path_TSD = null;

            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                return null;
            }

            List<SystemSpaceResult> result = null;
            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD, true))
            {

                result = ToSAM_SpaceSystemCalculationResults(sAMTPDDocument, out path_TSD);
            }

            return result;
        }

        public static List<SystemSpaceResult> ToSAM_SpaceSystemCalculationResults(this SAMTPDDocument sAMTPDDocument, out string path_TSD)
        {
            path_TSD = null;

            if (sAMTPDDocument == null)
            {
                return null;
            }

            return ToSAM_SpaceSystemResults(sAMTPDDocument.TPDDocument, out path_TSD);
        }

        public static List<SystemSpaceResult> ToSAM_SpaceSystemResults(this TPDDoc tPDDoc, out string path_TSD)
        {
            path_TSD = null;

            if (tPDDoc == null)
            {
                return null;
            }

            path_TSD = tPDDoc.EnergyCentre?.GetTSDData(1)?.TSDPath;

            List<SystemSpaceResult> result = new List<SystemSpaceResult>();

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

            if(start == -1 || end == -1 || end < start)
            {
                return result;
            }

            foreach (SystemZone systemZone in systemZones)
            {
                SystemSpaceResult spaceSystemCalculationResults = systemZone?.ToSAM_SpaceSystemResult(null, start, end);

                // A zone that produced no result used to be added as a null element, so every caller had
                // to remember that a list of results might contain nothing at some positions - and the
                // count of the list stopped meaning the number of rooms with results.
                if (spaceSystemCalculationResults == null)
                {
                    continue;
                }

                result.Add(spaceSystemCalculationResults);
            }

            return result;
        }
    }
}
