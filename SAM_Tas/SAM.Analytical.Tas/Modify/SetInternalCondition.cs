namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.InternalCondition SetInternalCondition(TBD.Building building, InternalCondition internalCondition, ProfileLibrary profileLibrary)
        {
            if (building == null || internalCondition == null)
                return null;

            TBD.InternalCondition result = building.AddIC(null);
            if (result == null)
                return null;

            result.name = internalCondition.Name;

            result.includeSolarInMRT = 1;

            TBD.Emitter emitter = null;
            double value = double.NaN;

            emitter = result.GetHeatingEmitter();
            if(emitter != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmmiterRadiantProportion, out value))
                    emitter.radiantProportion = System.Convert.ToSingle(value);

                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmmiterCoefficient, out value))
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
            }

            emitter = result.GetCoolingEmitter();
            if (emitter != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmmiterRadiantProportion, out value))
                    emitter.radiantProportion = System.Convert.ToSingle(value);

                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmmiterCoefficient, out value))
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
            }

            TBD.InternalGain internalGain = result.GetInternalGain();
            internalGain.lightingRadProp = (float)0.3;
            internalGain.lightingViewCoefficient = (float)0.49;
            internalGain.equipmentRadProp = (float)0.1;
            internalGain.equipmentViewCoefficient = (float)0.372;
            internalGain.occupantRadProp = (float)0.2;
            internalGain.occupantViewCoefficient = (float)0.227;
            internalGain.domesticHotWater = (float)0.197;

            if (internalCondition.TryGetValue(InternalConditionParameter.LightingLevel, out value))
                internalGain.targetIlluminance = System.Convert.ToSingle(value);

            internalGain.personGain = 0;
            if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyLatentGain, out value))
                internalGain.personGain += System.Convert.ToSingle(value);
            if (internalCondition.TryGetValue(InternalConditionParameter.OccupancySensibleGain, out value))
                internalGain.personGain += System.Convert.ToSingle(value);

            Profile profile = internalCondition.GetProfile(ProfileType.Infiltration, profileLibrary);
            if (profile != null)
            {
                if(internalCondition.TryGetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, out value))
                {
                    TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticI);
                    if (profile_TBD != null)
                        UpdateProfile(profile_TBD, profile, value);
                }
            }

            return result;
        }
    }
}