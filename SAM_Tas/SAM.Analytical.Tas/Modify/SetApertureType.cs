// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.ApertureType SetApertureType(this Building building, buildingElement buildingElement, ISingleOpeningProperties singleOpeningProperties, string name = null, int index = -1)
        {
            return SetApertureType(building, buildingElement, singleOpeningProperties, out string _, name, index);
        }

        /// <summary>
        /// Writes the aperture control (factor, discharge coefficient, day types and - where the opening
        /// properties carry one - an availability schedule) for one TBD building element.
        /// <para>
        /// <b>A TBD aperture type is a REUSABLE DEFINITION, not a per-window object.</b> The same type may
        /// be assigned to any number of building elements, so two hundred windows stating the same opening
        /// control need one type and two hundred assignments. This method therefore looks the control up by
        /// its DEFINITION - discharge coefficient, factor, profile mode, function, the 24 schedule values,
        /// the description and the day-type membership - and creates one only when nothing equivalent
        /// exists. It is the same discipline as schedule reuse (values decide, names do not), applied one
        /// level up. Previously the type was named after the building element, whose name carries the SAM
        /// aperture's GUID, and a GUID-named type can never be found again by the next identical window:
        /// naming and sharing were mutually exclusive, and naming won.
        /// </para>
        /// <para>
        /// <b>A shared definition is immutable.</b> When an equivalent type is found, NOTHING on it is
        /// written - not even rewritten to the same value - because every other element referencing it would
        /// see the write. Reuse requires full equality; anything less creates a new type under a
        /// deterministic, collision-suffixed name. The one exception is the fenced legacy path below.
        /// </para>
        /// <para>
        /// <b>The legacy per-element path is kept exactly as it was.</b> When every aperture type already on
        /// the element is named after the element itself, those names carry the aperture GUID and so are
        /// exclusive to it; the previous in-place update applies unchanged and a legacy TBD behaves
        /// bit-for-bit as it did. Such a TBD converges on shared types through a fresh export, not by being
        /// rewritten in place. Passing an explicit <paramref name="name"/> selects the same path, because an
        /// explicit name is an explicit instruction about which type to write.
        /// </para>
        /// <para>
        /// <b>A schedule and a Function are not mutually exclusive.</b> When both are present,
        /// <c>profile.type</c> is <c>ticFunctionProfile</c> so the function governs the base opening curve,
        /// and the schedule stays assigned as an availability multiplier on top of it - the same combination
        /// <see cref="AssignApertureTypes(Building, buildingElement, IEnumerable{dayType}, ApertureConstruction)"/>
        /// already writes for its own day/night split. A schedule with no function is written as
        /// <c>ticValueProfile</c>, where the schedule's own values are the whole curve.
        /// </para>
        /// <para>
        /// <b>Write order matters, and the schedule goes LAST.</b> The profile's mode is established first
        /// (<c>value</c>, <c>factor</c>, <c>setbackValue</c>, <c>type</c>, <c>function</c>) and
        /// <c>profile.schedule</c> is assigned only after that. Previously <c>profile.type</c> was set to
        /// <c>ticFunctionProfile</c> AFTER the schedule had been assigned, which risked the mode change
        /// discarding the schedule reference. The assignment is then read straight back and its values
        /// re-verified, so a schedule-write failure and a profile-assignment failure report as two different
        /// things instead of both surfacing as "the schedule is wrong in TAS".
        /// </para>
        /// <para>
        /// <b><c>setbackValue</c> is part of the schedule's meaning.</b> A TBD schedule selects between the
        /// profile's own value/function for an ON hour and its <c>setbackValue</c> for an OFF hour, so an
        /// availability schedule is only meaningful alongside a setback of 0. It is written explicitly
        /// whenever a schedule is written, rather than relied on to be 0 already.
        /// </para>
        /// <para>
        /// <b>Reuse, never overwrite.</b> Schedules are matched by their 24 VALUES, not by name - see
        /// <see cref="Create.GetOrCreateSchedule(Building, DailyAvailabilitySchedule, out string)"/> - so
        /// repeated export reuses one building-level schedule instead of duplicating it. If the matched TBD
        /// aperture type already carries a schedule, that schedule is retained when its values equal the
        /// requested ones (behaviourally the same control), and the write is REFUSED when they differ: a
        /// differently-valued schedule may be user-authored control this method has no business erasing.
        /// Value equality is the test; the name is not.
        /// </para>
        /// <para>
        /// <b>What the refusals do and do not guarantee.</b> Every failure returns null with
        /// <paramref name="refusal"/> set, and the guarantee is specifically this: <b>a failed or
        /// incompatible schedule is never assigned, and an existing different-valued schedule is never
        /// overwritten.</b> No TBD schedule is created before the SAM-side source has been validated either,
        /// which matters because <c>TBD.Building</c> has no <c>RemoveSchedule</c> and a schedule created in
        /// error could not be withdrawn.
        /// </para>
        /// <para>
        /// <b>This method is NOT transactional, and must not be described as such.</b> By the time a refusal
        /// can fire, it may already have created the aperture type and written its <c>description</c>; and a
        /// refusal from the final assignment read-back comes after <c>dischargeCoefficient</c>, <c>value</c>,
        /// <c>factor</c>, <c>setbackValue</c>, <c>type</c> and <c>function</c> have been written. Schedule
        /// resolution is deliberately hoisted above all of those writes - it needs none of them - so an
        /// unusable source, a naming collision or a failed schedule write leaves the profile's existing mode
        /// untouched. Only the two assignment-verification refusals are unavoidably late, because they exist
        /// to check an assignment that has to have happened first. Reuse, by contrast, IS all-or-nothing:
        /// it writes nothing at all.
        /// </para>
        /// <para>
        /// <b>What a late failure leaves behind, and how it is contained.</b> A created aperture type is
        /// never removed on failure - it stays in the TBD exactly as far as the write got - so its name is
        /// RESERVED with the cache the moment it exists: the object can never become a reusable definition
        /// this session, and no later creation will accidentally choose its name. And before a new type is
        /// registered as reusable or assigned at all, it is read back in full through the same seed reader
        /// that classifies pre-existing types and must equal the requested definition; a mismatch refuses,
        /// leaving the type named, reserved and non-reusable.
        /// </para>
        /// </summary>
        /// <param name="refusal">Why nothing was written, or null on success.</param>
        public static TBD.ApertureType SetApertureType(this Building building, buildingElement buildingElement, ISingleOpeningProperties singleOpeningProperties, out string refusal, string name = null, int index = -1)
        {
            //The legacy 1-based child index doubles as the reuse ordinal on the shared path. Forwarding -1
            //would make two calls for two identical indexed openings both resolve to occurrence 1, so the
            //second would reuse the first's type and the assignment guard would silently collapse two
            //openings into one. Position-derived ordinals keep the two calls two distinct types; the
            //multiple-opening entry point computes the true per-definition occurrence and is unaffected.
            return SetApertureType(building, buildingElement, singleOpeningProperties, out refusal, name, index, null, Query.ApertureTypeOrdinal(index));
        }

        /// <summary>
        /// The same write, taking the <see cref="BuildingReuseCache"/> that makes definition lookup a
        /// memory read instead of two full COM scans of every aperture type in the building per opening
        /// child, and the <paramref name="ordinal"/> half of the reuse key.
        /// </summary>
        /// <param name="cache">
        /// The open document's cache, or null to build a single-use one. Null is behaviourally identical -
        /// only slower - so every existing call site keeps working unchanged.
        /// </param>
        /// <param name="ordinal">
        /// The 1-based occurrence of this control among the element's opening children; -1 or 0 mean the
        /// first. TAS keeps one entry per aperture type on an element, so an element's second identical
        /// opening needs a second, distinct type - which is itself shared with every other element's second
        /// identical opening. See <see cref="Query.ApertureTypeOrdinals(IEnumerable{ApertureTypeDefinition})"/>.
        /// </param>
        public static TBD.ApertureType SetApertureType(this Building building, buildingElement buildingElement, ISingleOpeningProperties singleOpeningProperties, out string refusal, string name, int index, BuildingReuseCache cache, int ordinal)
        {
            refusal = null;

            if(building == null || buildingElement == null || singleOpeningProperties == null)
            {
                return null;
            }

            string name_Temp = name;
            if(name_Temp == null)
            {
                name_Temp = buildingElement.name;
            }

            if(string.IsNullOrWhiteSpace(name_Temp))
            {
                return null;
            }

            if(index != -1)
            {
                name_Temp = string.Format("{0} {1}", name_Temp, index);
            }

            BuildingReuseCache cache_Temp = cache ?? new BuildingReuseCache(building);

            //An explicit name is an explicit instruction about WHICH aperture type to write, so it keeps
            //the previous named, in-place path rather than being resolved to a shared definition.
            if (name != null)
            {
                return SetApertureType_Named(building, buildingElement, singleOpeningProperties, out refusal, name_Temp, cache_Temp);
            }

            //Resolve the control the opening asks for. This touches no COM at all, so an opening that
            //states an unusable schedule is refused before anything in the TBD has been created or changed.
            ApertureTypeDefinition apertureTypeDefinition = singleOpeningProperties.ApertureTypeDefinition(cache_Temp.DayTypeNames, out string name_Schedule, out string refusal_Definition);
            if (apertureTypeDefinition == null)
            {
                refusal = string.Format("Aperture type '{0}': {1}", name_Temp, refusal_Definition ?? "the opening control could not be resolved.");
                return null;
            }

            int ordinal_Temp = ordinal < 1 ? 1 : ordinal;

            //What the element ALREADY carried, before this export touched it, decides what may happen next.
            List<KeyValuePair<string, ApertureTypeDefinition>> assignments = cache_Temp.ExistingAssignments(buildingElement);

            Analytical.Tas.ApertureTypeReconciliation reconciliation = Query.ApertureTypeReconciliation(buildingElement.name, assignments, apertureTypeDefinition, ordinal_Temp, out int index_Assigned, out string refusal_Reconciliation);
            switch (reconciliation)
            {
                case Analytical.Tas.ApertureTypeReconciliation.Legacy:
                    return SetApertureType_Named(building, buildingElement, singleOpeningProperties, out refusal, name_Temp, cache_Temp);

                case Analytical.Tas.ApertureTypeReconciliation.Refuse:
                    refusal = refusal_Reconciliation;
                    return null;

                case Analytical.Tas.ApertureTypeReconciliation.Reuse:
                    //Already correct. The element carries this very control, so there is nothing to write
                    //and nothing to assign.
                    return cache_Temp.ExistingApertureTypes(buildingElement)[index_Assigned];
            }

            //Building-level reuse. A hit is assigned and nothing on it is touched: it belongs to every other
            //element that references it.
            TBD.ApertureType result = cache_Temp.FindApertureType(apertureTypeDefinition, ordinal_Temp);
            if (result != null)
            {
                AssignApertureType(buildingElement, result, cache_Temp);
                return result;
            }

            //A new definition is genuinely needed. The name is derived from the definition - never from the
            //building element, and so never from a physical aperture's GUID.
            string name_New = Query.ApertureTypeName(cache_Temp.ApertureTypeNames(), apertureTypeDefinition, ordinal_Temp, out string refusal_Name);
            if (name_New == null)
            {
                refusal = refusal_Name;
                return null;
            }

            result = building.AddApertureType(null);
            if (result == null)
            {
                return null;
            }

            result.name = name_New;

            //Reserved the moment the name exists in the TBD, BEFORE anything else is written: from here on
            //a late failure leaves the type behind, and it must never become reusable - but its name still
            //occupies the namespace, so the next creation cannot accidentally choose it again. Only the
            //full read-back verification below upgrades this reservation to a reusable registration.
            cache_Temp.ReserveApertureType(result, ordinal_Temp);

            if (singleOpeningProperties.TryGetValue(OpeningPropertiesParameter.Description, out string description))
            {
                result.description = description;
            }

            dynamic @dynamic = result;

            profile profile = @dynamic.GetProfile();
            if (profile == null)
            {
                //Everything below writes through this profile, so there is nothing to write to. Reported
                //rather than thrown, in keeping with this method's other outcomes.
                refusal = string.Format("Aperture type '{0}' has no TBD profile to write the aperture control to.", name_New);
                return null;
            }

            //Resolved BEFORE the profile is touched. Nothing below this point is needed to resolve it, so
            //hoisting it here means an invalid source, a naming collision or a failed COM write leaves the
            //profile's existing mode, factor and function exactly as they were, instead of half-rewritten.
            schedule schedule_Resolved = null;
            if (apertureTypeDefinition.HasSchedule)
            {
                //Validates, then reuses by value, then creates at most one schedule and verifies its write
                //by reading all 24 values back.
                schedule_Resolved = building.GetOrCreateSchedule(name_Schedule, apertureTypeDefinition.ScheduleValues, out string refusal_Schedule, cache_Temp);
                if (schedule_Resolved == null)
                {
                    refusal = string.Format("Aperture type '{0}': {1}", name_New, refusal_Schedule ?? "no TBD schedule could be resolved.");
                    return null;
                }
            }

            @dynamic.dischargeCoefficient = apertureTypeDefinition.DischargeCoefficient;

            profile.value = 1;

            //The definition already carries the factor TBD is to hold, including the Part O
            //AlwaysClosed -> 0 override: an opening that takes no part in the overheating ventilation
            //strategy multiplies out any function- or schedule-driven curve, without needing a second
            //24-hour zero schedule purely for symmetry.
            profile.factor = apertureTypeDefinition.Factor;

            if (apertureTypeDefinition.HasSchedule)
            {
                //The schedule's OFF hours select this value, so an availability schedule requires it to be 0.
                profile.setbackValue = 0;
            }

            //The profile's mode is established BEFORE the schedule is assigned - see the remarks. A Function
            //claims the base curve; a schedule on its own is the curve.
            if (apertureTypeDefinition.Mode == ApertureTypeProfileMode.Function)
            {
                profile.type = ProfileTypes.ticFunctionProfile;
                profile.function = apertureTypeDefinition.Function;
            }
            else if (apertureTypeDefinition.Mode == ApertureTypeProfileMode.ScheduleOnly)
            {
                //profile.type = ProfileTypes.ticHourlyFunctionProfile;  //TODO: 2023-04-19 To be implemented once Tas allows ticHourlyFunctionProfile or ticYearlyFunctionProfile
                profile.type = ProfileTypes.ticValueProfile;
            }

            if (apertureTypeDefinition.HasSchedule)
            {
                profile.schedule = schedule_Resolved;

                //Read the assignment back. This separates "the schedule did not persist its values" from
                //"the profile did not keep the schedule reference" - the second being what a mode change
                //after assignment could cause.
                schedule schedule_Assigned = profile.schedule;
                if (schedule_Assigned == null)
                {
                    refusal = string.Format("Aperture type '{0}': the TBD profile did not retain the assigned schedule '{1}'.", name_New, schedule_Resolved.name);
                    return null;
                }

                int[] values_Assigned = schedule_Assigned.HourlyValues();
                if (!Query.ScheduleValuesEqual(values_Assigned, apertureTypeDefinition.ScheduleValues))
                {
                    refusal = string.Format("Aperture type '{0}': the schedule assigned to the TBD profile ('{1}') does not read back the requested 24 hourly values.", name_New, schedule_Assigned.name);
                    return null;
                }
            }

            foreach (dayType dayType in cache_Temp.DayTypes)
            {
                @dynamic.SetDayType(dayType, true);
            }

            //Full read-back verification, run only for a newly created definition (reuse writes nothing,
            //so there is nothing to verify there). The whole type is read back through the same seed
            //reader that classifies pre-existing types - discharge coefficient, description, profile
            //value/factor/setback/type/function, the schedule and its 24 values, and day-type membership -
            //and only a persisted definition EQUAL to the requested one makes the type reusable. A type
            //that reads back differently, or not at all, is refused: its name stays reserved, it is never
            //handed to the next opening as a reusable definition, and it is not assigned here either.
            ApertureTypeDefinition persisted = result.ApertureTypeDefinition(out string refusal_ReadBack);
            if (persisted == null || !persisted.Equals(apertureTypeDefinition))
            {
                refusal = string.Format("Aperture type '{0}' was created but did not read back as the requested opening control ({1}), so it was left in the TBD as a named, non-reusable type and was not assigned.", name_New, refusal_ReadBack ?? "the persisted control differs from the requested one");
                return null;
            }

            //Registered as reusable only once the write has fully succeeded AND verified, so a refused
            //write can never be handed to the next opening as a reusable definition.
            cache_Temp.RegisterApertureType(result, apertureTypeDefinition, ordinal_Temp);

            AssignApertureType(buildingElement, result, cache_Temp);

            return result;
        }

        /// <summary>
        /// The previous per-element write, unchanged: find or create the aperture type carrying
        /// <paramref name="name_Temp"/> and update it in place.
        /// <para>
        /// <b>Why writing into an existing type is safe here and nowhere else.</b> This path is only
        /// reached when the name was supplied explicitly, or when every aperture type on the element is
        /// named after the element - and an element's name carries the SAM aperture's GUID, so a type named
        /// after it can belong to no other element. Elsewhere a type is shared and a write would reach every
        /// element referencing it.
        /// </para>
        /// </summary>
        private static TBD.ApertureType SetApertureType_Named(Building building, buildingElement buildingElement, ISingleOpeningProperties singleOpeningProperties, out string refusal, string name_Temp, BuildingReuseCache cache)
        {
            refusal = null;

            //Resolve the SAM-side schedule source first. This touches no COM at all, so an opening that
            //states an unusable schedule is refused before anything in the TBD has been created or changed.
            bool scheduleRequested = singleOpeningProperties.TryGetOpeningScheduleSource(out string name_Schedule, out int[] values_Schedule, out string refusal_Source);
            if (refusal_Source != null)
            {
                refusal = string.Format("Aperture type '{0}': {1}", name_Temp, refusal_Source);
                return null;
            }

            TBD.ApertureType result = cache.FindApertureTypeByName(name_Temp);
            bool apertureType_Existed = result != null;

            if(result == null)
            {
                result = building.AddApertureType(null);
                if (result == null)
                {
                    return null;
                }

                result.name = name_Temp;

                //Reserved with NO definition: a per-element type is exclusive to its element by name and
                //must never be adopted by another. Its name still occupies the namespace.
                cache.ReserveApertureType(result, 1, "A per-element aperture type is exclusive to its building element by name, so it is not reusable by any other.");
            }

            if (singleOpeningProperties.TryGetValue(OpeningPropertiesParameter.Description, out string description))
            {
                result.description = description;
            }

            dynamic @dynamic = result;

            profile profile = @dynamic.GetProfile();
            if (profile == null)
            {
                //Everything below writes through this profile, so there is nothing to write to. Reported
                //rather than thrown, in keeping with this method's other outcomes.
                refusal = string.Format("Aperture type '{0}' has no TBD profile to write the aperture control to.", name_Temp);
                return null;
            }

            //An aperture type that already carries a schedule is judged by that schedule's VALUES, before
            //anything is written. Equal values are the same control by another name and are retained;
            //different values belong to somebody else and are refused, not overwritten.
            schedule schedule_Resolved = null;
            if (scheduleRequested && apertureType_Existed)
            {
                schedule schedule_Existing = profile.schedule;
                if (schedule_Existing != null)
                {
                    int[] values_Existing = schedule_Existing.HourlyValues();
                    if (values_Existing != null && Query.ScheduleValuesEqual(values_Existing, values_Schedule))
                    {
                        schedule_Resolved = schedule_Existing;
                    }
                    else
                    {
                        refusal = string.Format("Aperture type '{0}' already carries a schedule ('{1}') whose 24 hourly values differ from the requested availability schedule, so it was left untouched rather than overwritten. Values, not names, decide whether an existing schedule is compatible.", name_Temp, schedule_Existing.name);
                        return null;
                    }
                }
            }

            //Resolved BEFORE the profile is touched. Nothing below this point is needed to resolve it, so
            //hoisting it here means an invalid source, a naming collision or a failed COM write leaves the
            //profile's existing mode, factor and function exactly as they were, instead of half-rewritten.
            if (scheduleRequested && schedule_Resolved == null)
            {
                //Validates, then reuses by value, then creates at most one schedule and verifies its write
                //by reading all 24 values back.
                schedule_Resolved = building.GetOrCreateSchedule(name_Schedule, values_Schedule, out string refusal_Schedule, cache);
                if (schedule_Resolved == null)
                {
                    refusal = string.Format("Aperture type '{0}': {1}", name_Temp, refusal_Schedule ?? "no TBD schedule could be resolved.");
                    return null;
                }
            }

            @dynamic.dischargeCoefficient = System.Convert.ToSingle(singleOpeningProperties.GetDischargeCoefficient());

            profile.value = 1;
            profile.factor = System.Convert.ToSingle(singleOpeningProperties.GetFactor());

            if (singleOpeningProperties is PartOOpeningProperties partOOpeningProperties_Restriction && partOOpeningProperties_Restriction.OpeningRestriction == OpeningRestriction.AlwaysClosed)
            {
                //The opening takes no part in the overheating ventilation strategy. Zeroing the factor
                //multiplies out any function- or schedule-driven curve regardless of what else this opening
                //carries, without needing a second 24-hour zero schedule purely for symmetry.
                profile.factor = 0;
            }

            if (scheduleRequested)
            {
                //The schedule's OFF hours select this value, so an availability schedule requires it to be 0.
                profile.setbackValue = 0;
            }

            //The profile's mode is established BEFORE the schedule is assigned - see the remarks. A Function
            //claims the base curve; a schedule on its own is the curve.
            if (singleOpeningProperties.TryGetValue(OpeningPropertiesParameter.Function, out string function))
            {
                profile.type = ProfileTypes.ticFunctionProfile;
                profile.function = function;
            }
            else if (scheduleRequested)
            {
                //profile.type = ProfileTypes.ticHourlyFunctionProfile;  //TODO: 2023-04-19 To be implemented once Tas allows ticHourlyFunctionProfile or ticYearlyFunctionProfile
                profile.type = ProfileTypes.ticValueProfile;
            }

            if (scheduleRequested)
            {
                profile.schedule = schedule_Resolved;

                //Read the assignment back. This separates "the schedule did not persist its values" from
                //"the profile did not keep the schedule reference" - the second being what a mode change
                //after assignment could cause.
                schedule schedule_Assigned = profile.schedule;
                if (schedule_Assigned == null)
                {
                    refusal = string.Format("Aperture type '{0}': the TBD profile did not retain the assigned schedule '{1}'.", name_Temp, schedule_Resolved.name);
                    return null;
                }

                int[] values_Assigned = schedule_Assigned.HourlyValues();
                if (!Query.ScheduleValuesEqual(values_Assigned, values_Schedule))
                {
                    refusal = string.Format("Aperture type '{0}': the schedule assigned to the TBD profile ('{1}') does not read back the requested 24 hourly values.", name_Temp, schedule_Assigned.name);
                    return null;
                }
            }

            foreach (dayType dayType in cache.DayTypes)
            {
                @dynamic.SetDayType(dayType, true);
            }

            AssignApertureType(buildingElement, result, cache);

            return result;
        }

        /// <summary>
        /// Assigns an aperture type to a building element unless the element already carries one of that
        /// name.
        /// <para>
        /// <b>The guard is load-bearing, not defensive.</b> <c>AssignApertureType</c> adds a SECOND entry
        /// when handed a type the element already has - verified against licensed TAS - which would give the
        /// element more openings than the model states. Names are unique per <c>(definition, ordinal)</c>,
        /// so a name test is exactly the right test.
        /// </para>
        /// </summary>
        private static void AssignApertureType(buildingElement buildingElement, TBD.ApertureType apertureType, BuildingReuseCache cache)
        {
            if (buildingElement == null || apertureType == null || cache.IsAssigned(buildingElement, apertureType.name))
            {
                return;
            }

            buildingElement.AssignApertureType(apertureType);
            cache.RegisterAssignment(buildingElement, apertureType);
        }
    }
}
