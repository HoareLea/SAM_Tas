// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core.Tas;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<SystemFan> UpdateFanAirflows(this string path_TPD, IEnumerable<SystemFan> systemFans)
        {
            if (systemFans == null || !systemFans.Any() || string.IsNullOrWhiteSpace(path_TPD) || !System.IO.File.Exists(path_TPD))
            {
                return null;
            }

            List<SystemFan> result = null;
            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {

                result = UpdateFanAirflows(sAMTPDDocument, systemFans);
            }

            return result;
        }

        public static List<SystemFan> UpdateFanAirflows(this SAMTPDDocument sAMTPDDocument, IEnumerable<SystemFan> systemFans)
        {
            return UpdateFanAirflows(sAMTPDDocument.TPDDocument, systemFans);
        }

        public static List<SystemFan> UpdateFanAirflows(this TPDDoc tPDDoc, IEnumerable<SystemFan> systemFans)
        {
            if(tPDDoc is null || systemFans is null)
            {
                return null;
            }

            List<SystemFan> result = new List<SystemFan>();

            List<PlantRoom> plantRooms = tPDDoc.EnergyCentre?.PlantRooms();
            if(plantRooms is null || plantRooms.Count == 0)
            {
                return result;
            }

            List<SystemFan> systemFans_Temp = new List<SystemFan>(systemFans);

            foreach (PlantRoom plantRoom in plantRooms)
            {
                for (int i = 1; i <= plantRoom.GetSystemCount(); i++)
                {
                    global::TPD.System system = plantRoom.GetSystem(i);
                    if(system is null)
                    {
                        continue;
                    }

                    for(int j = systemFans_Temp.Count - 1; j >= 0; j--)
                    {
                        SystemFan systemFan = systemFans_Temp[j];

                        string reference = systemFan?.GetValue<string>(SystemObjectParameter.Reference);
                        if (string.IsNullOrWhiteSpace(reference))
                        {
                            continue;
                        }

                        global::TPD.Fan fan = system.GetComponentByGUID(reference) as global::TPD.Fan;
                        if (fan is null)
                        {
                            continue;
                        }

                        global::TPD.Fan fan_TPD = Convert.ToTPD(systemFan, system, fan);
                        if(fan_TPD != null)
                        {
                            result.Add(systemFan);
                            systemFans_Temp.RemoveAt(j);
                        }
                    }

                    if(systemFans_Temp.Count == 0)
                    {
                        break;
                    }
                }

                if (systemFans_Temp.Count == 0)
                {
                    break;
                }
            }

            if(result != null && result.Count > 0)
            {
                tPDDoc.Save();
            }

            return result;

        }
    }
}