// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Writes a SAM <see cref="InternalCondition"/> into a TBD internal condition <b>without</b>
        /// an owning <see cref="Space"/>.
        /// <para/>
        /// The space-based overload (<see cref="UpdateInternalCondition(TBD.InternalCondition, Space, ProfileLibrary, AdjacencyCluster)"/>)
        /// derives every gain from the space's area/volume/occupancy. A library/template condition
        /// has no space, so the gains are taken straight from the condition's own stored per-area
        /// parameters instead — the same values the import read off the TBD profiles. This is what
        /// lets unused internal conditions (imported via
        /// <see cref="AddUnusedInternalConditions(AdjacencyCluster, TBD.Building, ProfileReuseIndex)"/>) round-trip back
        /// out on export.
        /// <para/>
        /// FIDELITY NOTE: <c>personGain</c> (metabolic rate, W/p) is not stored on the SAM condition —
        /// the import only uses it transiently to derive area-per-person — so it is written as 0 here.
        /// That is immaterial for an unassigned library condition (nothing simulates it), but it means
        /// this is a library-completeness round-trip, not a bit-exact one for occupancy density.
        /// </summary>
        public static bool UpdateInternalCondition(this TBD.InternalCondition internalCondition_TBD, InternalCondition internalCondition, ProfileLibrary profileLibrary)
        {
            if (internalCondition_TBD == null || internalCondition == null)
            {
                return false;
            }

            // Restore the captured description (e.g. NCM activity name); fall back to the name.
            string description = internalCondition.Name;
            if (internalCondition.TryGetValue(InternalConditionParameter.Description, out string description_Temp) && !string.IsNullOrWhiteSpace(description_Temp))
            {
                description = description_Temp;
            }

            internalCondition_TBD.name = internalCondition.Name;
            internalCondition_TBD.description = description;
            internalCondition_TBD.includeSolarInMRT = 1;

            double value = double.NaN;

            TBD.Emitter emitter = internalCondition_TBD.GetHeatingEmitter();
            if (emitter != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmitterRadiantProportion, out value) && !double.IsNaN(value))
                {
                    emitter.radiantProportion = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmitterCoefficient, out value) && !double.IsNaN(value))
                {
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
                }
            }

            emitter = internalCondition_TBD.GetCoolingEmitter();
            if (emitter != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmitterRadiantProportion, out value) && !double.IsNaN(value))
                {
                    emitter.radiantProportion = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmitterCoefficient, out value) && !double.IsNaN(value))
                {
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
                }
            }

            TBD.InternalGain internalGain = internalCondition_TBD.GetInternalGain();
            if (internalGain != null)
            {
                if (internalCondition.TryGetValue(InternalConditionParameter.LightingRadiantProportion, out value) && !double.IsNaN(value))
                {
                    internalGain.lightingRadProp = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyRadiantProportion, out value) && !double.IsNaN(value))
                {
                    internalGain.occupantRadProp = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentRadiantProportion, out value) && !double.IsNaN(value))
                {
                    internalGain.equipmentRadProp = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.LightingViewCoefficient, out value) && !double.IsNaN(value))
                {
                    internalGain.lightingViewCoefficient = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyViewCoefficient, out value) && !double.IsNaN(value))
                {
                    internalGain.occupantViewCoefficient = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentViewCoefficient, out value) && !double.IsNaN(value))
                {
                    internalGain.equipmentViewCoefficient = System.Convert.ToSingle(value);
                }

                internalGain.domesticHotWater = (float)0.197;
                internalGain.name = description;

                // No space, so no per-person metabolic rate to recover — see fidelity note.
                internalGain.personGain = 0;

                if (internalCondition.TryGetValue(InternalConditionParameter.LightingLevel, out value) && !double.IsNaN(value))
                {
                    internalGain.targetIlluminance = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlowPerPerson, out value) && !double.IsNaN(value))
                {
                    internalGain.freshAirRate = (float)value * 1000;
                }

                UpdateProfile(internalGain, (int)TBD.Profiles.ticI, internalCondition, ProfileType.Infiltration, profileLibrary, InternalConditionParameter.InfiltrationAirChangesPerHour);
                UpdateProfile(internalGain, (int)TBD.Profiles.ticLG, internalCondition, ProfileType.Lighting, profileLibrary, InternalConditionParameter.LightingGainPerArea);
                UpdateProfile(internalGain, (int)TBD.Profiles.ticOSG, internalCondition, ProfileType.Occupancy, profileLibrary, InternalConditionParameter.OccupancySensibleGainPerPerson);
                UpdateProfile(internalGain, (int)TBD.Profiles.ticOLG, internalCondition, ProfileType.Occupancy, profileLibrary, InternalConditionParameter.OccupancyLatentGainPerPerson);
                UpdateProfile(internalGain, (int)TBD.Profiles.ticESG, internalCondition, ProfileType.EquipmentSensible, profileLibrary, InternalConditionParameter.EquipmentSensibleGainPerArea);
                UpdateProfile(internalGain, (int)TBD.Profiles.ticELG, internalCondition, ProfileType.EquipmentLatent, profileLibrary, InternalConditionParameter.EquipmentLatentGainPerArea);
                UpdateProfile(internalGain, (int)TBD.Profiles.ticCOG, internalCondition, ProfileType.Pollutant, profileLibrary, InternalConditionParameter.PollutantGenerationPerArea);
                // ticV is an ACH rate, and a template has no space, so there is no volume to convert
                // through: whatever parameter is read here becomes the factor verbatim. Prefer the ACH
                // basis - that is what the TBD import now writes, and it is the only one dimensionally
                // meaningful without a volume. Fall back to SupplyAirFlow so a SAM-authored template that
                // only carries the legacy m3/s parameter keeps the factor it has always been given.
                InternalConditionParameter internalConditionParameter_Ventilation = InternalConditionParameter.SupplyAirFlow;
                if (internalCondition.TryGetValue(InternalConditionParameter.SupplyAirChangesPerHour, out double airChangesPerHour) && !double.IsNaN(airChangesPerHour))
                {
                    internalConditionParameter_Ventilation = InternalConditionParameter.SupplyAirChangesPerHour;
                }

                UpdateProfile(internalGain, (int)TBD.Profiles.ticV, internalCondition, ProfileType.Ventilation, profileLibrary, internalConditionParameter_Ventilation);
            }

            TBD.Thermostat thermostat = internalCondition_TBD.GetThermostat();
            if (thermostat != null)
            {
                List<string> names = new List<string>();

                thermostat.controlRange = 0;
                thermostat.proportionalControl = 0;

                // Setpoint profiles carry no gain magnitude — written with factor 1, as the space path does.
                names.Add(UpdateThermostatProfile(thermostat, (int)TBD.Profiles.ticUL, internalCondition, ProfileType.Cooling, profileLibrary));
                names.Add(UpdateThermostatProfile(thermostat, (int)TBD.Profiles.ticLL, internalCondition, ProfileType.Heating, profileLibrary));
                names.Add(UpdateThermostatProfile(thermostat, (int)TBD.Profiles.ticHLL, internalCondition, ProfileType.Humidification, profileLibrary));
                names.Add(UpdateThermostatProfile(thermostat, (int)TBD.Profiles.ticHUL, internalCondition, ProfileType.Dehumidification, profileLibrary));

                names.RemoveAll(x => string.IsNullOrWhiteSpace(x));
                if (names.Count != 0)
                {
                    thermostat.name = string.Join(" & ", names);
                }
            }

            return true;
        }

        /// <summary>
        /// Adds a SAM <see cref="InternalCondition"/> to <paramref name="building"/> as a new TBD
        /// internal condition, with no owning space. Companion to the space-based
        /// <see cref="AddInternalCondition(TBD.Building, Space, ProfileLibrary, AdjacencyCluster)"/>.
        /// </summary>
        public static TBD.InternalCondition AddInternalCondition(this TBD.Building building, InternalCondition internalCondition, ProfileLibrary profileLibrary, IEnumerable<TBD.dayType> dayTypes_NonHDD = null)
        {
            if (building == null || internalCondition == null || profileLibrary == null)
            {
                return null;
            }

            TBD.InternalCondition internalCondition_TBD = building.AddIC(null);
            if (internalCondition_TBD == null)
            {
                return null;
            }

            IEnumerable<TBD.dayType> dayTypes = dayTypes_NonHDD;
            if (dayTypes == null)
            {
                List<TBD.dayType> dayTypes_Temp = building.DayTypes();
                if (dayTypes_Temp != null)
                {
                    dayTypes_Temp.RemoveAll(x => x.name.Equals("HDD"));
                    dayTypes = dayTypes_Temp;
                }
            }

            if (dayTypes != null)
            {
                foreach (TBD.dayType dayType in dayTypes)
                {
                    internalCondition_TBD.SetDayType(dayType, true);
                }
            }

            UpdateInternalCondition(internalCondition_TBD, internalCondition, profileLibrary);

            return internalCondition_TBD;
        }

        /// <summary>
        /// Writes the unused (library) internal conditions held in <paramref name="adjacencyCluster"/>
        /// into <paramref name="building"/>, skipping any name already present (a space's condition or
        /// a pre-existing one). The export-side counterpart to
        /// <see cref="AddUnusedInternalConditions(AdjacencyCluster, TBD.Building, ProfileReuseIndex)"/>.
        /// <para/>
        /// Only conditions with <b>no space relation</b> are written. The import stores a zone's
        /// HDD/CDD companion conditions as cluster objects too, but those are related to a space and
        /// handled by the per-space export (<see cref="UpdateZone_HDD(TBD.Building, TBD.zone, Space, ProfileLibrary)"/>);
        /// writing them here would duplicate them. A genuine library condition (the kind
        /// <see cref="AddUnusedInternalConditions(AdjacencyCluster, TBD.Building, ProfileReuseIndex)"/> adds on import)
        /// has no space relation, so this stays a no-op unless unused conditions were imported.
        /// </summary>
        public static List<TBD.InternalCondition> AddUnusedInternalConditions(this TBD.Building building, AdjacencyCluster adjacencyCluster, ProfileLibrary profileLibrary, IEnumerable<TBD.dayType> dayTypes_NonHDD = null)
        {
            if (building == null || adjacencyCluster == null || profileLibrary == null)
            {
                return null;
            }

            List<InternalCondition> internalConditions = new List<InternalCondition>();
            IEnumerable<InternalCondition> internalConditions_Template = adjacencyCluster.GetInternalConditions(false, true);
            if (internalConditions_Template != null)
            {
                foreach (InternalCondition internalCondition in internalConditions_Template)
                {
                    if (internalCondition == null || string.IsNullOrWhiteSpace(internalCondition.Name))
                    {
                        continue;
                    }

                    // Skip conditions tied to a space (e.g. HDD/CDD companions) — the per-space
                    // export already writes those; only genuinely unassigned library conditions belong here.
                    List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(internalCondition);
                    if (spaces != null && spaces.Count != 0)
                    {
                        continue;
                    }

                    internalConditions.Add(internalCondition);
                }
            }

            if (internalConditions.Count == 0)
            {
                return null;
            }

            // Names already in the building (e.g. the per-space conditions UpdateZones just wrote);
            // never duplicate one of those.
            HashSet<string> existingNames = new HashSet<string>();
            List<TBD.InternalCondition> internalConditions_TBD_Existing = Query.InternalConditions(building);
            if (internalConditions_TBD_Existing != null)
            {
                foreach (TBD.InternalCondition internalCondition_TBD in internalConditions_TBD_Existing)
                {
                    if (!string.IsNullOrWhiteSpace(internalCondition_TBD?.name))
                    {
                        existingNames.Add(internalCondition_TBD.name);
                    }
                }
            }

            List<TBD.InternalCondition> result = new List<TBD.InternalCondition>();
            foreach (InternalCondition internalCondition in internalConditions)
            {
                if (existingNames.Contains(internalCondition.Name))
                {
                    continue;
                }

                TBD.InternalCondition internalCondition_TBD = building.AddInternalCondition(internalCondition, profileLibrary, dayTypes_NonHDD);
                if (internalCondition_TBD != null)
                {
                    existingNames.Add(internalCondition.Name);
                    result.Add(internalCondition_TBD);
                }
            }

            return result;
        }

        // Writes one internalGain profile from the condition's stored per-area value (used as the
        // TBD profile factor); the SAM Profile supplies the normalised shape. Missing value -> 0.
        private static void UpdateProfile(TBD.InternalGain internalGain, int profileIndex, InternalCondition internalCondition, ProfileType profileType, ProfileLibrary profileLibrary, InternalConditionParameter parameter)
        {
            Profile profile = internalCondition.GetProfile(profileType, profileLibrary);
            if (profile == null)
            {
                return;
            }

            TBD.profile profile_TBD = internalGain.GetProfile(profileIndex);
            if (profile_TBD == null)
            {
                return;
            }

            if (!internalCondition.TryGetValue(parameter, out double value) || double.IsNaN(value))
            {
                value = 0;
            }

            Update(profile_TBD, profile, value);
        }

        // Writes one thermostat setpoint profile (factor 1, no gain magnitude); returns the profile
        // name for the composite thermostat name, or null if absent.
        private static string UpdateThermostatProfile(TBD.Thermostat thermostat, int profileIndex, InternalCondition internalCondition, ProfileType profileType, ProfileLibrary profileLibrary)
        {
            Profile profile = internalCondition.GetProfile(profileType, profileLibrary);
            if (profile == null)
            {
                return null;
            }

            TBD.profile profile_TBD = thermostat.GetProfile(profileIndex);
            if (profile_TBD != null)
            {
                Update(profile_TBD, profile, 1);
            }

            return profile.Name;
        }
    }
}
