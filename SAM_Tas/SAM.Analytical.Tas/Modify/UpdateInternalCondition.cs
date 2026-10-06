// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateInternalCondition(this TBD.InternalCondition internalCondition_TBD, Space space, ProfileLibrary profileLibrary, AdjacencyCluster adjacencyCluster = null)
        {
            if (space == null || internalCondition_TBD == null)
                return false;

            InternalCondition internalCondition = space.InternalCondition;
            if (internalCondition == null)
                return false;

            // Restore the original internal-condition description (e.g. NCM activity "S37_OfficeCell")
            // captured on import; fall back to the name for models that never carried one.
            string description = internalCondition.Name;
            if (internalCondition.TryGetValue(InternalConditionParameter.Description, out string description_Temp) && !string.IsNullOrWhiteSpace(description_Temp))
            {
                description = description_Temp;
            }

            internalCondition_TBD.name = space.Name;
            internalCondition_TBD.description = description;

            internalCondition_TBD.includeSolarInMRT = 1;

            TBD.Emitter emitter = null;
            double value = double.NaN;

            HeatingSystem heatingSystem = adjacencyCluster?.MechanicalSystems<HeatingSystem>(space)?.FirstOrDefault();
            
            emitter = internalCondition_TBD.GetHeatingEmitter();
            if(emitter != null)
            {
                emitter.name = internalCondition.GetSystemTypeName<HeatingSystemType>();

                HeatingSystemType heatingSystemType = heatingSystem?.Type as HeatingSystemType;
                if(heatingSystemType != null && heatingSystemType.TryGetValue(HeatingSystemTypeParameter.RadiantProportion, out value) && !double.IsNaN(value))
                {
                    emitter.radiantProportion = System.Convert.ToSingle(value);
                }
                
                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmitterRadiantProportion, out value) && !double.IsNaN(value))
                {
                    emitter.radiantProportion = System.Convert.ToSingle(value);
                }

                if (heatingSystemType != null && heatingSystemType.TryGetValue(HeatingSystemTypeParameter.ViewCoefficient, out value) && !double.IsNaN(value))
                {
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingEmitterCoefficient, out value) && !double.IsNaN(value))
                {
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
                }
            }

            CoolingSystem coolingSystem = adjacencyCluster?.MechanicalSystems<CoolingSystem>(space)?.FirstOrDefault();

            emitter = internalCondition_TBD.GetCoolingEmitter();
            if (emitter != null)
            {
                emitter.name = internalCondition.GetSystemTypeName<CoolingSystemType>();

                CoolingSystemType coolingSystemType = coolingSystem?.Type as CoolingSystemType;
                if (coolingSystemType != null && coolingSystemType.TryGetValue(CoolingSystemTypeParameter.RadiantProportion, out value) && !double.IsNaN(value))
                {
                    emitter.radiantProportion = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmitterRadiantProportion, out value))
                {
                    emitter.radiantProportion = System.Convert.ToSingle(value);
                }

                if (coolingSystemType != null && coolingSystemType.TryGetValue(CoolingSystemTypeParameter.ViewCoefficient, out value) && !double.IsNaN(value))
                {
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingEmitterCoefficient, out value))
                {
                    emitter.viewCoefficient = System.Convert.ToSingle(value);
                }
            }

            TBD.InternalGain internalGain = internalCondition_TBD.GetInternalGain();

            if(internalCondition.TryGetValue(InternalConditionParameter.LightingRadiantProportion, out value))
            {
                internalGain.lightingRadProp = System.Convert.ToSingle(value);
            }

            if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyRadiantProportion, out value))
            {
                internalGain.occupantRadProp = System.Convert.ToSingle(value);
            }

            if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentRadiantProportion, out value))
            {
                internalGain.equipmentRadProp = System.Convert.ToSingle(value);
            }

            if (internalCondition.TryGetValue(InternalConditionParameter.LightingViewCoefficient, out value))
            {
                internalGain.lightingViewCoefficient = System.Convert.ToSingle(value);
            }

            if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyViewCoefficient, out value))
            {
                internalGain.occupantViewCoefficient = System.Convert.ToSingle(value);
            }

            if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentViewCoefficient, out value))
            {
                internalGain.equipmentViewCoefficient = System.Convert.ToSingle(value);
            }

            internalGain.domesticHotWater = (float)0.197;

            internalGain.name = description;
            internalGain.description = internalCondition.GetSystemTypeName<VentilationSystemType>();

            if (internalCondition.TryGetValue(InternalConditionParameter.LightingLevel, out value))
                internalGain.targetIlluminance = System.Convert.ToSingle(value);

            internalGain.personGain = 0;
            double occupancyGainPerPerson = Analytical.Query.OccupancyGainPerPerson(space);
            if (!double.IsNaN(occupancyGainPerPerson))
                internalGain.personGain = System.Convert.ToSingle(occupancyGainPerPerson);

            if (internalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlowPerPerson, out double supplyAirFlowPerPerson) && !double.IsNaN(supplyAirFlowPerPerson))
            {
                internalGain.freshAirRate = (float)supplyAirFlowPerPerson * 1000;
            }

            Profile profile = null;

            TBD.profile profile_TBD;


            profile = internalCondition.GetProfile(ProfileType.Infiltration, profileLibrary);
            if (profile != null)
            {
                if(internalCondition.TryGetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, out value))
                {
                    profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticI);
                    if (profile_TBD != null)
                        Update(profile_TBD, profile, value);
                }
            }

            double area = double.NaN;
            if (!space.TryGetValue(Analytical.SpaceParameter.Area, out area))
                area = double.NaN;

            profile = internalCondition.GetProfile(ProfileType.Lighting, profileLibrary);
            profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticLG);
            if (profile_TBD != null)
            {
                double gain = Analytical.Query.CalculatedLightingGain(space);
                if (double.IsNaN(area) || area == 0 || double.IsNaN(gain))
                    gain = 0;
                else
                    gain = gain / area;

                Update(profile_TBD, profile, gain);

                if (internalCondition.TryGetValue(InternalConditionParameter.LightingControlFunction, out string lightingControlFunction) && !string.IsNullOrWhiteSpace(lightingControlFunction))
                {
                    if (profile_TBD.type == TBD.ProfileTypes.ticHourlyProfile)
                    {
                        profile_TBD.type = TBD.ProfileTypes.ticHourlyFunctionProfile;
                    }
                    else if (profile_TBD.type == TBD.ProfileTypes.ticYearlyProfile)
                    {
                        profile_TBD.type = TBD.ProfileTypes.ticYearlyFunctionProfile;
                    }
                    else
                    {
                        profile_TBD.type = TBD.ProfileTypes.ticFunctionProfile;
                    }

                    profile_TBD.function = lightingControlFunction;
                }
            }

            profile = internalCondition.GetProfile(ProfileType.Occupancy, profileLibrary);
            if (profile != null)
            {
                profile_TBD = null;

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOLG);
                if (profile_TBD != null)
                {
                    double gain = Analytical.Query.OccupancyLatentGain(space);
                    if (double.IsNaN(area) || area == 0 || double.IsNaN(gain))
                        gain = 0;
                    else
                        gain = gain / area;

                    Update(profile_TBD, profile, gain);
                }

                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticOSG);
                if (profile_TBD != null)
                {
                    double gain = Analytical.Query.OccupancySensibleGain(space);
                    if (double.IsNaN(area) || area == 0 || double.IsNaN(gain))
                        gain = 0;
                    else
                        gain = gain / area;

                    Update(profile_TBD, profile, gain);
                }
            }

            profile = internalCondition.GetProfile(ProfileType.EquipmentSensible, profileLibrary);
            if (profile != null)
            {
                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticESG);
                if (profile_TBD != null)
                {
                    double gain = Analytical.Query.CalculatedEquipmentSensibleGain(space);
                    if (double.IsNaN(area) || area == 0 || double.IsNaN(gain))
                        gain = 0;
                    else
                        gain = gain / area;

                    Update(profile_TBD, profile, gain);
                }
            }

            profile = internalCondition.GetProfile(ProfileType.EquipmentLatent, profileLibrary);
            if (profile != null)
            {
                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticELG);
                if (profile_TBD != null)
                {
                    double gain = Analytical.Query.CalculatedEquipmentLatentGain(space);
                    if (double.IsNaN(area) || area == 0 || double.IsNaN(gain))
                        gain = 0;
                    else
                        gain = gain / area;

                    Update(profile_TBD, profile, gain);
                }
            }

            profile = internalCondition.GetProfile(ProfileType.Pollutant, profileLibrary);
            if (profile != null)
            {
                profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticCOG);
                if (profile_TBD != null)
                {
                    double generation = Analytical.Query.CalculatedPollutantGeneration(space);
                    if (double.IsNaN(area) || area == 0 || double.IsNaN(generation))
                        generation = 0;
                    else
                        generation = generation / area;

                    Update(profile_TBD, profile, generation);
                }
            }

            profile = internalCondition.GetProfile(ProfileType.Ventilation, profileLibrary);
            profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticV);
            if (profile_TBD != null)
            {
                //The FULL calculated supply-air requirement, in ACH - every basis, per-person included.
                //
                //REQUIREMENT vs REALISATION. The four SAM airflow parameters state an engineering requirement;
                //they do not by themselves prescribe how TAS delivers it (a TBD Ventilation profile, an IZAM,
                //Tas Systems). The "profile != null" gate below is that choice, and it is deliberately the only
                //thing that activates Building Simulator mechanical ventilation here: the presence of airflow
                //data must never switch it on by itself. Where a profile IS assigned, the rate written must be
                //the whole requirement - internalGain.freshAirRate above holds the per-person rate too, but as
                //TAS's Outside Air field, which does not itself supply the zone in the simulation, so this is
                //not a double count.
                //
                //This total is not what stopped the ticV factor growing on every round trip: the import used to
                //read it back as the single SupplyAirChangesPerHour basis, so the next export summed the other
                //bases on top of it again. SAMZoneMetadata now carries the authored decomposition across the
                //seam instead. See Query.VentilationAirChangesPerHour and Modify.RestoreVentilationRequirement.
                double value_Temp = Query.VentilationAirChangesPerHour(space);

                if (double.IsNaN(value_Temp))
                {
                    value_Temp = 1;
                }

                if (profile != null)
                {
                    Update(profile_TBD, profile, value_Temp);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.VentilationFunction, out string ventilationFunction) && !string.IsNullOrWhiteSpace(ventilationFunction))
                {
                    if (profile_TBD.type == TBD.ProfileTypes.ticHourlyProfile)
                    {
                        profile_TBD.type = TBD.ProfileTypes.ticHourlyFunctionProfile;
                    }
                    else if (profile_TBD.type == TBD.ProfileTypes.ticYearlyProfile)
                    {
                        profile_TBD.type = TBD.ProfileTypes.ticYearlyFunctionProfile;
                    }
                    else 
                    {
                        profile_TBD.type = TBD.ProfileTypes.ticFunctionProfile;
                    }

                    profile_TBD.function = ventilationFunction;

                    if (internalCondition.TryGetValue(InternalConditionParameter.VentilationFunctionDescription, out string ventilationFunctionDescription))
                    {
                        profile_TBD.description = ventilationFunctionDescription;
                    }

                    if (internalCondition.TryGetValue(InternalConditionParameter.VentilationFunctionSetback, out double ventilationFunctionSetback) && double.IsNaN(ventilationFunctionSetback))
                    {
                        profile_TBD.setbackValue = System.Convert.ToSingle(ventilationFunctionSetback);
                    }

                    if (internalCondition.TryGetValue(InternalConditionParameter.VentilationFunctionFactor, out double ventilationFunctionFactor) && double.IsNaN(ventilationFunctionFactor))
                    {
                        profile_TBD.value = System.Convert.ToSingle(ventilationFunctionFactor);
                    }
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

                    profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticUL);
                    if (profile_TBD != null)
                        Update(profile_TBD, profile, 1);
                }

                profile = internalCondition.GetProfile(ProfileType.Heating, profileLibrary);
                if (profile != null)
                {
                    names.Add(profile.Name);

                    profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticLL);
                    if (profile_TBD != null)
                        Update(profile_TBD, profile, 1);
                }

                profile = internalCondition.GetProfile(ProfileType.Humidification, profileLibrary);
                if (profile != null)
                {
                    names.Add(profile.Name);

                    profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHLL);
                    if (profile_TBD != null)
                        Update(profile_TBD, profile, 1);
                }

                profile = internalCondition.GetProfile(ProfileType.Dehumidification, profileLibrary);
                if (profile != null)
                {
                    names.Add(profile.Name);

                    profile_TBD = thermostat.GetProfile((int)TBD.Profiles.ticHUL);
                    if (profile_TBD != null)
                        Update(profile_TBD, profile, 1);
                }

                names.RemoveAll(x => string.IsNullOrWhiteSpace(x));

                if (names.Count != 0)
                    thermostat.name = string.Join(" & ", names);
            }

            return true;
        }
    }
}