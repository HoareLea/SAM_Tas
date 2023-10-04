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

            TBD.Calendar calendar = tBDDocument.Building.GetCalendar();
            List<TBD.dayType> dayTypes = calendar.DayTypes();
            if(dayTypes != null)
            {
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

                string name = nCMData.Name;
                if(string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                TBD.zone zone = space.Match(zones);
                if(zone == null)
                {
                    continue;
                }

                internalConditions_TBD = Query.InternalConditions(tBDDocument);
                TBD.InternalCondition internalCondition_TBD = null;

                List<TBD.InternalCondition> internalConditions_TBD_Temp = internalConditions_TBD.FindAll(x => x.name.StartsWith(name));
                if(internalConditions_TBD_Temp != null && internalConditions_TBD_Temp.Count != 0)
                {
                    internalConditions_TBD_Temp.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                    internalCondition_TBD = internalConditions_TBD_Temp[0];
                }

                if(internalCondition_TBD == null)
                {
                    List<TIC.InternalCondition> internalConditions_TIC_Temp = internalConditions_TIC.FindAll(x => x.name.StartsWith(name));
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

                dayTypes = tBDDocument.Building.DayTypes();
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
                List<string> names = new List<string>();
                foreach (TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
                {
                    if(internalConditions_TBD_New.Find(x => x.name == internalCondition_TBD.name) != null)
                    {
                        continue;
                    }

                    names.Add(internalCondition_TBD.name);
                }

                foreach(string name in names)
                {
                    tBDDocument.Building.RemoveInternalCondition(name);
                }
            }

            tBDDocument.Building.ClearDesignDays();

            if(zones != null)
            {
                foreach(TBD.zone zone in zones)
                {
                    zone.sizeCooling = (int)TBD.SizingType.tbdSizing;
                    zone.sizeHeating = (int)TBD.SizingType.tbdSizing;
                }
            }

            return result;
        }
    }
}