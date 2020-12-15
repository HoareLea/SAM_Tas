using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateInternalCondition(this TBD.InternalCondition internalCondition_TBD, InternalCondition internalCondition, double area, double occupancy, ProfileLibrary profileLibrary)
        {
            if (internalCondition_TBD == null || internalCondition == null)
                return false;

            internalCondition_TBD.description = internalCondition.Name;

            internalCondition_TBD.includeSolarInMRT = 1;

            TBD.Emitter emitter = null;
            double value = double.NaN;

            emitter = internalCondition_TBD.GetHeatingEmitter();
            if(emitter != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmmiterRadiantProportion, out value))
                    emitter.radiantProportion = System.Convert.ToSingle(value);

                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmmiterCoefficient, out value))
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
            }

            emitter = internalCondition_TBD.GetCoolingEmitter();
            if (emitter != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmmiterRadiantProportion, out value))
                    emitter.radiantProportion = System.Convert.ToSingle(value);

                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmmiterCoefficient, out value))
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
            }

            TBD.InternalGain internalGain = internalCondition_TBD.GetInternalGain();
            internalGain.lightingRadProp = (float)0.3;
            internalGain.lightingViewCoefficient = (float)0.49;
            internalGain.equipmentRadProp = (float)0.1;
            internalGain.equipmentViewCoefficient = (float)0.372;
            internalGain.occupantRadProp = (float)0.2;
            internalGain.occupantViewCoefficient = (float)0.227;
            internalGain.domesticHotWater = (float)0.197;
            internalGain.name = internalCondition.Name;

            if (internalCondition.TryGetValue(InternalConditionParameter.LightingLevel, out value))
                internalGain.targetIlluminance = System.Convert.ToSingle(value);

            internalGain.personGain = 0;
            if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, out value))
                internalGain.personGain += System.Convert.ToSingle(value);
            if (internalCondition.TryGetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, out value))
                internalGain.personGain += System.Convert.ToSingle(value);

            Profile profile = null;
            
            profile = internalCondition.GetProfile(ProfileType.Infiltration, profileLibrary);
            if (profile != null)
            {
                if(internalCondition.TryGetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, out value))
                {
                    TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticI);
                    if (profile_TBD != null)
                        UpdateProfile(profile_TBD, profile, value);
                }
            }

            profile = internalCondition.GetProfile(ProfileType.Lighting, profileLibrary);
            if (profile != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.LightingGainPerArea, out value))
                {
                    TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticLG);
                    if (profile_TBD != null)
                        UpdateProfile(profile_TBD, profile, value);
                }
            }

            if(!double.IsNaN(area) && area != 0)
            {
                if(double.IsNaN(occupancy) || occupancy == 0)
                {
                    if (internalCondition.TryGetValue(InternalConditionParameter.AreaPerPerson, out value))
                        occupancy = area / value;
                }

                if(!double.IsNaN(occupancy) && occupancy != 0)
                {
                    profile = internalCondition.GetProfile(ProfileType.Occupancy, profileLibrary);
                    if (profile != null)
                    {
                        if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, out value))
                        {
                            TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                            if (profile_TBD != null)
                            {
                                value = occupancy * value / area;
                                UpdateProfile(profile_TBD, profile, value);
                            }
                        }
                    }

                    profile = internalCondition.GetProfile(ProfileType.Occupancy, profileLibrary);
                    if (profile != null)
                    {
                        if (internalCondition.TryGetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, out value))
                        {
                            TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOSG);
                            if (profile_TBD != null)
                            {
                                value = occupancy * value / area;
                                UpdateProfile(profile_TBD, profile, value);
                            }
                        }
                    }
                }
            }

            profile = internalCondition.GetProfile(ProfileType.EquipmentSensible, profileLibrary);
            if (profile != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, out value))
                {
                    TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticESG);
                    if (profile_TBD != null)
                    {
                        UpdateProfile(profile_TBD, profile, value);
                    }
                }
            }

            profile = internalCondition.GetProfile(ProfileType.EquipmentLatent, profileLibrary);
            if (profile != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentLatentGainPerArea, out value))
                {
                    TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticELG);
                    if (profile_TBD != null)
                    {
                        UpdateProfile(profile_TBD, profile, value);
                    }
                }
            }

            profile = internalCondition.GetProfile(ProfileType.Pollutant, profileLibrary);
            if (profile != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.PollutantGenerationPerArea, out value))
                {
                    TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticCOG);
                    if (profile_TBD != null)
                        UpdateProfile(profile_TBD, profile, value);
                }
            }

            TBD.Thermostat thermostat = internalCondition_TBD.GetThermostat();
            if (thermostat != null)
            {
                List<string> names = new List<string>();

                thermostat.controlRange = 0;
                thermostat.proportionalControl = 0;

                profile = internalCondition.GetProfile(ProfileType.Cooling, profileLibrary);
                if (profile != null)
                {
                    names.Add(profile.Name);

                    TBD.profile profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticUL);
                    if (profile_TBD != null)
                        UpdateProfile(profile_TBD, profile, 1);
                }

                profile = internalCondition.GetProfile(ProfileType.Heating, profileLibrary);
                if (profile != null)
                {
                    names.Add(profile.Name);

                    TBD.profile profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticLL);
                    if (profile_TBD != null)
                        UpdateProfile(profile_TBD, profile, 1);
                }

                profile = internalCondition.GetProfile(ProfileType.Humidification, profileLibrary);
                if (profile != null)
                {
                    names.Add(profile.Name);

                    TBD.profile profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHLL);
                    if (profile_TBD != null)
                        UpdateProfile(profile_TBD, profile, 1);
                }

                profile = internalCondition.GetProfile(ProfileType.Dehumidification, profileLibrary);
                if (profile != null)
                {
                    names.Add(profile.Name);

                    TBD.profile profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHUL);
                    if (profile_TBD != null)
                        UpdateProfile(profile_TBD, profile, 1);
                }

                names.RemoveAll(x => string.IsNullOrWhiteSpace(x));

                if (names.Count != 0)
                    thermostat.name = string.Join(" & ", names);
            }

            return true;
        }

        public static bool UpdateInternalCondition(this TBD.InternalCondition internalCondition_TBD, Space space, ProfileLibrary profileLibrary)
        {
            if (internalCondition_TBD == null || space == null)
                return false;

            InternalCondition internalCondition = space.InternalCondition;
            if (internalCondition == null)
                return false;

            double area = double.NaN;
            if (!space.TryGetValue(SpaceParameter.Area, out area))
                area = double.NaN;

            double occupancy = double.NaN;
            if (!space.TryGetValue(SpaceParameter.Occupancy, out occupancy))
                occupancy = double.NaN;

            internalCondition_TBD.name = space.Name;

            return UpdateInternalCondition(internalCondition_TBD, internalCondition, area, occupancy, profileLibrary);
        }
    }
}