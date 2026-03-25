using SAM.Core.Tas;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<string> UpdateAirflows(this string path_TPD, Dictionary<string, double> airflows)
        {
            if (airflows == null || airflows.Count == 0 || string.IsNullOrWhiteSpace(path_TPD) || !System.IO.File.Exists(path_TPD))
            {
                return null;
            }

            List<string> result = null;
            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {

                result = UpdateAirflows(sAMTPDDocument, airflows);
            }

            return result;
        }

        public static List<string> UpdateAirflows(this SAMTPDDocument sAMTPDDocument, Dictionary<string, double> airflows)
        {
            return UpdateAirflows(sAMTPDDocument.TPDDocument, airflows);
        }

        public static List<string> UpdateAirflows(this TPDDoc tPDDoc, Dictionary<string, double> airflows)
        {
            if(tPDDoc is null || airflows is null)
            {
                return null;
            }

            List<string> result = new List<string>();

            List<PlantRoom> plantRooms = tPDDoc.EnergyCentre?.PlantRooms();
            if(plantRooms is null || plantRooms.Count == 0)
            {
                return result;
            }

            foreach (PlantRoom plantRoom in plantRooms)
            {
                for (int i = 1; i <= plantRoom.GetSystemCount(); i++)
                {
                    global::TPD.System system = plantRoom.GetSystem(i);
                    if(system is null)
                    {
                        continue;
                    }

                    List<SystemZone> systemZones = system.SystemZones();
                    if(systemZones is null)
                    {
                        continue;
                    }

                    foreach (SystemZone systemZone in systemZones)
                    {
                        dynamic @dynamic = systemZone;

                        string name = dynamic.Name;

                        if (!airflows.TryGetValue(name, out double airFlow))
                        {
                            continue;
                        }

                        //systemZone.FlowRate.Type = global::TPD.tpdSizedVariable.tpdSizedVariableNone;
                        //systemZone.FreshAir.Type = global::TPD.tpdSizedVariable.tpdSizedVariableNone;

                        @dynamic.FlowRate.Type = tpdSizedVariable.tpdSizedVariableValue;
                        @dynamic.FlowRate.Value = airFlow;

                        @dynamic.FreshAir.Type = tpdSizedVariable.tpdSizedVariableNone;

                        result.Add(name);
                    }
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