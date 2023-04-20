namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static InternalCondition ToSAM(this TBD.InternalCondition internalCondition, double area = double.NaN)
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
                result.SetValue(InternalConditionParameter.LightingRadiantProportion, internalGain.lightingRadProp);
                result.SetValue(InternalConditionParameter.OccupancyRadiantProportion, internalGain.occupantRadProp);
                result.SetValue(InternalConditionParameter.EquipmentRadiantProportion, internalGain.equipmentRadProp);

                result.SetValue(InternalConditionParameter.LightingViewCoefficient, internalGain.lightingViewCoefficient);
                result.SetValue(InternalConditionParameter.OccupancyViewCoefficient, internalGain.occupantViewCoefficient);
                result.SetValue(InternalConditionParameter.EquipmentViewCoefficient, internalGain.equipmentViewCoefficient);

                TBD.profile profile_TBD = null;
                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticI);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.InfiltrationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                    result.SetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, profile_TBD.GetExtremeValue(true));
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticLG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.LightingProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                    result.SetValue(InternalConditionParameter.LightingGainPerArea, profile_TBD.GetExtremeValue(true));
                    result.SetValue(InternalConditionParameter.LightingLevel, internalGain.targetIlluminance);
                }

                double personGain = internalGain.personGain;
                double gain = 0;

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOSG);
                if (profile_TBD != null)
                {
                    double value = profile_TBD.GetExtremeValue(true);
                    result.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, value);
                    gain += value;
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                if (profile_TBD != null)
                {
                    double value = profile_TBD.GetExtremeValue(true);
                    result.SetValue(InternalConditionParameter.OccupancyProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                    result.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, value);
                    gain += value;
                }

                if(!double.IsNaN(area) && !double.IsNaN(gain))
                {
                    double occupancy = (gain * area) / personGain;
                    result.SetValue(InternalConditionParameter.AreaPerPerson, area / occupancy);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticESG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentSensibleProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                    result.SetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, profile_TBD.GetExtremeValue(true));
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticELG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentLatentProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                    result.SetValue(InternalConditionParameter.EquipmentLatentGainPerArea, profile_TBD.GetExtremeValue(true));
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticCOG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.PollutantProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                    result.SetValue(InternalConditionParameter.PollutantGenerationPerArea, profile_TBD.GetExtremeValue(true));
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticV);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.VentilationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                    result.SetValue(InternalConditionParameter.SupplyAirFlow, profile_TBD.GetExtremeValue(true));
                }
            }

            TBD.Thermostat thermostat = internalCondition.GetThermostat();
            if (internalGain != null)
            {
                TBD.profile profile_TBD = null;

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticUL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.CoolingProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticLL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.HeatingProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHLL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.HumidificationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHUL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.DehumidificationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TBD.name));
                }
            }

            return result;
        }

        public static InternalCondition ToSAM(this TIC.InternalCondition internalCondition, double area = double.NaN)
        {
            if(internalCondition == null)
            {
                return null;
            }

            InternalCondition result = new InternalCondition(internalCondition.name);

            TIC.InternalGain internalGain = internalCondition.GetInternalGain();
            if (internalGain != null)
            {
                result.SetValue(InternalConditionParameter.LightingRadiantProportion, internalGain.lightingRadProp);
                result.SetValue(InternalConditionParameter.OccupancyRadiantProportion, internalGain.occupantRadProp);
                result.SetValue(InternalConditionParameter.EquipmentRadiantProportion, internalGain.equipmentRadProp);

                result.SetValue(InternalConditionParameter.LightingViewCoefficient, internalGain.lightingViewCoefficient);
                result.SetValue(InternalConditionParameter.OccupancyViewCoefficient, internalGain.occupantViewCoefficient);
                result.SetValue(InternalConditionParameter.EquipmentViewCoefficient, internalGain.equipmentViewCoefficient);

                TIC.profile profile_TIC = null;
                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticI);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.InfiltrationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, profile_TIC.GetExtremeValue(true));
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticLG);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.LightingProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.LightingGainPerArea, profile_TIC.GetExtremeValue(true));
                    result.SetValue(InternalConditionParameter.LightingLevel, internalGain.targetIlluminance);
                }

                double personGain = internalGain.personGain;
                double gain = 0;

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticOSG);
                if (profile_TIC != null)
                {
                    double value = profile_TIC.GetExtremeValue(true);
                    result.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, value);
                    gain += value;
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                if (profile_TIC != null)
                {
                    double value = profile_TIC.GetExtremeValue(true);
                    result.SetValue(InternalConditionParameter.OccupancyProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, value);
                    gain += value;
                }

                if (!double.IsNaN(area) && !double.IsNaN(gain))
                {
                    double occupancy = (gain * area) / personGain;
                    result.SetValue(InternalConditionParameter.AreaPerPerson, area / occupancy);
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticESG);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentSensibleProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, profile_TIC.GetExtremeValue(true));
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticELG);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentLatentProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.EquipmentLatentGainPerArea, profile_TIC.GetExtremeValue(true));
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticCOG);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.PollutantProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.PollutantGenerationPerArea, profile_TIC.GetExtremeValue(true));
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticV);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.VentilationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.SupplyAirFlow, profile_TIC.GetExtremeValue(true));
                }

            }

            TIC.Thermostat thermostat = internalCondition.GetThermostat();
            if (internalGain != null)
            {
                TIC.profile profile_TIC = null;

                profile_TIC = thermostat.GetProfile((int)TBD.Profiles.ticUL);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.CoolingProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                }

                profile_TIC = thermostat.GetProfile((int)TBD.Profiles.ticLL);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.HeatingProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                }

                profile_TIC = thermostat.GetProfile((int)TBD.Profiles.ticHLL);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.HumidificationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                }

                profile_TIC = thermostat.GetProfile((int)TBD.Profiles.ticHUL);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.DehumidificationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                }
            }

            return result;
        }
    }
}
