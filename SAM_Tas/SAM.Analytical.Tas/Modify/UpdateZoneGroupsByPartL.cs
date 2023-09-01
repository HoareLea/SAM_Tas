using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static HashSet<string> UpdateZoneGroupsByPartL(this TBD.TBDDocument tBDDocument, AnalyticalModel analyticalModel)
        {
            if (tBDDocument == null || analyticalModel == null)
            {
                return null;
            }

            List<TBD.zone> zones = tBDDocument.Building.Zones();
            if (zones == null || zones.Count == 0)
            {
                return null;
            }

            List<Space> spaces = analyticalModel.GetSpaces();
            if (spaces == null || spaces.Count == 0)
            {
                return null;
            }

            tBDDocument.Building.RemoveZoneGroups();

            HashSet<string> result = new HashSet<string>();
            foreach (Space space in spaces)
            {
                InternalCondition internalCondition = space?.InternalCondition;
                if (internalCondition == null)
                {
                    continue;
                }

                if (!internalCondition.TryGetValue(InternalConditionParameter.NCMData, out NCMData nCMData) || nCMData == null)
                {
                    continue;
                }

                NCMSystemType nCMSystemType = nCMData.SystemType;
                if (nCMSystemType == NCMSystemType.Undefined)
                {
                    continue;
                }

                List<string> values = new List<string>();
                values.Add(string.Format("NCM:{0}", Core.Query.Description(nCMSystemType)));

                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingSystemTypeName, out string heatingSystemTypeName) && !string.IsNullOrWhiteSpace(heatingSystemTypeName))
                {
                    values.Add(string.Format("H:{0}", heatingSystemTypeName));
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingSystemTypeName, out string coolingSystemTypeName) && !string.IsNullOrWhiteSpace(coolingSystemTypeName))
                {
                    values.Add(string.Format("C:{0}", coolingSystemTypeName));
                }

                string description = nCMData?.Description;
                if(!string.IsNullOrWhiteSpace(description))
                {
                    values.Add(description);
                }

                string name = string.Join("_", values).Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                TBD.zone zone = space.Match(zones);
                if (zone == null)
                {
                    continue;
                }

                List<TBD.ZoneGroup> zoneGroups = tBDDocument.Building.ZoneGroups();
                TBD.ZoneGroup zoneGroup = zoneGroups?.Find(x => x.name == name);
                if (zoneGroup == null)
                {
                    zoneGroup = tBDDocument.Building.AddZoneGroup();
                    zoneGroup.name = name;
                    zoneGroup.type = (int)TBD.ZoneGroupType.tbdHVACZG;
                }

                if (zoneGroup == null)
                {
                    continue;
                }

                zoneGroup.InsertZone(zone);
                result.Add(zoneGroup.name);
            }

            return result;
        }
    }
}