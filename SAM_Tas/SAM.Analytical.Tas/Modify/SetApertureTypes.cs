// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using TBD;
using SAM.Geometry.Object.Spatial;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// The prefix a note carries when it reports a problem rather than an observation. Callers that
        /// surface notes in a UI raise these differently - the Grasshopper workflow component raises them
        /// as runtime warnings and everything else as remarks.
        /// </summary>
        public const string NotePrefix_Issue = "ISSUE: ";

        /// <summary>
        /// How many detailed lines a single gate may contribute before the rest are counted instead of
        /// listed. A model with a systematically misaligned aperture set would otherwise put one warning on
        /// the Grasshopper canvas per aperture. Whenever the cap bites it is stated explicitly - a truncated
        /// list that does not say it was truncated reads as "that was all of them".
        /// </summary>
        private const int NoteLimit_PerGate = 20;

        /// <summary>
        /// Sets Apertures Types by matching geometry
        /// </summary>
        /// <param name="building"></param>
        /// <param name="adjacencyCluster"></param>
        /// <param name="tolerance"></param>
        public static List<TBD.ApertureType> SetApertureTypes(this Building building, AdjacencyCluster adjacencyCluster, double tolerance = Core.Tolerance.Distance)
        {
            return SetApertureTypes(building, adjacencyCluster, out List<string> _, tolerance);
        }

        /// <summary>
        /// The same match and write, reporting through <paramref name="notes"/> what happened at each gate
        /// for every TBD aperture: whether the pane identity was carried, which SAM aperture the geometry
        /// selected, whether that aperture carried opening properties, whether the TBD building element was
        /// found, and any refusal from the aperture-type write itself.
        /// <para>
        /// <b>This overload changes no export behaviour whatever.</b> The aperture the write is applied to
        /// is selected by exactly the call the previous implementation used, with the same tolerance and the
        /// same first-match semantics. Where more than one SAM aperture face contains the TBD pane's
        /// internal point, that is REPORTED - with the full candidate list and the one production actually
        /// chose - and not acted on, precisely so that a diagnostic run establishes whether the geometry
        /// match is implicated at all before anything about it is changed.
        /// </para>
        /// <para>
        /// The silent <c>continue</c>s of the previous implementation are what let a Part O availability
        /// schedule fail to reach a TBD with nothing said anywhere.
        /// </para>
        /// </summary>
        public static List<TBD.ApertureType> SetApertureTypes(this Building building, AdjacencyCluster adjacencyCluster, out List<string> notes, double tolerance = Core.Tolerance.Distance)
        {
            notes = [];

            if(building == null || adjacencyCluster == null)
            {
                notes.Add(NotePrefix_Issue + "Aperture types: no TBD building or SAM adjacency cluster to match between, so no aperture control was written at all.");
                return null;
            }

            List<buildingElement> buildingElements = building.BuildingElements();
            if (buildingElements == null || buildingElements.Count == 0)
            {
                notes.Add(NotePrefix_Issue + "Aperture types: the TBD carries no building elements, so no aperture control was written at all.");
                return null;
            }

            AdjacencyCluster adjacencyCluster_Temp = building?.ToSAM();
            if(adjacencyCluster_Temp == null)
            {
                notes.Add(NotePrefix_Issue + "Aperture types: the TBD building could not be converted to SAM, so its apertures could not be matched and no aperture control was written at all.");
                return null;
            }

            //in gbXML workflow building is moved (by our interpretation) TBD to 0,0,z this is model wihtout shade so to be able to match by geometry we need to remove shade
            //take boundin box and move building by vector, watch  if we do SAM to TBD without gbXML if we need to do it
            BoundingBox3D boundingBox3D_Temp = adjacencyCluster_Temp.GetPanels().FindAll(x => !adjacencyCluster_Temp.Shade(x)).BoundingBox3D();
            BoundingBox3D boundingBox3D = adjacencyCluster.GetPanels().FindAll(x => !adjacencyCluster.Shade(x)).BoundingBox3D();
            adjacencyCluster_Temp.Transform(Transform3D.GetTranslation(new Vector3D(boundingBox3D_Temp.GetCentroid(), boundingBox3D.GetCentroid())));

            List<Panel> panels_Temp = adjacencyCluster_Temp.GetPanels();
            if(panels_Temp == null || panels_Temp.Count == 0)
            {
                notes.Add(NotePrefix_Issue + "Aperture types: the TBD side carried no panels to match apertures on, so no aperture control was written at all.");
                return null;
            }

            Dictionary<string, buildingElement> buildingElementsByGuid = new Dictionary<string, buildingElement>(buildingElements.Count);
            foreach (buildingElement be in buildingElements)
            {
                if (!string.IsNullOrWhiteSpace(be?.GUID))
                {
                    buildingElementsByGuid[be.GUID] = be;
                }
            }

            // Build once; replaces a per-aperture AdjacencyCluster.Apertures(point, ...) call that
            // re-walked every panel each time (O(apertures * panels) on a 625-zone TM59 model).
            List<Tuple<BoundingBox3D, Aperture>> aperturesIndex = adjacencyCluster.AperturesWithBoundingBoxes();

            //One COM pass over the building's reusable definitions, for the whole match. Without it every
            //opening child re-read every aperture type and every schedule in the building.
            BuildingReuseCache cache = new BuildingReuseCache(building);

            List<TBD.ApertureType> result = new List<TBD.ApertureType>();

            int count_Seen = 0;
            int count_Updated = 0;
            int count_ScheduleRequested = 0;
            int count_ScheduleWritten = 0;

            //One counter per gate, so a cap on the detail lines never hides how many apertures a gate
            //actually stopped.
            Dictionary<string, int> counts_Gate = new Dictionary<string, int>();
            Dictionary<string, int> counts_GateReported = new Dictionary<string, int>();

            foreach(Panel panel_Temp in panels_Temp)
            {
                List<Aperture> apertures_Temp = panel_Temp?.Apertures;
                if(apertures_Temp == null || apertures_Temp.Count == 0)
                {
                    continue;
                }

                foreach(Aperture aperture_Temp in apertures_Temp)
                {
                    count_Seen++;

                    string name_Temp = string.IsNullOrWhiteSpace(aperture_Temp?.Name) ? aperture_Temp?.Guid.ToString() : aperture_Temp.Name;
                    string identity_Temp = string.Format("TBD aperture '{0}' ({1})", name_Temp, aperture_Temp?.Guid);

                    if (!aperture_Temp.TryGetValue(ApertureParameter.PaneBuildingElementGuid, out string GUID) || string.IsNullOrWhiteSpace(GUID))
                    {
                        AddGateNote(notes, counts_Gate, counts_GateReported, "1 pane identity", string.Format("{0}: no PaneBuildingElementGuid, so no TBD building element is identified for it. Gate 1 - the pane identity was not carried through the TBD conversion.", identity_Temp));
                        continue;
                    }

                    Point3D point3D = aperture_Temp.GetFace3D()?.GetInternalPoint3D(tolerance);
                    if (point3D == null)
                    {
                        AddGateNote(notes, counts_Gate, counts_GateReported, "2 geometry", string.Format("{0} (pane {1}): its geometry yields no internal point to match with. Gate 2 - the geometry match could not even be attempted.", identity_Temp, GUID));
                        continue;
                    }

                    //THE PRODUCTION SELECTION, UNCHANGED. Query.Apertures already tests the aperture FACE -
                    //the bounding box inside it is only a pre-filter - and returns the first face-level
                    //match. Nothing below alters which aperture is chosen.
                    Aperture aperture = aperturesIndex?.Apertures(point3D, 1, Core.Tolerance.MacroDistance)?.FirstOrDefault();

                    if (aperture == null)
                    {
                        AddGateNote(notes, counts_Gate, counts_GateReported, "3 no SAM aperture", string.Format("{0} (pane {1}): no SAM aperture face contains its internal point {2} within {3} m. Gate 3 - the TBD side and the SAM side are not aligned here: an offset between the two models, an aperture missing from the SAM side, or a pane point falling outside every SAM aperture face.", identity_Temp, GUID, point3D, Core.Tolerance.MacroDistance));
                        continue;
                    }

                    if(!aperture.TryGetValue(Analytical.ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties) || openingProperties == null)
                    {
                        AddGateNote(notes, counts_Gate, counts_GateReported, "4 no opening properties", string.Format("{0} (pane {1}) matched SAM aperture '{2}' ({3}), but that aperture carries no OpeningProperties, so there is no aperture control to write. Gate 4 - the SAM-side opening properties.", identity_Temp, GUID, aperture.Name, aperture.Guid));
                        continue;
                    }

                    //Whether this aperture is one the Part O work cares about is decided BEFORE the write, so
                    //that an interesting aperture is reported in full and an ordinary one contributes only to
                    //the counters. Hundreds of "window with no schedule updated" remarks would bury the few
                    //lines that matter.
                    bool scheduleRequested = TryDescribeScheduleRequest(openingProperties, out string description_Request);

                    //Per CHILD: a multiple-opening aperture can state one schedule request per child, and
                    //each is verified below against the aperture type THAT child's write returned.
                    List<bool> scheduleRequests = openingProperties.OpeningScheduleRequests();
                    int count_Requested = scheduleRequests.FindAll(x => x).Count;
                    if (scheduleRequested)
                    {
                        count_ScheduleRequested += count_Requested;
                    }

                    if(!buildingElementsByGuid.TryGetValue(GUID, out buildingElement buildingElement) || buildingElement == null)
                    {
                        AddGateNote(notes, counts_Gate, counts_GateReported, "5 building element", string.Format("{0} matched SAM aperture '{1}' ({2}){3}, but no TBD building element carries the pane GUID '{4}'. Gate 5 - the building element identity.", identity_Temp, aperture.Name, aperture.Guid, scheduleRequested ? " requesting " + description_Request : string.Empty, GUID));
                        continue;
                    }

                    List<TBD.ApertureType> apertureTypes = SetApertureTypes(building, buildingElement, openingProperties, out List<string> notes_Write, out List<int> childIndices, null, cache);
                    if(apertureTypes == null || apertureTypes.Count == 0)
                    {
                        notes.AddRange(notes_Write ?? []);
                        AddGateNote(notes, counts_Gate, counts_GateReported, "6 aperture type write", string.Format("{0} matched SAM aperture '{1}' ({2}){3} and TBD building element '{4}' ({5}), but no aperture type was written. Gate 6 - the write itself; the refusal reported alongside this line names the reason.", identity_Temp, aperture.Name, aperture.Guid, scheduleRequested ? " requesting " + description_Request : string.Empty, buildingElement.name, GUID));
                        continue;
                    }

                    count_Updated++;
                    result.AddRange(apertureTypes);

                    //Refusals from a partially successful multiple-opening write are reported even where at
                    //least one aperture type came back.
                    notes.AddRange((notes_Write ?? []).FindAll(x => x.StartsWith(NotePrefix_Issue, StringComparison.Ordinal)));

                    if (!scheduleRequested)
                    {
                        //An ordinary opening with no availability schedule. Counted, not narrated.
                        continue;
                    }

                    //An aperture that DOES carry a Part O restriction or an explicit schedule has the
                    //schedule read back off the TBD rather than restated from the SAM side - a note that
                    //merely echoed the request would claim success the TBD had not delivered. Each
                    //requesting child is checked against the aperture type ITS OWN write returned, using
                    //the correspondence the write reported - never against the first aperture type, which
                    //can belong to a different child of a multiple-opening aperture. Success is counted and
                    //says nothing; only a schedule that did not arrive is narrated.
                    ScheduleDeliveryByChild(apertureTypes, childIndices, scheduleRequests.Count, out bool[] delivered, out TBD.ApertureType[] apertureTypesByChild);

                    List<int> undelivered = openingProperties.UndeliveredOpeningScheduleRequests(delivered);
                    count_ScheduleWritten += count_Requested - undelivered.Count;

                    foreach (int childIndex in undelivered)
                    {
                        TBD.ApertureType apertureType_Child = apertureTypesByChild[childIndex];
                        notes.Add(NotePrefix_Issue + string.Format("{0} matched SAM aperture '{1}' ({2}); {3}; TBD building element '{4}' ({5}); requested {6}; but the schedule for {7} did not arrive - {8}",
                            identity_Temp,
                            aperture.Name,
                            aperture.Guid,
                            DescribeOpeningProperties(openingProperties),
                            buildingElement.name,
                            GUID,
                            description_Request,
                            scheduleRequests.Count == 1 ? "the opening" : string.Format("opening {0} of {1}", childIndex + 1, scheduleRequests.Count),
                            apertureType_Child == null
                                ? "its write was refused - the refusal reported alongside this line names the reason."
                                : string.Format("aperture type '{0}' carries no schedule afterwards - it read back as {1}", apertureType_Child.name, DescribeApertureTypeProfile(apertureType_Child) ?? "NO PROFILE - the aperture type came back without a TBD profile to read.")));
                    }

                    //This is the ONLY place the candidate set is examined, and it is examined for the record,
                    //never to select: it establishes whether the first-match rule above had a choice to get
                    //wrong before anything about that rule is changed.
                    List<Aperture> apertures_Candidate = aperturesIndex?.Apertures(point3D, int.MaxValue, Core.Tolerance.MacroDistance);
                    if (apertures_Candidate != null && apertures_Candidate.Count > 1)
                    {
                        notes.Add(NotePrefix_Issue + string.Format("{0}: {1} SAM aperture faces contain its internal point, so the first-match rule had a choice. Production selected '{2}' ({3}); the full candidate list is {4}. The export was NOT changed by this - it is reported so the selection can be checked against the model.", identity_Temp, apertures_Candidate.Count, aperture.Name, aperture.Guid, string.Join(", ", apertures_Candidate.ConvertAll(x => string.Format("'{0}' ({1})", x?.Name, x?.Guid)))));
                    }
                }
            }

            //Inserted at the front so the summary is the first thing a reader sees, and so a run in which
            //nothing was requested says so rather than saying nothing.
            List<string> notes_Summary = [];
            notes_Summary.Add(string.Format("Aperture types: {0} TBD apertures seen, {1} updated, {2} opening(s) requested an availability schedule, {3} of those read a schedule back off the TBD profile.", count_Seen, count_Updated, count_ScheduleRequested, count_ScheduleWritten));

            foreach (KeyValuePair<string, int> keyValuePair in counts_Gate)
            {
                counts_GateReported.TryGetValue(keyValuePair.Key, out int reported);
                notes_Summary.Add(string.Format("{0}Aperture types: {1} aperture(s) stopped at gate {2}{3}.", keyValuePair.Value == 0 ? string.Empty : NotePrefix_Issue, keyValuePair.Value, keyValuePair.Key, reported < keyValuePair.Value ? string.Format("; {0} listed individually above, the remaining {1} are counted only", reported, keyValuePair.Value - reported) : string.Empty));
            }

            if (count_ScheduleRequested != count_ScheduleWritten)
            {
                notes_Summary.Add(NotePrefix_Issue + string.Format("Aperture types: {0} of {1} requested availability schedules did NOT read one back off the TBD. The per-aperture lines above name which opening, and at which gate.", count_ScheduleRequested - count_ScheduleWritten, count_ScheduleRequested));
            }

            notes.InsertRange(0, notes_Summary);

            return result;
        }

        public static List<TBD.ApertureType> SetApertureTypes(this Building building, buildingElement buildingElement, IOpeningProperties openingProperties, string name = null, BuildingReuseCache cache = null)
        {
            return SetApertureTypes(building, buildingElement, openingProperties, out List<string> _, name, cache);
        }

        /// <summary>
        /// The same write, collecting the refusal of the underlying
        /// <see cref="SetApertureType(Building, buildingElement, ISingleOpeningProperties, out string, string, int)"/>
        /// call instead of discarding it - the drop that previously made an incompatible or unprovable
        /// schedule invisible to the workflow.
        /// </summary>
        public static List<TBD.ApertureType> SetApertureTypes(this Building building, buildingElement buildingElement, IOpeningProperties openingProperties, out List<string> notes, string name = null, BuildingReuseCache cache = null)
        {
            return SetApertureTypes(building, buildingElement, openingProperties, out notes, out List<int> _, name, cache);
        }

        /// <summary>
        /// The same write, additionally reporting the CORRESPONDENCE between the opening's child properties
        /// and the aperture types that came back: <paramref name="childIndices"/> runs parallel to the
        /// returned list and holds, for each returned aperture type, the index of the child opening property
        /// whose write produced it (always 0 for a single opening). A refused child is simply absent - the
        /// returned list is not padded - so the correspondence survives a <see cref="MultipleOpeningProperties"/>
        /// in which only some children were written. Callers verifying "the schedule this child requested is
        /// on the aperture type this child got" need exactly this; checking the FIRST returned aperture type
        /// instead is what made a schedule delivered to a later child look missing, and one missing from a
        /// later child look delivered.
        /// <para>
        /// <b>Multiplicity is preserved exactly.</b> N opening children produce N distinct TBD aperture
        /// types even when the children are identical: TAS keeps one entry per type on an element, so
        /// assigning one type twice would collapse a two-opening window into a one-opening window. What
        /// identical children DO share is with other elements - every window's second identical opening
        /// uses the one type keyed at ordinal 2. See
        /// <see cref="Query.ApertureTypeOrdinals(IEnumerable{ApertureTypeDefinition})"/>.
        /// </para>
        /// </summary>
        /// <param name="cache">The open document's reuse cache, or null to build a single-use one.</param>
        public static List<TBD.ApertureType> SetApertureTypes(this Building building, buildingElement buildingElement, IOpeningProperties openingProperties, out List<string> notes, out List<int> childIndices, string name = null, BuildingReuseCache cache = null)
        {
            notes = [];
            childIndices = null;

            if (building == null || buildingElement == null || openingProperties == null)
            {
                notes.Add(NotePrefix_Issue + "Aperture type: no TBD building, building element or opening properties to write one from.");
                return null;
            }

            BuildingReuseCache cache_Temp = cache ?? new BuildingReuseCache(building);

            if(openingProperties is ISingleOpeningProperties)
            {
                TBD.ApertureType apertureType = SetApertureType(building, buildingElement, (ISingleOpeningProperties)openingProperties, out string refusal, name, -1, cache_Temp, 1);
                if(apertureType == null)
                {
                    notes.Add(NotePrefix_Issue + string.Format("Aperture type '{0}': {1}", name ?? buildingElement.name, refusal ?? "the aperture type could not be written, and the write reported no reason."));
                    return null;
                }

                childIndices = new List<int>() { 0 };
                return new List<TBD.ApertureType>() { apertureType };
            }

            if(openingProperties is MultipleOpeningProperties)
            {
                List<ISingleOpeningProperties> singleOpeningProperties = ((MultipleOpeningProperties)openingProperties).SingleOpeningProperties;
                if(singleOpeningProperties == null)
                {
                    notes.Add(NotePrefix_Issue + string.Format("Aperture type '{0}': the multiple opening properties carried no single opening properties.", name ?? buildingElement.name));
                    return null;
                }

                //Resolved COM-free, for the whole element at once, because a child's ordinal is a property
                //of the children BEFORE it: two identical children are occurrence 1 and occurrence 2 of one
                //definition, whereas two different children are both occurrence 1 of their own.
                List<ApertureTypeDefinition> apertureTypeDefinitions = new List<ApertureTypeDefinition>();
                List<string> dayTypeNames = cache_Temp.DayTypeNames;
                foreach (ISingleOpeningProperties single in singleOpeningProperties)
                {
                    apertureTypeDefinitions.Add(single.ApertureTypeDefinition(dayTypeNames, out string _));
                }

                List<int> ordinals = Query.ApertureTypeOrdinals(apertureTypeDefinitions);

                List<TBD.ApertureType> result = new List<TBD.ApertureType>();
                childIndices = new List<int>();
                for (int i =0; i < singleOpeningProperties.Count; i++)
                {
                    int index = singleOpeningProperties.Count == 1 ? -1 : i + 1;

                    //index: named deliberately - the name argument stays defaulted here, as it always has.
                    //Passing the caller's name into this branch would rename every aperture type a
                    //multiple-opening aperture produces.
                    TBD.ApertureType apertureType = SetApertureType(building, buildingElement, singleOpeningProperties[i], out string refusal, null, index, cache_Temp, i < ordinals.Count ? ordinals[i] : -1);
                    if (apertureType == null)
                    {
                        notes.Add(NotePrefix_Issue + string.Format("Aperture type '{0}' (opening {1} of {2}): {3}", buildingElement.name, i + 1, singleOpeningProperties.Count, refusal ?? "the aperture type could not be written, and the write reported no reason."));
                        continue;
                    }

                    result.Add(apertureType);
                    childIndices.Add(i);
                }

                return result;
            }

            notes.Add(NotePrefix_Issue + string.Format("Aperture type '{0}': opening properties of type {1} are neither single nor multiple, so no aperture control was written.", name ?? buildingElement.name, openingProperties.GetType().Name));

            return null;
        }

        /// <summary>
        /// Maps a write's returned aperture types back onto the opening's child properties:
        /// <paramref name="delivered"/>[i] is whether the aperture type child i's write returned carries a
        /// schedule read back off the TBD, and <paramref name="apertureTypesByChild"/>[i] is that aperture
        /// type itself (null where child i's write was refused, so a failure can still be described).
        /// <paramref name="childIndices"/> is the correspondence the write reported - parallel to
        /// <paramref name="apertureTypes"/>, the child index each returned aperture type came from - so a
        /// compacted, partially successful multiple-opening write cannot shift a later child into child 0's
        /// slot.
        /// </summary>
        private static void ScheduleDeliveryByChild(List<TBD.ApertureType> apertureTypes, List<int> childIndices, int childCount, out bool[] delivered, out TBD.ApertureType[] apertureTypesByChild)
        {
            delivered = new bool[childCount];
            apertureTypesByChild = new TBD.ApertureType[childCount];

            if (apertureTypes == null || childIndices == null)
            {
                return;
            }

            for (int i = 0; i < apertureTypes.Count; i++)
            {
                int childIndex = i < childIndices.Count ? childIndices[i] : -1;
                if (childIndex < 0 || childIndex >= childCount)
                {
                    continue;
                }

                apertureTypesByChild[childIndex] = apertureTypes[i];
                delivered[childIndex] = ApertureTypeSchedule(apertureTypes[i]) != null;
            }
        }

        /// <summary>
        /// Records a gate stop, listing the first <see cref="NoteLimit_PerGate"/> individually and counting
        /// the rest. The caller's summary states the count and how many were listed.
        /// </summary>
        private static void AddGateNote(List<string> notes, Dictionary<string, int> counts_Gate, Dictionary<string, int> counts_GateReported, string gate, string note)
        {
            counts_Gate.TryGetValue(gate, out int count);
            counts_Gate[gate] = count + 1;

            counts_GateReported.TryGetValue(gate, out int reported);
            if (reported >= NoteLimit_PerGate)
            {
                return;
            }

            counts_GateReported[gate] = reported + 1;
            notes.Add(NotePrefix_Issue + note);
        }

        /// <summary>
        /// Whether the opening states an availability schedule the TBD aperture control must carry, and a
        /// one-line description of it. Uses the same resolution the write itself uses, so the reported
        /// request cannot drift from the requested one.
        /// </summary>
        private static bool TryDescribeScheduleRequest(IOpeningProperties openingProperties, out string description)
        {
            description = null;

            List<ISingleOpeningProperties> singleOpeningProperties = [];
            if (openingProperties is ISingleOpeningProperties single)
            {
                singleOpeningProperties.Add(single);
            }
            else if (openingProperties is MultipleOpeningProperties multiple && multiple.SingleOpeningProperties != null)
            {
                singleOpeningProperties.AddRange(multiple.SingleOpeningProperties);
            }

            List<string> descriptions = [];
            foreach (ISingleOpeningProperties item in singleOpeningProperties)
            {
                if (item == null)
                {
                    continue;
                }

                bool requested = item.TryGetOpeningScheduleSource(out string name_Schedule, out int[] values_Schedule, out string refusal_Source);
                if (refusal_Source != null)
                {
                    //An opening that STATES a schedule it cannot supply is exactly the case worth surfacing.
                    descriptions.Add(string.Format("an unusable schedule source ({0})", refusal_Source));
                    continue;
                }

                if (!requested)
                {
                    continue;
                }

                descriptions.Add(string.Format("schedule '{0}' [{1}]", name_Schedule, DescribeValues(values_Schedule)));
            }

            if (descriptions.Count == 0)
            {
                return false;
            }

            description = string.Join("; ", descriptions);
            return true;
        }

        /// <summary>
        /// The SAM-side opening properties, named concretely - a PartOOpeningProperties carrying a
        /// restriction and a ProfileOpeningProperties carrying an explicit schedule are the two shapes the
        /// Part O route produces, and telling them apart in the log is what identifies which one arrived.
        /// </summary>
        private static string DescribeOpeningProperties(IOpeningProperties openingProperties)
        {
            if (openingProperties == null)
            {
                return "no opening properties";
            }

            if (openingProperties is PartOOpeningProperties partOOpeningProperties)
            {
                return string.Format("PartOOpeningProperties, OpeningRestriction {0}, Schedule '{1}'", partOOpeningProperties.OpeningRestriction, partOOpeningProperties.Schedule?.Name ?? "(none)");
            }

            if (openingProperties is ProfileOpeningProperties profileOpeningProperties)
            {
                return string.Format("ProfileOpeningProperties, Schedule '{0}', legacy Profile '{1}'", profileOpeningProperties.Schedule?.Name ?? "(none)", profileOpeningProperties.Profile?.Name ?? "(none)");
            }

            return openingProperties.GetType().Name;
        }

        /// <summary>
        /// The schedule the TBD profile actually carries, READ BACK off the object the write returned, or
        /// null where the aperture type exposes no profile or that profile points at no schedule. This is
        /// the distinction the whole Part O schedule investigation turned on - "SAM asked for a schedule"
        /// against "the TBD profile carries one" - and it is what the requested/written counters compare.
        /// <para>
        /// Presence is all that is checked here: the values are already verified against the request inside
        /// <see cref="SetApertureType(Building, buildingElement, ISingleOpeningProperties, out string, string, int)"/>,
        /// which refuses rather than assigning a schedule that does not match.
        /// </para>
        /// </summary>
        private static schedule ApertureTypeSchedule(TBD.ApertureType apertureType)
        {
            if (apertureType == null)
            {
                return null;
            }

            dynamic @dynamic = apertureType;

            profile profile = @dynamic.GetProfile();

            return profile?.schedule;
        }

        /// <summary>
        /// The TBD aperture type's profile, READ BACK off the object the write returned: type, factor,
        /// setback, function and the schedule it points at with that schedule's own 24 values. Used only to
        /// describe a FAILURE - an aperture that asked for a schedule and did not get one - where knowing
        /// what the profile does carry is what identifies the reason. Returns null where the aperture type
        /// exposes no profile at all.
        /// </summary>
        private static string DescribeApertureTypeProfile(TBD.ApertureType apertureType)
        {
            if (apertureType == null)
            {
                return null;
            }

            dynamic @dynamic = apertureType;

            profile profile = @dynamic.GetProfile();
            if (profile == null)
            {
                return null;
            }

            schedule schedule = profile.schedule;

            return string.Format("type {0}, factor {1:0.000}, setback {2:0.000}, function '{3}', schedule {4}",
                profile.type,
                profile.factor,
                profile.setbackValue,
                profile.function,
                schedule == null ? "NONE - the profile carries no schedule" : string.Format("'{0}' [{1}]", schedule.name, DescribeValues(schedule.HourlyValues())));
        }

        /// <summary>
        /// 24 hourly values as a compact run of digits, so a whole schedule fits on the line that reports it
        /// and two schedules can be compared by eye.
        /// </summary>
        private static string DescribeValues(int[] values)
        {
            if (values == null)
            {
                return "no values";
            }

            return string.Concat(values.Select(x => x == 0 ? "0" : x == 1 ? "1" : "?"));
        }
    }
}
