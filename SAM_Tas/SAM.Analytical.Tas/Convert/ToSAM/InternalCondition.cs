namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static InternalCondition ToSAM(this TBD.InternalCondition internalCondition)
        {
            if (internalCondition == null)
            {
                return null;
            }

            InternalCondition result = new InternalCondition(internalCondition.name);

            TBD.Emitter emitter = null;

            emitter = internalCondition.GetHeatingEmitter();
            if (emitter != null)
            {
                result.SetValue(InternalConditionParameter.HeatingEmitterRadiantProportion, emitter.radiantProportion);
                result.SetValue(InternalConditionParameter.HeatingEmitterCoefficient, emitter.viewCoefficient);
            }

            emitter = internalCondition.GetCoolingEmitter();
            if (emitter != null)
            {
                result.SetValue(InternalConditionParameter.CoolingEmitterRadiantProportion, emitter.radiantProportion);
                result.SetValue(InternalConditionParameter.CoolingEmitterCoefficient, emitter.viewCoefficient);
            }

            TBD.InternalGain internalGain = internalCondition.GetInternalGain();
            if (internalGain != null)
            {
                TBD.profile profile_TBD = null;
                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticI);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.InfiltrationProfileName, profile_TBD.name);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticLG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.LightingProfileName, profile_TBD.name);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.OccupancyProfileName, profile_TBD.name);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticESG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentSensibleProfileName, profile_TBD.name);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticELG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentLatentProfileName, profile_TBD.name);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticCOG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.PollutantProfileName, profile_TBD.name);
                }

            }

            TBD.Thermostat thermostat = internalCondition.GetThermostat();
            if (internalGain != null)
            {
                TBD.profile profile_TBD = null;

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticUL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.CoolingProfileName, profile_TBD.name);
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticLL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.HeatingProfileName, profile_TBD.name);
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHLL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.HumidificationProfileName, profile_TBD.name);
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHUL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.DehumidificationProfileName, profile_TBD.name);
                }
            }

            return result;
        }
    }
}
