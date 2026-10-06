// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        /// <summary>
        /// Backwards-compatible overload - forwards to the <c>ProfileReuseIndex</c> overload with no index, so
        /// callers compiled against the previous <c>ToSAM(TBD.InternalCondition, double)</c> signature keep
        /// working. An added optional parameter changes that signature's arity and breaks them at runtime.
        /// </summary>
        public static InternalCondition ToSAM(this TBD.InternalCondition internalCondition, double area = double.NaN)
        {
            return ToSAM(internalCondition, area, null);
        }

        /// <summary>
        /// Import one TBD internal condition.
        /// </summary>
        /// <param name="internalCondition">The TBD internal condition to import.</param>
        /// <param name="area">The owning space's floor area, or NaN when the condition owns no space - the per-area gains are then kept raw.</param>
        /// <param name="profileReuseIndex">
        /// The conversion-wide reuse index, or null.
        /// <para>
        /// With an index, every profile reference this writes is the CANONICAL name of the shared
        /// <c>ProfileLibrary</c> definition behind the slot, so two zones carrying the same activity reference
        /// one profile instead of two copies of it. Without one, the legacy
        /// <c>"{internal condition} [{profile}]"</c> reference is written, exactly as before - which is what
        /// callers that build no library, or build the legacy one, still need.
        /// </para>
        /// <para>
        /// The index MUST be the same instance the model's <c>ProfileLibrary</c> was built from
        /// (<see cref="ToSAM_ProfileLibrary(TBD.Building, ProfileReuseIndex)"/>), or these references name
        /// nothing.
        /// </para>
        /// </param>
        public static InternalCondition ToSAM(this TBD.InternalCondition internalCondition, double area, ProfileReuseIndex profileReuseIndex)
        {
            if (internalCondition == null)
            {
                return null;
            }

            InternalCondition result = new InternalCondition(internalCondition.name);

            // Preserve the TBD internal-condition description (e.g. the NCM activity "S37_OfficeCell").
            // The SAM name carries the instance name ("Cell 1"); without this the description is lost
            // and the export rebuilds it from the name on round-trip.
            string description = internalCondition.description;
            if (!string.IsNullOrWhiteSpace(description))
            {
                result.SetValue(InternalConditionParameter.Description, description);
            }

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
                    result.SetValue(InternalConditionParameter.InfiltrationProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticI, ProfileType.Infiltration, profile_TBD));
                    result.SetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, Query.GainMagnitude(profile_TBD));
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticLG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.LightingProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticLG, ProfileType.Lighting, profile_TBD));
                    result.SetValue(InternalConditionParameter.LightingGainPerArea, Query.GainMagnitude(profile_TBD));
                    result.SetValue(InternalConditionParameter.LightingLevel, internalGain.targetIlluminance);
                }

                double personGain = internalGain.personGain;

                // TBD occupancy sensible/latent gains are per floor-area (W/m2); personGain is the
                // per-person metabolic rate (W/p). Read the per-area gains, derive the occupancy from
                // them + personGain, then store the gains PER PERSON (perArea * areaPerPerson) so a
                // round-trip reproduces the original per-area gains AND the metabolic rate (the
                // per-person sensible+latent sums back to personGain).
                //
                // The per-area gains MUST be the profile factors (Query.GainMagnitude), not the profile
                // extremes. This block is scale-invariant in the gains themselves - the per-person split
                // is a RATIO, so it survives any common factor - but the OCCUPANCY it derives is not:
                // areaPerPerson = personGain / gainPerArea, so an under-read gainPerArea inflates
                // areaPerPerson, thins the occupancy, and the next export writes an equally thinned gain.
                // With the extreme (factor * max(values)) that made occupancy decay by the schedule's
                // peak on EVERY generation, without bound. See Query.GainMagnitude.
                double sensiblePerArea = double.NaN;
                double latentPerArea = double.NaN;

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOSG);
                if (profile_TBD != null)
                {
                    sensiblePerArea = Query.GainMagnitude(profile_TBD);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                if (profile_TBD != null)
                {
                    latentPerArea = Query.GainMagnitude(profile_TBD);
                    result.SetValue(InternalConditionParameter.OccupancyProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticOLG, ProfileType.Occupancy, profile_TBD));
                }

                double gainPerArea = (double.IsNaN(sensiblePerArea) ? 0 : sensiblePerArea) + (double.IsNaN(latentPerArea) ? 0 : latentPerArea);
                if (!double.IsNaN(area) && !double.IsNaN(personGain) && personGain > 0 && gainPerArea > 0)
                {
                    double occupancy = (gainPerArea * area) / personGain;
                    double areaPerPerson = occupancy > 0 ? area / occupancy : double.NaN;   // == personGain / gainPerArea
                    if (!double.IsNaN(areaPerPerson))
                    {
                        result.SetValue(InternalConditionParameter.AreaPerPerson, areaPerPerson);

                        if (!double.IsNaN(sensiblePerArea))
                        {
                            result.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, sensiblePerArea * areaPerPerson);
                        }

                        if (!double.IsNaN(latentPerArea))
                        {
                            result.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, latentPerArea * areaPerPerson);
                        }
                    }
                }
                else
                {
                    // No usable personGain/area — keep the raw per-area values rather than nothing.
                    if (!double.IsNaN(sensiblePerArea))
                    {
                        result.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, sensiblePerArea);
                    }
                    if (!double.IsNaN(latentPerArea))
                    {
                        result.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, latentPerArea);
                    }
                }

                // Outside air per person. TBD freshAirRate is l/s/p; SAM stores SupplyAirFlowPerPerson
                // in m3/s/p (export multiplies back by 1000). Without this the import kept SAM's default
                // (8 l/s/p) and dropped the source value (e.g. 40).
                if (!float.IsNaN(internalGain.freshAirRate))
                {
                    result.SetValue(InternalConditionParameter.SupplyAirFlowPerPerson, internalGain.freshAirRate / 1000.0);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticESG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentSensibleProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticESG, ProfileType.EquipmentSensible, profile_TBD));
                    result.SetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, Query.GainMagnitude(profile_TBD));
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticELG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentLatentProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticELG, ProfileType.EquipmentLatent, profile_TBD));
                    result.SetValue(InternalConditionParameter.EquipmentLatentGainPerArea, Query.GainMagnitude(profile_TBD));
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticCOG);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.PollutantProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticCOG, ProfileType.Pollutant, profile_TBD));
                    result.SetValue(InternalConditionParameter.PollutantGenerationPerArea, Query.GainMagnitude(profile_TBD));
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticV);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.VentilationProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticV, ProfileType.Ventilation, profile_TBD));

                    //ticV is an AIR CHANGE RATE, and it is the profile's FACTOR that carries it - the values
                    //are the schedule the factor scales. Two things follow, both learned the hard way:
                    //
                    //1. It belongs on the [ACH] basis, not SupplyAirFlow [m3/s]. Storing an ACH in the m3/s
                    //   parameter made Query.CalculatedSupplyAirFlow read it as m3/s and the export's own
                    //   "/ volume * 3600" inflate it by 3600/volume - a licensed round trip turned a 2.0 ACH
                    //   source profile into 40.8 ACH on a 200 m3 zone.
                    //2. It must be the factor itself, NOT GetExtremeValue(true) (= factor * max(values)). The
                    //   imported Profile keeps the RAW values, and Modify.Update writes the basis back as
                    //   profile_TBD.factor and re-applies those same values - so storing the peak scales the
                    //   schedule twice, by factor * max^2. Invisible whenever the profile is normalised to a
                    //   peak of 1 (the usual TAS convention, and what the first authored oracle used), but a
                    //   factor of 2 with a peak value of 0.5 round-trips 1.0 ACH as 0.5 ACH.
                    //   Point 2 was later found to hold for EVERY gain slot, not just this one, and is now
                    //   stated once in Query.GainMagnitude - which this line is the original instance of.
                    //
                    //Both were harmless while the ventilation reference dangled; both went live the moment
                    //ticV became a collected, resolvable profile.
                    result.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, profile_TBD.factor);
                }
            }

            TBD.Thermostat thermostat = internalCondition.GetThermostat();
            if (internalGain != null)
            {
                TBD.profile profile_TBD = null;

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticUL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.CoolingProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticUL, ProfileType.Cooling, profile_TBD));
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticLL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.HeatingProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticLL, ProfileType.Heating, profile_TBD));
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHLL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.HumidificationProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticHLL, ProfileType.Humidification, profile_TBD));
                }

                profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHUL);
                if (profile_TBD != null)
                {
                    result.SetValue(InternalConditionParameter.DehumidificationProfileName, ProfileName(profileReuseIndex, internalCondition.name, TBD.Profiles.ticHUL, ProfileType.Dehumidification, profile_TBD));
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
                    result.SetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, Query.GainMagnitude(profile_TIC));
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticLG);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.LightingProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.LightingGainPerArea, Query.GainMagnitude(profile_TIC));
                    result.SetValue(InternalConditionParameter.LightingLevel, internalGain.targetIlluminance);
                }

                double personGain = internalGain.personGain;

                // TBD occupancy sensible/latent gains are per floor-area (W/m2); personGain is the
                // per-person metabolic rate (W/p). Read the per-area gains, derive the occupancy from
                // them + personGain, then store the gains PER PERSON (perArea * areaPerPerson) so a
                // round-trip reproduces the original per-area gains AND the metabolic rate.
                // Magnitudes come from the profile factor, never its extreme - see Query.GainMagnitude.
                double sensiblePerArea = double.NaN;
                double latentPerArea = double.NaN;

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticOSG);
                if (profile_TIC != null)
                {
                    sensiblePerArea = Query.GainMagnitude(profile_TIC);
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                if (profile_TIC != null)
                {
                    latentPerArea = Query.GainMagnitude(profile_TIC);
                    result.SetValue(InternalConditionParameter.OccupancyProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                }

                double gainPerArea = (double.IsNaN(sensiblePerArea) ? 0 : sensiblePerArea) + (double.IsNaN(latentPerArea) ? 0 : latentPerArea);
                if (!double.IsNaN(area) && !double.IsNaN(personGain) && personGain > 0 && gainPerArea > 0)
                {
                    double occupancy = (gainPerArea * area) / personGain;
                    double areaPerPerson = occupancy > 0 ? area / occupancy : double.NaN;   // == personGain / gainPerArea
                    if (!double.IsNaN(areaPerPerson))
                    {
                        result.SetValue(InternalConditionParameter.AreaPerPerson, areaPerPerson);

                        if (!double.IsNaN(sensiblePerArea))
                        {
                            result.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, sensiblePerArea * areaPerPerson);
                        }

                        if (!double.IsNaN(latentPerArea))
                        {
                            result.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, latentPerArea * areaPerPerson);
                        }
                    }
                }
                else
                {
                    // No usable personGain/area — keep the raw per-area values rather than nothing.
                    if (!double.IsNaN(sensiblePerArea))
                    {
                        result.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, sensiblePerArea);
                    }
                    if (!double.IsNaN(latentPerArea))
                    {
                        result.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, latentPerArea);
                    }
                }

                // Outside air per person. TBD freshAirRate is l/s/p; SAM stores SupplyAirFlowPerPerson
                // in m3/s/p (export multiplies back by 1000).
                if (!float.IsNaN(internalGain.freshAirRate))
                {
                    result.SetValue(InternalConditionParameter.SupplyAirFlowPerPerson, internalGain.freshAirRate / 1000.0);
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticESG);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentSensibleProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, Query.GainMagnitude(profile_TIC));
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticELG);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.EquipmentLatentProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.EquipmentLatentGainPerArea, Query.GainMagnitude(profile_TIC));
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticCOG);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.PollutantProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));
                    result.SetValue(InternalConditionParameter.PollutantGenerationPerArea, Query.GainMagnitude(profile_TIC));
                }

                profile_TIC = internalGain.GetProfile((int)TBD.Profiles.ticV);
                if (profile_TIC != null)
                {
                    result.SetValue(InternalConditionParameter.VentilationProfileName, string.Format("{0} [{1}]", internalCondition.name, profile_TIC.name));

                    //Same rule as the TBD overload above: the ticV FACTOR is the ACH rate, and it is not a
                    //volume flow.
                    result.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, profile_TIC.factor);
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

        /// <summary>
        /// The profile reference one TBD internal-condition slot writes.
        /// <para>
        /// With no index this is the legacy <c>"{internal condition} [{profile}]"</c> name. With one it is the
        /// canonical name of the shared library definition, answered from the slot map so no value is re-read
        /// over COM - and, if that map cannot answer (two TBD internal conditions share a name and disagree on
        /// this slot), from the definition itself, which always can. Only a slot the index never collected at all
        /// falls back to the legacy name.
        /// </para>
        /// </summary>
        private static string ProfileName(ProfileReuseIndex profileReuseIndex, string internalConditionName, TBD.Profiles slot, ProfileType profileType, TBD.profile profile_TBD)
        {
            string name_Legacy = Query.ProfileName_Legacy(internalConditionName, profile_TBD?.name);

            if (profileReuseIndex == null || profile_TBD == null)
            {
                return name_Legacy;
            }

            string result = profileReuseIndex.GetProfileName(internalConditionName, (int)slot);
            if (result != null)
            {
                return result;
            }

            result = profileReuseIndex.GetProfileName(Analytical.Query.Text(profileType), Core.Tas.Query.Values(profile_TBD));

            return result ?? name_Legacy;
        }
    }
}
