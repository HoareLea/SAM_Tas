using SAM.Core.Tas;
using System.Collections.Generic;
using TBD;
using System.Linq;
using SAM.Core;
using SAM.Geometry.Object.Spatial;
using System;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<string> UpdateIZAMs(this string path_TBD, AdjacencyCluster adjacencyCluster)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<string> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateIZAMs(sAMTBDDocument, adjacencyCluster);
                if (result != null)
                {
                    sAMTBDDocument.Save();
                }
            }

            return result;
        }

        public static List<string> UpdateIZAMs(this SAMTBDDocument sAMTBDDocument, AdjacencyCluster adjacencyCluster)
        {
            if (sAMTBDDocument == null)
            {
                return null;
            }

            return UpdateIZAMs(sAMTBDDocument.TBDDocument, adjacencyCluster);
        }

        public static List<string> UpdateIZAMs(this TBDDocument tBDDocument, AdjacencyCluster adjacencyCluster)
        {
            if(tBDDocument == null || adjacencyCluster == null)
            {
                return null;
            }

            Building building = tBDDocument.Building;

            List<zone> zones = building.Zones();

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if(spaces == null)
            {
                return null;
            }

            List<AirHandlingUnit> airHandlingUnits = adjacencyCluster.GetObjects<AirHandlingUnit>();
            if(airHandlingUnits == null)
            {
                return null;
            }

            List<dayType> dayTypes = building.DayTypes();
            dayTypes.RemoveAll(x => x.name.Equals("CDD") || x.name.Equals("HDD"));

            List<string> result = new List<string>();

            double height = 2;

            double elevation = 0;
            Geometry.Spatial.BoundingBox3D boundingBox3D = adjacencyCluster?.GetPanels()?.BoundingBox3D(1);
            if(boundingBox3D != null)
            {
                elevation = boundingBox3D.Min.Z - height -  1;
            }

            

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                AirHandlingUnitAirMovement airHandlingUnitAirMovement = adjacencyCluster.GetRelatedObjects<AirHandlingUnitAirMovement>(airHandlingUnit)?.FirstOrDefault();
                if(airHandlingUnitAirMovement == null)
                {
                    continue;
                }

                AdjacencyCluster adjacencyCluster_Temp = Create.AdjacencyCluster(elevation, 3, height, 3);
                elevation -= height - 1;

                Update(building, adjacencyCluster_Temp, Analytical.Query.DefaultMaterialLibrary(), true);

                Space space = adjacencyCluster_Temp.GetSpaces().FirstOrDefault();

                zones = building.Zones();
                zone zone = zones.Match(space.Name, false, true);
                if(zone == null)
                {
                    continue;
                }

                zone.name = airHandlingUnit.Name;
                zone.sizeHeating = (int)TBD.SizingType.tbdSizing;

                string name = string.Format("{0}", airHandlingUnitAirMovement.Name);

                RemoveInternalConditions(building, new string[] { name });

                TBD.InternalCondition internalCondition = building.AddIC(null);
                internalCondition.name = name;
                foreach (dayType dayType in dayTypes)
                {
                    internalCondition.SetDayType(dayType, true);
                }

                Thermostat thermostat = internalCondition.GetThermostat();

                if(thermostat != null)
                {
                    Profile heating = airHandlingUnitAirMovement.Heating;
                    if (heating != null)
                    {
                        profile profile_TBD = thermostat.GetProfile((int)Profiles.ticLL);
                        if (profile_TBD != null)
                        {
                            Update(profile_TBD, heating, 1);
                        }
                    }

                    Profile cooling = airHandlingUnitAirMovement.Cooling;
                    if (cooling != null)
                    {
                        profile profile_TBD = thermostat.GetProfile((int)Profiles.ticUL);
                        if (profile_TBD != null)
                        {
                            Update(profile_TBD, cooling, 1);
                        }
                    }

                    Profile humidification = airHandlingUnitAirMovement.Humidification;
                    if (humidification != null)
                    {
                        profile profile_TBD = thermostat.GetProfile((int)Profiles.ticHLL);
                        if (profile_TBD != null)
                        {
                            Update(profile_TBD, humidification, 1);
                        }
                    }

                    Profile dehumidification = airHandlingUnitAirMovement.Dehumidification;
                    if (dehumidification != null)
                    {
                        profile profile_TBD = thermostat.GetProfile((int)Profiles.ticHUL);
                        if (profile_TBD != null)
                        {
                            Update(profile_TBD, dehumidification, 1);
                        }
                    }

                    zone.AssignIC(internalCondition, true);
                }

                double airFlow = Analytical.Query.AirFlow(adjacencyCluster, airHandlingUnitAirMovement, out Profile profile_AirHandlingUnit);
                if(profile_AirHandlingUnit != null)
                {
                    name = string.Format("IZAM {0} FROM OUTSIDE", airHandlingUnitAirMovement.Name);
                    RemoveIZAMs(building, new string[] { name });

                    IZAM iZAM = building.AddIZAM(null);
                    iZAM.fromOutside = 1;
                    iZAM.name = name;
                    result.Add(iZAM.name);

                    foreach (dayType dayType in dayTypes)
                    {
                        iZAM.SetDayType(dayType, true);
                    }

                    profile profile = iZAM.GetProfile();
                    profile.Update(profile_AirHandlingUnit, airFlow);

                    zone.AssignIZAM(iZAM, true);
                }

            }

            List<Tuple<IZAM, SAMObject>> tuples = new List<Tuple<IZAM, SAMObject>>();
            foreach(Space space in spaces)
            {
                zone zone = zones.Match(space.Name, false, true);
                if (zone == null)
                {
                    continue;
                }

                zone.sizeHeating = (int)TBD.SizingType.tbdNoSizing;

                List<SpaceAirMovement> spaceAirMovements = adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(space);
                if (spaceAirMovements == null || spaceAirMovements.Count == 0)
                {
                    continue;
                }

                ObjectReference objectReference_Space = new ObjectReference(space);

                foreach(SpaceAirMovement spaceAirMovement in spaceAirMovements)
                {
                    ObjectReference objectReference_From = Core.Convert.ComplexReference<ObjectReference>(spaceAirMovement.From);
                    ObjectReference objectReference_To = Core.Convert.ComplexReference<ObjectReference>(spaceAirMovement.To);

                    SAMObject sAMObject_From = adjacencyCluster.GetObjects<SAMObject>(objectReference_From)?.FirstOrDefault();
                    SAMObject sAMObject_To = adjacencyCluster.GetObjects<SAMObject>(objectReference_To)?.FirstOrDefault();

                    string name = string.Format("IZAM {0}", sAMObject_From.Name);
                    name = sAMObject_To == null ? string.Format("{0} TO OUTSIDE", name) : string.Format("{0} TO {1}", name, sAMObject_To.Name);

                    RemoveIZAMs(building, new string[] { name });

                    IZAM iZAM = building.AddIZAM(null);

                    foreach (dayType dayType in dayTypes)
                    {
                        iZAM.SetDayType(dayType, true);
                    }

                    iZAM.name = name;
                    iZAM.fromOutside = 0;
                    result.Add(iZAM.name);

                    profile profile = iZAM.GetProfile();
                    profile.Update(spaceAirMovement.Profile, spaceAirMovement.AirFlow);

                    zone.AssignIZAM(iZAM, true);

                    if (sAMObject_From != null && sAMObject_From.Guid != space.Guid)
                    {
                        zone zone_From = zones.Match(sAMObject_From.Name, false, true);
                        if(zone_From != null)
                        {
                            iZAM.SetSourceZone(zone_From);
                        }
                    }
                }
            }

            return result;
        }
    }
}