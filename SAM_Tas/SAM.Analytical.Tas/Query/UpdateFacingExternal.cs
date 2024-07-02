using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static AnalyticalModel UpdateFacingExternal(this AnalyticalModel analyticalModel, string path_TBD)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD))
                return null;

            AnalyticalModel result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateFacingExternal(analyticalModel, sAMTBDDocument);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static AnalyticalModel UpdateFacingExternal(this AnalyticalModel analyticalModel, SAMTBDDocument sAMTBDDocument)
        {
            if (analyticalModel == null || sAMTBDDocument == null)
                return null;

            return UpdateFacingExternal(analyticalModel, sAMTBDDocument.TBDDocument);


        }

        public static AnalyticalModel UpdateFacingExternal(this AnalyticalModel analyticalModel, TBD.TBDDocument tBDDocument)
        {
            if (analyticalModel == null || tBDDocument == null)
                return null;

            Dictionary<string, TBD.zone> zones = tBDDocument.Building?.ZoneDictionary();
            if (zones == null)
                return null;

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
                return null;

            Dictionary<string, Space> spaces = adjacencyCluster.SpaceDictionary<Space>();
            if (spaces == null)
                return null;

            if (zones.Count == 0 || spaces.Count == 0)
                return new AnalyticalModel(analyticalModel);

            foreach (KeyValuePair<string, Space> keyValuePair in spaces)
            {
                TBD.zone zone = null;
                if (!zones.TryGetValue(keyValuePair.Key, out zone))
                    continue;

                if (zone == null)
                    continue;

                Space space_New = UpdateFacingExternal(keyValuePair.Value, zone);
                if (space_New == null)
                    continue;

                adjacencyCluster.AddObject(space_New);
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }

        public static Space UpdateFacingExternal(this Space space, TBD.zone zone)
        {
            if (space == null || zone == null)
                return null;

            int index = 0;
            TBD.zoneSurface zoneSurface = zone.GetSurface(index);
            bool facingExternal = false;
            bool facingExternalGlazing = false;
            while (zoneSurface != null)
            {
                if (zoneSurface.buildingElement != null)
                {
                    string name = zoneSurface.buildingElement.name;
                    if (!string.IsNullOrEmpty(name) && name.ToUpper().Contains("ADIABATIC"))
                        zoneSurface.type = TBD.SurfaceType.tbdNullLink;

                    if (!facingExternal)
                        facingExternal = !string.IsNullOrEmpty(name) && name.ToUpper().Contains("EXT") && !name.ToUpper().Contains("GRD");

                    if (!facingExternalGlazing)
                        facingExternalGlazing = !string.IsNullOrEmpty(name) && name.ToUpper().Contains("EXT_GLZ");
                }

                if (facingExternal && facingExternalGlazing)
                {
                    zoneSurface = null;
                }
                else
                {
                    index++;
                    zoneSurface = zone.GetSurface(index);
                }
            }


            Space space_New = new Space(space);
            space_New.SetValue(Analytical.SpaceParameter.FacingExternal, facingExternal);
            space_New.SetValue(Analytical.SpaceParameter.FacingExternalGlazing, facingExternalGlazing);

            return space_New;
        }
 }
}
