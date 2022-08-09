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

                if(!space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) || string.IsNullOrWhiteSpace(zoneGuid))
                {
                    continue;
                }

                TBD.zone zone = zones.Find(x => x.GUID == zoneGuid);
                if(zone == null)
                {
                    continue;
                }

                List<TBD.InternalCondition> internalConditions_TBD = Query.InternalConditions(tBDDocument);

                TBD.InternalCondition internalCondition_TBD = null;

                List<TBD.InternalCondition> internalConditions_TBD_Temp = internalConditions_TBD.FindAll(x => x.name.StartsWith(typeName));
                if(internalConditions_TBD_Temp != null)
                {
                    internalConditions_TBD_Temp.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                    internalCondition_TBD = internalConditions_TBD_Temp[0];
                }

                if(internalConditions_TBD == null)
                {
                    List<TIC.InternalCondition> internalConditions_TIC_Temp = internalConditions_TIC.FindAll(x => x.name.StartsWith(typeName));
                    if(internalConditions_TIC_Temp != null && internalConditions_TIC_Temp.Count != 0)
                    {
                        internalConditions_TIC_Temp.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                        TIC.InternalCondition internalCondition_TIC = internalConditions_TIC_Temp[0];
                        dynamic @dynamic = tICDocument.ToGlobalMem(internalCondition_TIC);
                        internalCondition_TBD = tBDDocument.Building.AddInternalConditionFromGlobal(internalCondition_TIC);
                    }
                }

                if(internalCondition_TBD == null)
                {
                    continue;
                }

                zone.AssignIC(internalCondition_TBD, false);
                result = true;
            }


            return result;
        }
    }
}