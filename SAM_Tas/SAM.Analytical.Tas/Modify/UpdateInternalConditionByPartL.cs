using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateInternalConditionByPartL(this TBD.TBDDocument tBDDocument, TIC.Document tICDocument, AnalyticalModel analyticalModel)
        {
            if(tBDDocument == null || tICDocument == null || analyticalModel == null)
            {
                return false;
            }

            List<TBD.zone> zones = tBDDocument.Building.Zones();
            if(zones == null || zones.Count == 0)
            {
                return false;
            }

            List<Space> spaces = analyticalModel.GetSpaces();
            if(spaces == null || spaces.Count == 0)
            {
                return false;
            }

            List<TIC.InternalCondition> internalConditions_TIC = Query.InternalConditions(tICDocument);

            List<TBD.InternalCondition> internalConditions_TBD;

            List<TBD.InternalCondition> internalConditions_TBD_New = new List<TBD.InternalCondition>();

            bool result = false;
            foreach (Space space in spaces)
            {
                InternalCondition internalCondition = space?.InternalCondition;
                if(internalCondition == null)
                {
                    continue;
                }

                if(!internalCondition.TryGetValue(InternalConditionParameter.NCMData, out NCMData nCMData) || nCMData == null)
                {
                    continue;
                }

                string typeName = nCMData.Type;
                if(string.IsNullOrWhiteSpace(typeName))
                {
                    continue;
                }

                TBD.zone zone = null;

                if (space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) && !string.IsNullOrWhiteSpace(zoneGuid))
                {
                    zones.Find(x => x.GUID == zoneGuid);
                }

                if (zone == null)
                {
                    zone = zones.Find(x => x.name == space.Name);
                }

                if(zone == null)
                {
                    continue;
                }

                internalConditions_TBD = Query.InternalConditions(tBDDocument);
                TBD.InternalCondition internalCondition_TBD = null;

                List<TBD.InternalCondition> internalConditions_TBD_Temp = internalConditions_TBD.FindAll(x => x.name.StartsWith(typeName));
                if(internalConditions_TBD_Temp != null && internalConditions_TBD_Temp.Count != 0)
                {
                    internalConditions_TBD_Temp.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                    internalCondition_TBD = internalConditions_TBD_Temp[0];
                }

                if(internalCondition_TBD == null)
                {
                    List<TIC.InternalCondition> internalConditions_TIC_Temp = internalConditions_TIC.FindAll(x => x.name.StartsWith(typeName));
                    if(internalConditions_TIC_Temp != null && internalConditions_TIC_Temp.Count != 0)
                    {
                        internalConditions_TIC_Temp.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                        TIC.InternalCondition internalCondition_TIC = internalConditions_TIC_Temp[0];
                        dynamic @dynamic = tICDocument.ToGlobalMem(internalCondition_TIC);
                        internalCondition_TBD = tBDDocument.Building.AddInternalConditionFromGlobal(@dynamic);
                    }
                }

                if(internalCondition_TBD == null)
                {
                    continue;
                }

                internalConditions_TBD_New.Add(internalCondition_TBD);

                zone.AssignIC(internalCondition_TBD, true);

                List<TBD.dayType> dayTypes = tBDDocument.Building.DayTypes();
                if (dayTypes != null && dayTypes.Count != 0)
                {
                    foreach(TBD.dayType dayType in dayTypes)
                    {
                        bool add = !(dayType.name == "HDD" || dayType.name == "CDD");
                        internalCondition_TBD.SetDayType(dayType, add);
                    }
                }

                result = true;
            }

            internalConditions_TBD = Query.InternalConditions(tBDDocument);
            if(internalConditions_TBD != null)
            {
                foreach (TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
                {
                    if(internalConditions_TBD_New.Find(x => x.name == internalCondition_TBD.name) != null)
                    {
                        continue;
                    }

                    tBDDocument.Building.RemoveInternalCondition(internalCondition_TBD.name);
                }
            }

            tBDDocument.Building.ClearDesignDays();

            return result;
        }
    }
}