using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;
using System.Reflection;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static SpaceSimulationResult ToSAM_SpaceSimulationResult(this TAS3D.Zone zone)
        {
            if (zone == null)
            {
                return null;
            }

            SpaceSimulationResult result = new SpaceSimulationResult(zone.name, Assembly.GetExecutingAssembly().GetName()?.Name, zone.GUID);
            result.SetValue("Color", Core.Convert.ToColor(zone.colour));
            result.SetValue("Description", zone.description);
            result.SetValue("External", zone.external);
            result.SetValue("Is Used", zone.isUsed);
            result.SetValue(Analytical.SpaceSimulationResultParameter.Area, zone.floorArea);
            result.SetValue(Analytical.SpaceSimulationResultParameter.Volume, zone.volume);

            return result;
        }

        public static SpaceSimulationResult ToSAM_SpaceSimulationResult(this TSD.ZoneData zoneData, IEnumerable<SpaceDataType> spaceDataTypes = null)
        {
            SpaceSimulationResult result = new SpaceSimulationResult(zoneData.name, Assembly.GetExecutingAssembly().GetName()?.Name, zoneData.zoneGUID);
            result.SetValue("Zone Number", zoneData.zoneNumber);
            result.SetValue("Description", zoneData.description);
            result.SetValue("Convective Weighting Factors Count", zoneData.nConvWeightingFactors);
            result.SetValue("Radiat Weighting Factors Count", zoneData.nRadWeightingFactors);
            result.SetValue(Analytical.SpaceSimulationResultParameter.Area, zoneData.floorArea);
            result.SetValue(Analytical.SpaceSimulationResultParameter.Volume, zoneData.volume);
            result.SetValue("Convective Common Ratio", zoneData.convectiveCommonRatio);
            result.SetValue("Radiat Common Ratio", zoneData.radiantCommonRatio);

            ParameterSet parameterSet = Create.ParameterSet_SpaceSimulationResult(ActiveSetting.Setting, zoneData);

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

            result.Add(parameterSet);

            result.SetValue(SpaceSimulationResultParameter.ZoneGuid, zoneData.zoneGUID);

            return result;
        }
    }
}
