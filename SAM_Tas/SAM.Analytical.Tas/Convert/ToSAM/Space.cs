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

        public static Space ToSAM(this TBD.zone zone, out List<InternalCondition> internalConditions)
        {
            internalConditions = null;

            if(zone == null)
            {
                return null;
            }

            Space result = new Space(zone.name);

            double area = zone.floorArea;

            result.SetValue(SpaceParameter.Area, area);
            result.SetValue(SpaceParameter.Volume, zone.volume);

            List<TBD.InternalCondition> internalConditions_TBD = zone.InternalConditions();
            if(internalConditions_TBD != null)
            {
                internalConditions = new List<InternalCondition>();
                
                foreach(TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
                {
                    InternalCondition internalCondition = internalCondition_TBD.ToSAM();
                    if(internalCondition == null)
                    {
                        continue;
                    }

                    TBD.InternalGain internalGain = internalCondition_TBD.GetInternalGain();
                    if(internalGain != null)
                    {
                        double personGain = internalGain.personGain;

                        TBD.profile profile = null;

                        double gain = 0;
                        profile =  internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                        if(profile != null)
                        {
                            gain += profile.GetExtremeValue(true);
                        }

                        profile = internalGain.GetProfile((int)TBD.Profiles.ticOSG);
                        if (profile != null)
                        {
                            gain += profile.GetExtremeValue(true);
                        }

                        double occupancy = (gain * area) / personGain;

                        internalCondition.SetValue(InternalConditionParameter.AreaPerPerson, area / occupancy);
                        //result.SetValue(SpaceParameter.Occupancy, occupancy);

                        profile = internalGain.GetProfile((int)TBD.Profiles.ticI);
                        if (profile != null)
                        {
                            internalCondition.SetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, profile.GetExtremeValue(true)); //.GetExtremeValue(true) awaiting Tas reply
                        }

                        profile = internalGain.GetProfile((int)TBD.Profiles.ticLG);
                        if (profile != null)
                        {
                            internalCondition.SetValue(InternalConditionParameter.LightingGainPerArea, profile.GetExtremeValue(true));
                            internalCondition.SetValue(InternalConditionParameter.LightingLevel, internalGain.targetIlluminance);
                        }

                        profile = internalGain.GetProfile((int)TBD.Profiles.ticOSG);
                        if (profile != null)
                        {
                            internalCondition.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, profile.GetExtremeValue(true));
                        }

                        profile = internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                        if (profile != null)
                        {
                            internalCondition.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, profile.GetExtremeValue(true));
                        }

                        profile = internalGain.GetProfile((int)TBD.Profiles.ticESG);
                        if (profile != null)
                        {
                            internalCondition.SetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, profile.GetExtremeValue(true));
                        }

                        profile = internalGain.GetProfile((int)TBD.Profiles.ticELG);
                        if (profile != null)
                        {
                            internalCondition.SetValue(InternalConditionParameter.EquipmentLatentGainPerArea, profile.GetExtremeValue(true));
                        }

                        profile = internalGain.GetProfile((int)TBD.Profiles.ticCOG);
                        if (profile != null)
                        {
                            result.SetValue(InternalConditionParameter.PollutantGenerationPerArea, profile.GetExtremeValue(true));
                        }
                    }

                    if (internalCondition == null)
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
