using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Space ToSAM(this TAS3D.Zone zone)
        {
            if (zone == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, zone);

            Space space = new Space(zone.name, null);
            space.Add(parameterSet);

            return space;
        }

        public static Space ToSAM(this TSD.ZoneData zoneData, IEnumerable<SpaceDataType> spaceDataTypes = null)
        {
            ParameterSet parameterSet = Create.ParameterSet_Space(ActiveSetting.Setting, zoneData);

            if(spaceDataTypes != null)
            {
                foreach(SpaceDataType spaceDataType in spaceDataTypes)
                {
                    List<double> values = zoneData.AnnualZoneResult<double>(spaceDataType);
                    if (values == null)
                        continue;

                    JArray jArray = new JArray();
                    values.ForEach(x => jArray.Add(x));

                    parameterSet.Add(spaceDataType.Text(), jArray);
                }
            }

            Space space = new Space(zoneData.name, null);
            space.Add(parameterSet);

            return space;
        }

        public static Space ToSAM(this TBD.zone zone, out List<InternalCondition> internalConditions)
        {
            internalConditions = null;

            if(zone == null)
            {
                return null;
            }

            Space result = new Space(zone.name);

            double area = zone.floorArea;

            result.SetValue(Analytical.SpaceParameter.Area, area);
            result.SetValue(Analytical.SpaceParameter.Volume, zone.volume);
            result.SetValue(SpaceParameter.ZoneGuid, zone.GUID);

            List<TBD.InternalCondition> internalConditions_TBD = zone.InternalConditions();
            if(internalConditions_TBD != null)
            {
                internalConditions = new List<InternalCondition>();
                
                foreach(TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
                {
                    InternalCondition internalCondition = internalCondition_TBD.ToSAM(area);
                    if(internalCondition == null)
                    {
                        continue;
                    }

                    internalConditions.Add(internalCondition);
                }
            }

            return result;
        }
    }
}
