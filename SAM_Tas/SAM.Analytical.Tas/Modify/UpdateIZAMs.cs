// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System.Collections.Generic;
using TBD;
using System.Linq;
using SAM.Core;
using SAM.Geometry.Object.Spatial;
using System;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<string> UpdateIZAMs(this string path_TBD, AdjacencyCluster adjacencyCluster)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<string> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateIZAMs(sAMTBDDocument, adjacencyCluster);
                if (result != null)
                {
                    sAMTBDDocument.Save();
                }
            }

            return result;
        }

        public static List<string> UpdateIZAMs(this SAMTBDDocument sAMTBDDocument, AdjacencyCluster adjacencyCluster)
        {
            if (sAMTBDDocument == null)
            {
                return null;
            }

            return UpdateIZAMs(sAMTBDDocument.TBDDocument, adjacencyCluster);
        }

        public static List<string> UpdateIZAMs(this TBDDocument tBDDocument, AdjacencyCluster adjacencyCluster)
        {
            if(tBDDocument == null || adjacencyCluster == null)
            {
                return null;
            }

            Building building = tBDDocument.Building;

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if(spaces == null)
            {
                return null;
            }

            List<AirHandlingUnit> airHandlingUnits = adjacencyCluster.GetObjects<AirHandlingUnit>();
            if(airHandlingUnits == null)
            {
                return null;
            }

            //HDD and CDD are excluded from the generated plant zone's internal condition on purpose, and
            //TAS's own pre-simulation check says so ("Zone 'MVHR-01' is missing internal conditions on some
            //daytypes"). That warning is expected - see Query.DayType_PlantZoneInternalCondition, which
            //holds the decision and the reason, and is what pins it.
            List<dayType> dayTypes = building.DayTypes();
            dayTypes.RemoveAll(x => !Query.DayType_PlantZoneInternalCondition(x?.name));

            List<string> result = new List<string>();

            double height = 2;

            double elevation = 0;
            Geometry.Spatial.BoundingBox3D boundingBox3D = adjacencyCluster?.GetPanels()?.BoundingBox3D(1);
            if(boundingBox3D != null)
            {
                elevation = boundingBox3D.Min.Z - height -  1;
            }

            // Pre-fetch and index zones once. Was being refetched (full COM rebuild) inside the AHU loop
            // AND linear-scanned via zones.Match() per AHU / per space / per movement.
            // Lookup key matches zones.Match(name, caseSensitive: false, trim: true) semantics.
            List<zone> zonesList = building.Zones() ?? new List<zone>();
            Dictionary<string, zone> zonesByKey = new Dictionary<string, zone>(zonesList.Count);
            foreach (zone z in zonesList)
            {
                string n = z?.name;
                if (!string.IsNullOrWhiteSpace(n))
                    zonesByKey[n.Trim().ToUpper()] = z;
            }

            // The room extract that must leave the building from the ROOM rather than through the unit,
            // because the unit is about to become a well-mixed TAS zone and cannot carry a supply and an
            // extract airstream past each other. Scoped to the design-terminal realization alone - see
            // Query.DesignTerminalExtractFlattening for what qualifies and why nothing else can.
            //
            // Empty on every generic MEP model, which routes nothing into its unit in the first place.
            HashSet<Guid> guids_FlattenedExtract = Query.DesignTerminalExtractFlattening(adjacencyCluster, out HashSet<Guid> guids_AirHandlingUnit_NoExhaust);

            // Pre-resolve everything the loops need so we can:
            //  (a) bulk-remove all to-be-replaced ICs and IZAMs in two calls (was per-iteration → O(N^2)),
            //  (b) avoid re-fetching GetRelatedObjects / GetObjects inside the modify loops.
            ResolveAirHandlingUnitMovements(
                adjacencyCluster,
                airHandlingUnits,
                guids_AirHandlingUnit_NoExhaust,
                out Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement> ahuMovements,
                out Dictionary<AirHandlingUnit, List<SpaceAirMovement>> ahuOutwardMovements,
                out HashSet<string> icNamesToReplace,
                out HashSet<string> izamNamesToReplace);

            // Resolved space-air-movement metadata, keyed by Space so the loop is O(spaces).
            Dictionary<Space, List<SpaceMovementInfo>> spaceMovements = new Dictionary<Space, List<SpaceMovementInfo>>();
            foreach (Space space in spaces)
            {
                List<SpaceAirMovement> sams = adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(space);
                if (sams == null || sams.Count == 0)
                    continue;

                List<SpaceMovementInfo> infos = new List<SpaceMovementInfo>(sams.Count);
                foreach (SpaceAirMovement sam in sams)
                {
                    ObjectReference refFrom = Core.Convert.ComplexReference<ObjectReference>(sam.From);
                    ObjectReference refTo = Core.Convert.ComplexReference<ObjectReference>(sam.To);

                    SAMObject sFrom = adjacencyCluster.GetObjects<SAMObject>(refFrom)?.FirstOrDefault();
                    SAMObject sTo = adjacencyCluster.GetObjects<SAMObject>(refTo)?.FirstOrDefault();

                    if (sFrom == null)
                        continue;

                    // The room's extract leaves the building HERE rather than at the unit. Dropping the
                    // destination is the whole of the transformation: everything downstream already treats a
                    // movement with no destination as one leaving from its source zone, so this lands as an
                    // IZAM on the ROOM's own zone with no source zone and fromOutside = 0 - the shape TAS
                    // itself authors for a zone discharging to outside - and is named "... TO OUTSIDE",
                    // which is also the name queued for removal, so a re-export replaces it cleanly.
                    //
                    // The movement's flow, profile and source are untouched. Only the SAM model's statement
                    // that this air passes through the unit is dropped, and only for the export.
                    //
                    // A TBD written before this movement was flattened carries it under the OLD name - "IZAM
                    // <room> TO <unit>". Queuing only the new "TO OUTSIDE" name below would leave that old
                    // IZAM behind on a re-export: the stale room-to-unit extract would survive alongside the
                    // new room-to-outside one while the unit's matching exhaust is suppressed, and the
                    // duplicate inflow would unbalance the unit's zone. Both names are queued so either
                    // vintage of this movement is replaced cleanly.
                    if (sTo != null && guids_FlattenedExtract.Contains(sam.Guid))
                    {
                        izamNamesToReplace.Add(string.Format("IZAM {0} TO {1}", sFrom.Name, sTo.Name));
                        sTo = null;
                    }

                    string izamName = string.Format("IZAM {0}", sFrom.Name);
                    izamName = sTo == null ? string.Format("{0} TO OUTSIDE", izamName) : string.Format("{0} TO {1}", izamName, sTo.Name);

                    izamNamesToReplace.Add(izamName);
                    infos.Add(new SpaceMovementInfo { Movement = sam, From = sFrom, To = sTo, IZAMName = izamName });
                }
                if (infos.Count > 0)
                    spaceMovements[space] = infos;
            }

            // Single bulk remove instead of per-iteration removes (which each scanned the full IC/IZAM list).
            // Note: this assumes distinct names per AHU / movement — the original code would have only kept
            // the last-added when names collide; with bulk-remove + simple adds, colliding names would yield
            // duplicates. In practice these names derive from unique AirHandlingUnitAirMovement / SAMObject
            // names so collisions don't occur.
            if (icNamesToReplace.Count > 0)
                RemoveInternalConditions(building, icNamesToReplace);
            if (izamNamesToReplace.Count > 0)
                RemoveIZAMs(building, izamNamesToReplace);

            // Which unit already has a plant zone in this file, and which needs one built. Resolved once,
            // before the loop, from the zones as they stand now - so a warm-started round, which opens a copy
            // of a canonical TBD this method has already run over, updates the three zones it finds instead of
            // appending three more beside them. See ResolvePlantZoneReuse for the two authorities it uses.
            Dictionary<Guid, PlantZoneCandidate> plantZoneReuse = ResolvePlantZoneReuse(
                airHandlingUnits,
                PlantZoneCandidates(zonesList),
                spaces.ConvertAll(x => x?.Name));

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                if (!ahuMovements.TryGetValue(airHandlingUnit, out AirHandlingUnitAirMovement airHandlingUnitAirMovement) || airHandlingUnitAirMovement == null)
                    continue;

                zone zone;

                if (plantZoneReuse.TryGetValue(airHandlingUnit.Guid, out PlantZoneCandidate plantZoneCandidate) && plantZoneCandidate != null)
                {
                    // This unit's plant zone is already here. Its internal condition and inter-zone air
                    // movements were stripped by the remove-by-name step above and are rebuilt below, exactly
                    // as they would be on a freshly built zone - so the zone is brought fully up to date
                    // rather than merely left in place. No box is created and no elevation is consumed.
                    zone = zonesList[plantZoneCandidate.Index];
                    if (zone == null)
                        continue;
                }
                else
                {
                    AdjacencyCluster adjacencyCluster_Temp = Create.AdjacencyCluster(elevation, 3, height, 3);
                    elevation -= height - 1;

                    int zoneCountBefore = zonesList.Count;
                    Update(building, adjacencyCluster_Temp, Analytical.Query.DefaultMaterialLibrary(), true);

                    Space space = adjacencyCluster_Temp.GetSpaces().FirstOrDefault();
                    if (space == null || string.IsNullOrWhiteSpace(space.Name))
                        continue;

                    // Refresh local zone tracking. Update() appends one new zone — pick it up and add to the dict
                    // without doing a per-iteration zones.Match scan over hundreds of zones.
                    zonesList = building.Zones() ?? new List<zone>();
                    for (int i = zoneCountBefore; i < zonesList.Count; i++)
                    {
                        zone newZ = zonesList[i];
                        string n = newZ?.name;
                        if (!string.IsNullOrWhiteSpace(n))
                            zonesByKey[n.Trim().ToUpper()] = newZ;
                    }

                    if (!zonesByKey.TryGetValue(space.Name.Trim().ToUpper(), out zone) || zone == null)
                        continue;
                }

                zone.name = airHandlingUnit.Name;
                if (!string.IsNullOrWhiteSpace(airHandlingUnit.Name))
                    zonesByKey[airHandlingUnit.Name.Trim().ToUpper()] = zone;

                // Stamped every time, not only on the zones this run creates: it is what the NEXT run matches
                // on, so a zone adopted by name here answers by identity from now on and stops depending on
                // what either it or its unit is currently called.
                string description = PlantZoneIdentity.Compose(zone.description, airHandlingUnit.Guid);
                if (!string.IsNullOrEmpty(description))
                    zone.description = description;

                zone.sizeHeating = (int)TBD.SizingType.tbdSizing;

                string name = string.Format("{0}", airHandlingUnitAirMovement.Name);

                TBD.InternalCondition internalCondition = building.AddIC(null);
                internalCondition.name = name;
                foreach (dayType dayType in dayTypes)
                {
                    internalCondition.SetDayType(dayType, true);
                }

                Thermostat thermostat = internalCondition.GetThermostat();

                if(thermostat != null)
                {
                    Profile heating = airHandlingUnitAirMovement.Heating;
                    if (heating != null)
                    {
                        profile profile_TBD = thermostat.GetProfile((int)Profiles.ticLL);
                        if (profile_TBD != null)
                        {
                            Update(profile_TBD, heating, 1);
                        }
                    }

                    Profile cooling = airHandlingUnitAirMovement.Cooling;
                    if (cooling != null)
                    {
                        profile profile_TBD = thermostat.GetProfile((int)Profiles.ticUL);
                        if (profile_TBD != null)
                        {
                            Update(profile_TBD, cooling, 1);
                        }
                    }

                    Profile humidification = airHandlingUnitAirMovement.Humidification;
                    if (humidification != null)
                    {
                        profile profile_TBD = thermostat.GetProfile((int)Profiles.ticHLL);
                        if (profile_TBD != null)
                        {
                            Update(profile_TBD, humidification, 1);
                        }
                    }

                    Profile dehumidification = airHandlingUnitAirMovement.Dehumidification;
                    if (dehumidification != null)
                    {
                        profile profile_TBD = thermostat.GetProfile((int)Profiles.ticHUL);
                        if (profile_TBD != null)
                        {
                            Update(profile_TBD, dehumidification, 1);
                        }
                    }

                    zone.AssignIC(internalCondition, true);
                }

                double airFlow = Analytical.Query.AirFlow(adjacencyCluster, airHandlingUnitAirMovement, out Profile profile_AirHandlingUnit);
                if(profile_AirHandlingUnit != null)
                {
                    name = string.Format("IZAM {0} FROM OUTSIDE", airHandlingUnitAirMovement.Name);

                    IZAM iZAM = building.AddIZAM(null);
                    iZAM.fromOutside = 1;
                    iZAM.name = name;
                    result.Add(iZAM.name);

                    foreach (dayType dayType in dayTypes)
                    {
                        iZAM.SetDayType(dayType, true);
                    }

                    profile profile = iZAM.GetProfile();
                    //Volumetric m3/s in, mass kg/s out - a TBD inter-zone air movement is a mass flow rate.
                    profile.UpdateIZAMProfile(profile_AirHandlingUnit, airFlow);

                    zone.AssignIZAM(iZAM, true);
                }

                // The unit's exhaust. A TBD inter-zone air movement that is assigned to a zone, has NO source
                // zone and is not from outside moves air OUT of that zone to outside - which is how TAS
                // itself authors a zone that discharges to outside (the "From Atrium to Outside" movement of
                // the shipped `example.tbd` sample has exactly this shape, and re-creating it through
                // Building.AddIZAM keeps that sample balanced and simulating). TBD.IIZAM exposes no outward
                // flag of any kind, so this shape is the whole of the representation.
                if (ahuOutwardMovements.TryGetValue(airHandlingUnit, out List<SpaceAirMovement> outwardMovements))
                {
                    foreach (SpaceAirMovement spaceAirMovement in outwardMovements)
                    {
                        IZAM iZAM = building.AddIZAM(null);
                        iZAM.fromOutside = 0;
                        iZAM.name = OutwardIZAMName(airHandlingUnit);
                        result.Add(iZAM.name);

                        foreach (dayType dayType in dayTypes)
                        {
                            iZAM.SetDayType(dayType, true);
                        }

                        profile profile = iZAM.GetProfile();
                        profile.UpdateIZAMProfile(spaceAirMovement.Profile, spaceAirMovement.AirFlow);

                        // Deliberately no SetSourceZone: a source zone would make this air arriving from
                        // somewhere else rather than leaving the building.
                        zone.AssignIZAM(iZAM, true);
                    }
                }
            }

            foreach(Space space in spaces)
            {
                if (string.IsNullOrWhiteSpace(space?.Name))
                    continue;
                if (!zonesByKey.TryGetValue(space.Name.Trim().ToUpper(), out zone zone) || zone == null)
                    continue;

                zone.sizeHeating = (int)TBD.SizingType.tbdNoSizing;

                if (!spaceMovements.TryGetValue(space, out List<SpaceMovementInfo> movements) || movements == null)
                    continue;

                foreach (SpaceMovementInfo info in movements)
                {
                    // The zone the movement delivers INTO.
                    //
                    // A TBD inter-zone air movement only ever moves air INTO the zones it is assigned to,
                    // from a source zone or from outside - TBD.IIZAM has a source zone and target zones and
                    // a fromOutside flag, and no outward direction of any kind. So the target has to be read
                    // from the movement's To endpoint rather than assumed to be the space the movement is
                    // related to. A movement that names another model object as its destination - "room A ->
                    // room B" transfer air, or a "room -> air handling unit" extract this export has chosen
                    // to keep - is an IZAM on THAT object's zone, sourced from this one; writing it onto the
                    // space it happens to be related to would move the air the wrong way, or nowhere.
                    //
                    // Where To does not resolve to a zone, including where it is null, the target is the
                    // space's own zone, which is exactly the behaviour every existing caller relies on: a
                    // supply movement's To IS the space, so it resolves to the same zone either way, and a
                    // generic space's outward movement has no destination and leaves from its own zone.
                    // A Part O extract flattened above arrives here with To already dropped, and so takes
                    // that same outward path.
                    zone zone_Target = zone;
                    string key_Target = space.Name.Trim().ToUpper();

                    if (info.To != null && !string.IsNullOrWhiteSpace(info.To.Name))
                    {
                        string key_To = info.To.Name.Trim().ToUpper();
                        if (zonesByKey.TryGetValue(key_To, out zone zone_To) && zone_To != null)
                        {
                            zone_Target = zone_To;
                            key_Target = key_To;
                        }
                    }

                    IZAM iZAM = building.AddIZAM(null);

                    foreach (dayType dayType in dayTypes)
                    {
                        iZAM.SetDayType(dayType, true);
                    }

                    iZAM.name = info.IZAMName;
                    iZAM.fromOutside = 0;
                    result.Add(iZAM.name);

                    profile profile = iZAM.GetProfile();
                    profile.UpdateIZAMProfile(info.Movement.Profile, info.Movement.AirFlow);

                    zone_Target.AssignIZAM(iZAM, true);

                    // Compared by resolved zone key rather than by COM object identity: two runtime callable
                    // wrappers over the same TAS zone are not reference-equal.
                    if (info.From != null && !string.IsNullOrWhiteSpace(info.From.Name))
                    {
                        string key_From = info.From.Name.Trim().ToUpper();
                        if (key_From != key_Target && zonesByKey.TryGetValue(key_From, out zone zoneFrom) && zoneFrom != null)
                        {
                            iZAM.SetSourceZone(zoneFrom);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves, from the SAM model alone, what <see cref="UpdateIZAMs(TBDDocument, AdjacencyCluster)"/>
        /// needs to know about each air handling unit before it touches the TBD file: its own supply/extract
        /// movement, its current outward (unit-to-outside) movements if any, and the internal-condition and
        /// IZAM names a re-export must remove before writing fresh ones.
        /// <para>
        /// Pure SAM.Analytical - no TBD/COM type appears anywhere in this method - which is what lets it be
        /// unit-tested with no TAS licence, install or COM server.
        /// </para>
        /// <para>
        /// <b>The outward IZAM name is queued unconditionally</b>, for every air handling unit this run is
        /// about to process (every one carrying an <see cref="AirHandlingUnitAirMovement"/>), independent of
        /// whether a current outward <see cref="SpaceAirMovement"/> exists for it. A TBD written by an
        /// earlier export can carry an "IZAM &lt;AHU&gt; TO OUTSIDE" from a topology this run no longer
        /// builds - for example after the unit's extract was flattened to leave from the rooms instead - and
        /// that stale movement would otherwise survive a re-export, adding unmatched outflow to the unit's
        /// zone that TAS refuses to simulate. An air handling unit this run does not process at all - no
        /// <see cref="AirHandlingUnitAirMovement"/>, so nothing of this export's is on it - contributes
        /// nothing to the removal set, so a modeller-authored unit and its own hand-built IZAMs are left
        /// alone.
        /// </para>
        /// </summary>
        public static void ResolveAirHandlingUnitMovements(
            AdjacencyCluster adjacencyCluster,
            IEnumerable<AirHandlingUnit> airHandlingUnits,
            HashSet<Guid> guids_AirHandlingUnit_NoExhaust,
            out Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement> ahuMovements,
            out Dictionary<AirHandlingUnit, List<SpaceAirMovement>> ahuOutwardMovements,
            out HashSet<string> icNamesToReplace,
            out HashSet<string> izamNamesToReplace)
        {
            ahuMovements = new Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement>();
            ahuOutwardMovements = new Dictionary<AirHandlingUnit, List<SpaceAirMovement>>();
            icNamesToReplace = new HashSet<string>();
            izamNamesToReplace = new HashSet<string>();

            if (adjacencyCluster == null || airHandlingUnits == null)
            {
                return;
            }

            guids_AirHandlingUnit_NoExhaust ??= new HashSet<Guid>();

            foreach (AirHandlingUnit ahu in airHandlingUnits)
            {
                if (ahu == null)
                    continue;

                AirHandlingUnitAirMovement m = adjacencyCluster.GetRelatedObjects<AirHandlingUnitAirMovement>(ahu)?.FirstOrDefault();
                if (m == null)
                    continue;
                ahuMovements[ahu] = m;
                if (!string.IsNullOrEmpty(m.Name))
                {
                    icNamesToReplace.Add(m.Name);
                    izamNamesToReplace.Add(string.Format("IZAM {0} FROM OUTSIDE", m.Name));
                }

                // Queued unconditionally - see the summary above - rather than only when the loop below
                // still finds a current outward movement to write.
                izamNamesToReplace.Add(OutwardIZAMName(ahu));

                // The unit's own movements OUT of the building - its exhaust, carrying away the extract air
                // it has drawn out of the rooms. These are movements of the UNIT rather than of a space, so
                // the space loop below never reaches them, and without them the unit's zone gains the whole
                // extract duty and never loses it. TAS refuses to simulate such a zone.
                List<SpaceAirMovement> outward = new List<SpaceAirMovement>();
                ObjectReference objectReference_AHU = new ObjectReference(ahu);

                foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(ahu) ?? new List<SpaceAirMovement>())
                {
                    if (spaceAirMovement == null || !string.IsNullOrWhiteSpace(spaceAirMovement.To))
                        continue;

                    if (objectReference_AHU != Core.Convert.ComplexReference<ObjectReference>(spaceAirMovement.From))
                        continue;

                    outward.Add(spaceAirMovement);
                }

                // Where this unit's extract has been flattened to leave from the rooms instead, the unit has
                // nothing left to exhaust: writing one anyway would take the same air out of the building
                // twice and unbalance the unit's zone. The name is still queued for removal above, so a
                // stale exhaust from an earlier export of the same building does not survive.
                if (outward.Count != 0 && !guids_AirHandlingUnit_NoExhaust.Contains(ahu.Guid))
                    ahuOutwardMovements[ahu] = outward;
            }
        }

        /// <summary>
        /// <b>Which existing TBD zone, if any, is the plant zone each air handling unit already owns.</b>
        /// This is what makes <see cref="UpdateIZAMs(TBDDocument, AdjacencyCluster)"/> idempotent: a unit that
        /// resolves to a zone here has that zone updated, and only a unit that resolves to nothing gets a new
        /// one built.
        /// <para>
        /// Without it, every re-run appended another zone. A Part O optimisation round warm starts from a
        /// copy of the canonical TBD - which has already been through this method's caller once - and then
        /// runs the ventilation half of the workflow again, so three units came back as six zones, three of
        /// them stripped of their internal conditions by the remove-by-name step that precedes the rebuild.
        /// </para>
        /// <para>
        /// <b>Two passes, strongest authority first.</b>
        /// </para>
        /// <para>
        /// 1. <b>Identity.</b> A candidate whose description states this unit's guid
        /// (<see cref="PlantZoneIdentity"/>) is its plant zone, whatever either is called. That is unaffected
        /// by renaming the unit and by two units sharing a name.
        /// </para>
        /// <para>
        /// 2. <b>Adoption by name - a fallback for TBDs written before the identity existed.</b> Such a
        /// plant zone states no guid, so without this pass it would be duplicated exactly once more before
        /// the new zone took over. A candidate is adopted when it states NO unit at all and its name matches
        /// the unit's exactly, case-insensitively.
        /// </para>
        /// <para>
        /// <b>What that protects, and what it does not.</b> Only one exclusion applies: a candidate whose
        /// name is also the name of one of the model's own spaces is never adopted. The zone this method's
        /// caller writes is a bare 3 x 3 x 2 box with no space behind it, so a name that belongs to a real
        /// room is a room. That is the whole of the guard.
        /// </para>
        /// <para>
        /// It follows - and is <b>not</b> currently prevented - that a zone a TAS user created themselves,
        /// which is not one of the model's spaces and which happens to be named exactly after an air
        /// handling unit, <b>can be adopted as that unit's plant zone</b>: renamed to the unit (a no-op,
        /// the names already match), stamped with the unit's guid, and given the internal condition and
        /// inter-zone air movements of a generated plant zone. No claim is made here that every
        /// modeller-created zone sharing a unit's name is left alone; only room-named ones are.
        /// </para>
        /// <para>
        /// The exposure is narrow - it needs an exact name collision with an air handling unit on a
        /// non-space zone, in a TBD predating the identity marker, and it cannot recur once the zone is
        /// stamped - and the alternative was worse: without pass 2, every such file duplicates its plant
        /// zones once on the next run. Adoption only ever reuses; it never deletes. Where a stronger
        /// guarantee is wanted, the discriminator to add is a positive marker on the zones this method
        /// writes, not a wider name rule.
        /// </para>
        /// <para>
        /// <b>One zone is claimed at most once.</b> Where a TBD already carries duplicates from before this
        /// fix, or two units genuinely share a name, each unit takes a different zone rather than all of them
        /// converging on the first match. Zones left over from that earlier accumulation are not touched:
        /// they cannot be told apart from a modeller's own work with any confidence, and the contract here is
        /// to stop the growth, not to prune history.
        /// </para>
        /// <para>
        /// Pure SAM.Analytical - no TBD/COM type appears anywhere in this method, which is what lets it be
        /// unit-tested with no TAS licence, install or COM server.
        /// </para>
        /// </summary>
        /// <param name="airHandlingUnits">The units this run is about to write plant zones for.</param>
        /// <param name="plantZoneCandidates">Every zone the TBD currently holds, as name and description.</param>
        /// <param name="spaceNames">The names of the model's spaces - the zones that are rooms, not plant.</param>
        /// <returns>The zone to reuse per air handling unit guid. A unit absent from it needs a new zone.</returns>
        public static Dictionary<Guid, PlantZoneCandidate> ResolvePlantZoneReuse(
            IEnumerable<AirHandlingUnit> airHandlingUnits,
            IEnumerable<PlantZoneCandidate> plantZoneCandidates,
            IEnumerable<string> spaceNames)
        {
            Dictionary<Guid, PlantZoneCandidate> result = new Dictionary<Guid, PlantZoneCandidate>();

            if (airHandlingUnits == null || plantZoneCandidates == null)
            {
                return result;
            }

            List<AirHandlingUnit> airHandlingUnits_Temp = new List<AirHandlingUnit>();
            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                if (airHandlingUnit != null && airHandlingUnit.Guid != Guid.Empty)
                {
                    airHandlingUnits_Temp.Add(airHandlingUnit);
                }
            }

            List<PlantZoneCandidate> candidates = new List<PlantZoneCandidate>();
            foreach (PlantZoneCandidate plantZoneCandidate in plantZoneCandidates)
            {
                if (plantZoneCandidate != null)
                {
                    candidates.Add(plantZoneCandidate);
                }
            }

            HashSet<string> keys_Space = new HashSet<string>();
            foreach (string spaceName in spaceNames ?? new List<string>())
            {
                if (!string.IsNullOrWhiteSpace(spaceName))
                {
                    keys_Space.Add(spaceName.Trim().ToUpper());
                }
            }

            HashSet<int> claimed = new HashSet<int>();

            //Pass 1 - the identity the zone itself states.
            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits_Temp)
            {
                foreach (PlantZoneCandidate candidate in candidates)
                {
                    if (claimed.Contains(candidate.Index))
                    {
                        continue;
                    }

                    if (PlantZoneIdentity.Parse(candidate.Description) != airHandlingUnit.Guid)
                    {
                        continue;
                    }

                    result[airHandlingUnit.Guid] = candidate;
                    claimed.Add(candidate.Index);
                    break;
                }
            }

            //Pass 2 - adopting a plant zone written before the identity existed.
            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits_Temp)
            {
                if (result.ContainsKey(airHandlingUnit.Guid) || string.IsNullOrWhiteSpace(airHandlingUnit.Name))
                {
                    continue;
                }

                string key = airHandlingUnit.Name.Trim().ToUpper();

                //A room keeps its zone. The generated plant zone has no space behind it, so a name that is
                //also a space name cannot be one. This is the ONLY exclusion: a non-space zone a TAS user
                //created, named exactly after a unit, is adoptable - see the summary.
                if (keys_Space.Contains(key))
                {
                    continue;
                }

                foreach (PlantZoneCandidate candidate in candidates)
                {
                    if (claimed.Contains(candidate.Index) || string.IsNullOrWhiteSpace(candidate.Name))
                    {
                        continue;
                    }

                    //Only a zone claiming NO unit is adoptable. One that names a different unit belongs to
                    //that unit, however it is currently spelled.
                    if (PlantZoneIdentity.Parse(candidate.Description) != Guid.Empty)
                    {
                        continue;
                    }

                    if (!string.Equals(candidate.Name.Trim().ToUpper(), key, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    result[airHandlingUnit.Guid] = candidate;
                    claimed.Add(candidate.Index);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// The zones as <see cref="ResolvePlantZoneReuse"/> needs to see them: name and description only, with
        /// the position in <paramref name="zones"/> carried through so the caller can get back to the COM
        /// object. Reading both strings here is the whole of the COM work the resolution requires.
        /// </summary>
        private static List<PlantZoneCandidate> PlantZoneCandidates(List<zone> zones)
        {
            List<PlantZoneCandidate> result = new List<PlantZoneCandidate>();

            if (zones == null)
            {
                return result;
            }

            for (int i = 0; i < zones.Count; i++)
            {
                zone zone = zones[i];
                if (zone == null)
                {
                    continue;
                }

                result.Add(new PlantZoneCandidate(i, zone.name, zone.description));
            }

            return result;
        }

        /// <summary>
        /// The name of the inter-zone air movement that takes an air handling unit's extract air out of the
        /// building, in the same form the space loop names an outward movement.
        /// </summary>
        private static string OutwardIZAMName(AirHandlingUnit airHandlingUnit)
        {
            return string.Format("IZAM {0} TO OUTSIDE", airHandlingUnit?.Name);
        }

        private sealed class SpaceMovementInfo
        {
            public SpaceAirMovement Movement;
            public SAMObject From;
            public SAMObject To;
            public string IZAMName;
        }
    }
}