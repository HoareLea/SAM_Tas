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
            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, zoneData);

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
    }
}
