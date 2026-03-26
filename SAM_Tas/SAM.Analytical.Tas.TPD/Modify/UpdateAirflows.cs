using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<string> UpdateAirflows(this string path_TPD, Dictionary<string, Tuple<double, double>> airflows)
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

        public static List<string> UpdateAirflows(this SAMTPDDocument sAMTPDDocument, Dictionary<string, Tuple<double, double>> airflows)
        {
            return UpdateAirflows(sAMTPDDocument.TPDDocument, airflows);
        }

        public static List<string> UpdateAirflows(this TPDDoc tPDDoc, Dictionary<string, Tuple<double, double>> airflows)
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
                        List<ZoneLoad> zoneLoads = systemZone.ZoneLoads();
                        if(zoneLoads is null)
                        {
                            continue;
                        }

                        double airFlow = double.NaN;
                        double freshAirFlow = double.NaN;

                        string name = null;
                        foreach (ZoneLoad zoneLoad in systemZone.ZoneLoads())
                        {
                            name = zoneLoad.Name;

                            if (!airflows.TryGetValue(name, out Tuple<double, double> tuple))
                            {
                                airFlow = double.NaN;
                                freshAirFlow = double.NaN;
                                continue;
                            }
                            else 
                            {
                                airFlow = tuple.Item1;
                                freshAirFlow = tuple.Item2;
                            }

                            if (!double.IsNaN(airFlow) || !double.IsNaN(freshAirFlow))
                            {
                                break;
                            }
                        }

                        if(double.IsNaN(airFlow) && double.IsNaN(freshAirFlow))
                        {
                            continue;
                        }

                        //systemZone.FlowRate.Type = global::TPD.tpdSizedVariable.tpdSizedVariableNone;
                        //systemZone.FreshAir.Type = global::TPD.tpdSizedVariable.tpdSizedVariableNone;

                        dynamic @dynamic = systemZone;

                        @dynamic.FreshAir.Type = tpdSizedVariable.tpdSizedVariableNone;
                        @dynamic.FlowRate.Type = tpdSizedVariable.tpdSizedVariableNone;

                        if(!double.IsNaN(airFlow))
                        {
                            @dynamic.FlowRate.Type = tpdSizedVariable.tpdSizedVariableValue;
                            @dynamic.FlowRate.Value = airFlow;
                        }

                        if (!double.IsNaN(freshAirFlow))
                        {
                            @dynamic.FreshAir.Type = tpdSizedVariable.tpdSizedVariableValue;
                            @dynamic.FreshAir.Value = freshAirFlow;
                        }

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