using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateZones(this TBD.Building building, IEnumerable<Space> spaces, ProfileLibrary profileLibrary, bool includeHDD = false)
        {
            if (building == null || spaces == null || profileLibrary == null)
                return false;

            Dictionary<string, TBD.zone> dictionary_Zones = building.ZoneDictionary();
            if (dictionary_Zones == null)
                return false;

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

            RemoveInternalConditions(building, dictionary_Spaces.Keys);

            List<TBD.zone> result = new List<TBD.zone>();
            foreach (Space space in spaces)
            {
                string name = space?.Name;
                if (name == null)
                    continue;

                TBD.zone zone = null;
                if (!dictionary_Zones.TryGetValue(name, out zone) || zone == null)
                    continue;

                result.Add(building.UpdateZone(zone, space, profileLibrary));
                if (includeHDD)
                    building.UpdateZone_HDD(zone, space, profileLibrary);
            }

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

            return result != null && result.Count > 0;
        }

        public static bool UpdateZones(this TBD.Building building, AdjacencyCluster adjacencyCluster, ProfileLibrary profileLibrary, bool includeHDD = false)
        {
            return UpdateZones(building, adjacencyCluster?.GetSpaces(), profileLibrary, includeHDD);
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