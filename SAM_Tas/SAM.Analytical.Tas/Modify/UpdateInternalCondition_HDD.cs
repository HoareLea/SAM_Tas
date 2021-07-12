namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateInternalCondition_HDD(this TBD.InternalCondition internalCondition_TBD, InternalCondition internalCondition, ProfileLibrary profileLibrary)
        {
            if (internalCondition_TBD == null || internalCondition == null)
                return false;

            internalCondition_TBD.description = internalCondition.Name + " - HDD";

            internalCondition_TBD.includeSolarInMRT = 0;

            TBD.Emitter emitter = null;
            double value = double.NaN;

            emitter = internalCondition_TBD.GetHeatingEmitter();
            if(emitter != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmitterRadiantProportion, out value))
                    emitter.radiantProportion = System.Convert.ToSingle(value);

                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmitterCoefficient, out value))
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
            }

            emitter = internalCondition_TBD.GetCoolingEmitter();
            if (emitter != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmitterRadiantProportion, out value))
                    emitter.radiantProportion = System.Convert.ToSingle(value);

                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmitterCoefficient, out value))
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
            }

            TBD.InternalGain internalGain = internalCondition_TBD.GetInternalGain();
            internalGain.name = internalCondition.Name + " - HDD";

            Profile profile = null;

            profile = internalCondition.GetProfile(ProfileType.Infiltration, profileLibrary);
            if (profile != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, out value))
                {
                    TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticI);
                    if (profile_TBD != null)
                    {
                        profile_TBD.name = profile.Name;
                        profile_TBD.type = TBD.ProfileTypes.ticValueProfile;
                        profile_TBD.factor = 1;
                        profile_TBD.value = System.Convert.ToSingle(value);
                    }
                }
            }

            TBD.Thermostat thermostat = internalCondition_TBD.GetThermostat();
            if (thermostat != null)
            {
                thermostat.controlRange = 0;
                thermostat.proportionalControl = 0;

                profile = internalCondition.GetProfile(ProfileType.Heating, profileLibrary);
                if (profile != null)
                {
                    thermostat.name = profile.Name;

                    TBD.profile profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticLL);
                    if (profile_TBD != null)
                    {
                        value = profile.MaxValue;
                        if (!double.IsNaN(value))
                        {
                            profile_TBD.name = profile.Name;
                            profile_TBD.type = TBD.ProfileTypes.ticValueProfile;
                            profile_TBD.factor = 1;
                            profile_TBD.value = System.Convert.ToSingle(value);
                        }
                    }
                }
            }

            return true;
        }

        public static bool UpdateInternalCondition_HDD(this TBD.InternalCondition internalCondition_TBD, Space space, ProfileLibrary profileLibrary)
        {
            if (internalCondition_TBD == null || space == null)
                return false;

            InternalCondition internalCondition = space.InternalCondition;
            if (internalCondition == null)
                return false;

            internalCondition_TBD.name = space.Name + " - HDD";

            return UpdateInternalCondition_HDD(internalCondition_TBD, internalCondition, profileLibrary);
        }
    }
}