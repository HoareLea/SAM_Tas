using SAM.Core.Tas;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Updates TBD.zones in given TBD.Buiding based on provided spaces and profileLibrary
        /// </summary>
        /// <param name="building">TBD.Buidling</param>
        /// <param name="adjacencyCluster">SAM Analytical AdjacencyCluster</param>
        /// <param name="profileLibrary">ProfileLibrary which contains information about profiles used in spaces</param>
        /// <param name="includeHDD">Include Heating Design Day in the process</param>
        /// <returns>TBD.zones have been used in update process</returns>
        public static bool UpdateZones(this TBD.Building building, AdjacencyCluster adjacencyCluster, ProfileLibrary profileLibrary, bool includeHDD = false)
        {
            if (building == null || adjacencyCluster == null || profileLibrary == null)
                return false;

            List<Space> spaces = adjacencyCluster?.GetSpaces();
            if(spaces == null || spaces.Count == 0)
            {
                return false;
            }

            //Zone Dictionary <- Dictionary constains zone.name as a key and TBD.zone as Value. Dictionary helps to match TBD.zone with SAM.Analytical.Space
            Dictionary<string, TBD.zone> dictionary_Zones = building.ZoneDictionary();
            if (dictionary_Zones == null)
                return false;
            
            //Space Dictionary <- Dictionary constains Space.Name as a key and SAM.Analytical.Space as Value. Assumption: InternalCondition Name equals to Space Name. It also holds names for HDD Spaces/InternalConditions
            Dictionary<string, Space> dictionary_Spaces = new Dictionary<string, Space>();
            foreach(Space space in spaces)
            {
                string name = space.Name;
                if (name == null)
                    continue;

                dictionary_Spaces[name] = space;
                if (includeHDD)
                    dictionary_Spaces[space.Name + " - HDD"] = space;
            }

            //Removes Internal Conditions with given names. Names are taken from Space Name (assumption Space Name equals InternalCondtion Name)
            RemoveInternalConditions(building, dictionary_Spaces.Keys);

            List<TBD.zone> result = new List<TBD.zone>();
            foreach (Space space in spaces)
            {
                string name = space?.Name;
                if (name == null)
                    continue;

                //Matching Space with TBD.zone via name
                TBD.zone zone = null;
                if (!dictionary_Zones.TryGetValue(name, out zone) || zone == null)
                    continue;

                zone = building.UpdateZone(zone, space, profileLibrary, adjacencyCluster);

                VentilationSystem ventilationSystem = adjacencyCluster.GetRelatedObjects<VentilationSystem>(space)?.FirstOrDefault();
                if(ventilationSystem != null)
                {
                    string ventilationSystemTypeName = (ventilationSystem.Type as VentilationSystemType)?.Name;
                    if(!string.IsNullOrWhiteSpace(ventilationSystemTypeName))
                    {
                        TBD.ZoneGroup zoneGroup = Query.ZoneGroups(building)?.Find(x => ventilationSystemTypeName.Equals(x.name));
                        if (zoneGroup == null)
                        {
                            zoneGroup = building.AddZoneGroup();
                            zoneGroup.name = ventilationSystemTypeName;
                            zoneGroup.type = (int)TBD.ZoneGroupType.tbdHVACZG;
                        }

                        if (zoneGroup != null)
                        {
                            zoneGroup.InsertZone(zone);
                        }
                    }
                }

                //Update TBD.zone using data stored in space and ProfileLibrary
                result.Add(zone);
                
                //Include HDD if includeHDD input set to true
                if (includeHDD)
                    building.UpdateZone_HDD(zone, space, profileLibrary);
            }

            //Updating Builidng Information
            building.description = string.Format("Delivered by SAM https://github.com/HoareLea/SAM [{0}]", System.DateTime.Now.ToString("yyyy/MM/dd"));

            TBD.GeneralDetails generaldetails = building.GetGeneralDetails();
            if(generaldetails != null)
            {
                if (generaldetails.engineer1 == "")
                    generaldetails.engineer1 = System.Environment.UserName;
                else if(generaldetails.engineer1 != System.Environment.UserName)
                    generaldetails.engineer2 = System.Environment.UserName;

                if (generaldetails.externalPollutant == 315) //600
                {
                    generaldetails.externalPollutant = 415;
                }
                generaldetails.TerrainType = TBD.TerrainType.tbdCity;
            }

            //Returning TBD.zones have been used in update process
            return result != null && result.Count > 0;
        }

        public static bool UpdateZones(this TBD.Building building, AnalyticalModel analyticalModel, bool includeHDD = false)
        {
            if (analyticalModel == null || building == null)
                return false;

            building.name = analyticalModel.Name;
            
            return UpdateZones(building, analyticalModel?.AdjacencyCluster, analyticalModel.ProfileLibrary, includeHDD);
        }

        public static bool UpdateZones(this AnalyticalModel analyticalModel, SAMTBDDocument sAMTBDDocument, bool includeHDD = false)
        {
            if (analyticalModel == null || sAMTBDDocument == null)
                return false;

            return UpdateZones(sAMTBDDocument.TBDDocument?.Building, analyticalModel, includeHDD);
        }

        public static bool UpdateZones(this AnalyticalModel analyticalModel, string path_TBD, bool includeHDD = false)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateZones(analyticalModel, sAMTBDDocument, includeHDD);
                if (result)
                    sAMTBDDocument.Save();
            }

            return result;
        }
    }
}