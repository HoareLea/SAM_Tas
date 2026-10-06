// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>One COM pass over a TBD's reusable definitions, held for the length of one open-document
    /// operation.</b>
    /// <para>
    /// A <c>TBD.schedule</c>, a <c>TBD.ApertureType</c>, a <c>TBD.Construction</c> and an aperture's
    /// <c>TBD.buildingElement</c> are all building-level definitions shared by every element that needs
    /// them, so an export's job is to find the one that already says what this window says and assign it -
    /// not to make another. Finding it used to mean re-reading the building: the aperture-control write
    /// scanned every aperture type in the building TWICE per opening child, and the schedule write rebuilt
    /// every schedule's 24 values per child. On a model with hundreds of windows that is the quadratic
    /// term. This class reads each collection once and answers from memory.
    /// </para>
    /// <para>
    /// <b>Four collections, one discipline.</b> Schedules and aperture types are Stage 1; aperture
    /// constructions and aperture building elements are Stage 2, and they are held exactly the same way -
    /// classified once, matched by full definitional equality and never by name, registered as reusable only
    /// after a write has completed, and reserved by name from the moment an object exists.
    /// Every construction and element NAME is held, because a name occupies the namespace whatever it names,
    /// but only APERTURE ones are ever classified or offered for reuse: a panel's element and construction
    /// are resolved by the direct export's own by-name dictionaries and this class takes no part in that.
    /// </para>
    /// <para>
    /// <b>Lifetime is one open document.</b> It holds live COM references, and the workflow re-opens the
    /// TBD between steps, so a cache kept across steps would hold dead pointers. Construct it where the
    /// document is opened and let it go when the document closes.
    /// </para>
    /// <para>
    /// <b>Nothing here writes.</b> The cache reads, classifies and remembers; creation and assignment stay
    /// with the callers, which register what they created so the next opening finds it. A seeded definition
    /// this export may not reuse is kept as a NAME only - so a new definition cannot collide with it - and
    /// the reason is available for reporting.
    /// </para>
    /// <para>
    /// <b>Reusable registration and name reservation are two different things.</b> A schedule or aperture
    /// type is registered as REUSABLE only after its write has fully succeeded and read back as requested.
    /// But the object exists in the TBD from the moment it is created and named - and there is no
    /// withdrawing it (no <c>RemoveSchedule</c>, and a created aperture type is left in place by policy) -
    /// so a creation whose write later fails is RESERVED by name immediately: it can never be matched as a
    /// reusable definition, yet its name still occupies the namespace and the next creation cannot
    /// accidentally choose it again.
    /// </para>
    /// </summary>
    public sealed class BuildingReuseCache
    {
        private sealed class ApertureTypeEntry
        {
            public TBD.ApertureType ApertureType;
            public string Name;
            public ApertureTypeDefinition Definition;
            public int Ordinal;
            public string Refusal;
        }

        private sealed class ConstructionEntry
        {
            public TBD.Construction Construction;
            public string Name;
            public ConstructionDefinition Definition;
            public string Refusal;

            /// <summary>A construction that was in the TBD before this export, still awaiting its content read.</summary>
            public bool Seed_Unclassified;
        }

        private sealed class BuildingElementEntry
        {
            public TBD.buildingElement BuildingElement;
            public string Name;
            public BuildingElementDefinition Definition;
            public string Refusal;

            /// <summary>An element that was in the TBD before this export, still awaiting its content read.</summary>
            public bool Seed_Unclassified;
        }

        private readonly List<KeyValuePair<TBD.schedule, int[]>> schedules = new List<KeyValuePair<TBD.schedule, int[]>>();
        private readonly List<ApertureTypeEntry> apertureTypes = new List<ApertureTypeEntry>();
        private readonly List<ConstructionEntry> constructions = new List<ConstructionEntry>();
        private readonly List<BuildingElementEntry> buildingElements = new List<BuildingElementEntry>();
        private readonly List<TBD.dayType> dayTypes = new List<TBD.dayType>();
        private readonly List<string> dayTypeNames = new List<string>();

        //Names of schedules created and named this session whose write did not complete. The schedule
        //stays in the TBD - there is no RemoveSchedule - but it is never registered as reusable; the name
        //still occupies the namespace.
        private readonly HashSet<string> scheduleNames_Reserved = new HashSet<string>();

        //Whether the deferred content read over the constructions and aperture building elements that were
        //already in the TBD has happened. See EnsureSeedClassification.
        private bool classified_Seeds;

        //Captured the first time an element is touched, i.e. BEFORE this export assigns anything to it.
        //That snapshot is what "already carried" means; everything after it is this export's own work.
        private readonly Dictionary<string, List<KeyValuePair<string, ApertureTypeDefinition>>> assignments_Existing = new Dictionary<string, List<KeyValuePair<string, ApertureTypeDefinition>>>();
        private readonly Dictionary<string, List<TBD.ApertureType>> assignments_ExistingApertureTypes = new Dictionary<string, List<TBD.ApertureType>>();
        private readonly Dictionary<string, HashSet<string>> assignments_Current = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Reads the building's schedules, aperture types and day types, and the NAMES of its constructions
        /// and building elements - one pass, no per-object content beyond that. The content read that decides
        /// construction and element reuse is deferred to the first lookup that needs it; see
        /// <see cref="EnsureSeedClassification"/>.
        /// </summary>
        public BuildingReuseCache(TBD.Building building)
        {
            Building = building;

            if (building == null)
            {
                return;
            }

            schedules.AddRange(building.SchedulesWithValues());

            List<TBD.ApertureType> apertureTypes_Building = building.ApertureTypes();
            if (apertureTypes_Building != null)
            {
                foreach (TBD.ApertureType apertureType in apertureTypes_Building)
                {
                    if (apertureType == null)
                    {
                        continue;
                    }

                    ApertureTypeDefinition apertureTypeDefinition = apertureType.ApertureTypeDefinition(out string refusal);

                    Query.TryDecomposeApertureTypeName(apertureType.name, out string _, out int ordinal);

                    apertureTypes.Add(new ApertureTypeEntry
                    {
                        ApertureType = apertureType,
                        Name = apertureType.name,
                        Definition = apertureTypeDefinition,
                        Ordinal = ordinal,
                        Refusal = refusal
                    });
                }
            }

            //The set every aperture control this export writes is assigned to - HDD and CDD excluded, as
            //the write has always excluded them.
            List<TBD.dayType> dayTypes_Building = building.DayTypes();
            if (dayTypes_Building != null)
            {
                foreach (TBD.dayType dayType in dayTypes_Building)
                {
                    if (dayType == null || dayType.name == "HDD" || dayType.name == "CDD")
                    {
                        continue;
                    }

                    dayTypes.Add(dayType);
                    dayTypeNames.Add(dayType.name);
                }
            }

            //Constructions and building elements: only their NAMES are read here. A name is what occupies the
            //namespace, and it is one property read; the CONTENT read that decides reuse is deferred to
            //EnsureSeedClassification, because a great many exports never need it. Modify.UpdateIZAMs, for
            //one, re-enters Modify.Update once per air handling unit with a synthetic, aperture-less cluster:
            //classifying every seeded construction's layers on each of those passes would be a per-IZAM COM
            //cost paid for an answer nothing asks for.
            List<TBD.Construction> constructions_Building = building.Constructions();
            if (constructions_Building != null)
            {
                foreach (TBD.Construction construction in constructions_Building)
                {
                    if (construction == null)
                    {
                        continue;
                    }

                    constructions.Add(new ConstructionEntry
                    {
                        Construction = construction,
                        Name = construction.name,
                        Definition = null,
                        Refusal = null,
                        Seed_Unclassified = true
                    });
                }
            }

            List<TBD.buildingElement> buildingElements_Building = building.BuildingElements();
            if (buildingElements_Building != null)
            {
                foreach (TBD.buildingElement buildingElement in buildingElements_Building)
                {
                    if (buildingElement == null)
                    {
                        continue;
                    }

                    buildingElements.Add(new BuildingElementEntry
                    {
                        BuildingElement = buildingElement,
                        Name = buildingElement.name,
                        Definition = null,
                        Refusal = null,
                        Seed_Unclassified = true
                    });
                }
            }
        }

        /// <summary>
        /// Reads what the constructions and aperture building elements that were ALREADY in the TBD hold -
        /// once, the first time a reuse lookup needs to know, and never again.
        /// <para>
        /// Deferred rather than done in the constructor because the answer is only ever needed by an export
        /// that actually places an aperture, and because the cheap half of it - the names, which is what
        /// collision avoidance needs - is already in hand. Only objects carrying this export's own
        /// <c>-pane</c>/<c>-frame</c> naming are read at all: a panel's construction can never be an aperture
        /// reuse candidate, so reading its layers would be COM traffic spent on an answer that is already
        /// "no". Elements are classified after constructions because classifying an element reads its
        /// construction.
        /// </para>
        /// </summary>
        private void EnsureSeedClassification()
        {
            if (classified_Seeds)
            {
                return;
            }

            //Set BEFORE the pass: classifying a building element reads its openings, and this must not
            //re-enter itself part way through.
            classified_Seeds = true;

            foreach (ConstructionEntry constructionEntry in constructions)
            {
                if (!constructionEntry.Seed_Unclassified)
                {
                    continue;
                }

                constructionEntry.Seed_Unclassified = false;

                if (!Query.TryDecomposeConstructionName(constructionEntry.Name, out string _, out AperturePart _))
                {
                    constructionEntry.Refusal = "TBD construction outside this export's aperture naming convention.";
                    continue;
                }

                constructionEntry.Definition = constructionEntry.Construction.ConstructionDefinition(out string refusal);
                constructionEntry.Refusal = refusal;
            }

            foreach (BuildingElementEntry buildingElementEntry in buildingElements)
            {
                if (!buildingElementEntry.Seed_Unclassified)
                {
                    continue;
                }

                buildingElementEntry.Seed_Unclassified = false;

                if (!Query.TryDecomposeBuildingElementName(buildingElementEntry.Name, out string _, out ApertureType _, out AperturePart _))
                {
                    buildingElementEntry.Refusal = "TBD building element outside this export's aperture naming convention.";
                    continue;
                }

                buildingElementEntry.Definition = buildingElementEntry.BuildingElement.BuildingElementDefinition(this, out string refusal);
                buildingElementEntry.Refusal = refusal;
            }
        }

        /// <summary>
        /// <b>Refuses named seeded constructions and building elements, keeping their NAMES only.</b> After
        /// this they can never be matched as reusable definitions, while their names still occupy the
        /// namespace so a new definition cannot collide with them - the same "kept as a NAME only" treatment
        /// a seed that fails its own gate receives.
        /// <para>
        /// <b>Why the gbXML route needs it.</b> TAS's own gbXML conversion creates one construction and one
        /// building element per aperture per part, named after the T3D window - which carries the physical
        /// aperture GUID. Those objects can pass every content gate, so the cache would happily hand one over
        /// as reusable, leaving twenty identical windows sharing a definition named after whichever one of
        /// them was enumerated first. Sharing and instance-naming are mutually exclusive: an instance-named
        /// definition can never be found again by anything but itself. So the gbXML pass refuses them up
        /// front (see <c>Query.NamesContainingApertureGuid</c>) and creates definition-named ones instead.
        /// </para>
        /// <para>
        /// Objects this export itself created and registered are untouched by a later call: only entries
        /// whose name is listed are refused, and this pass lists names read off the building before it wrote
        /// anything.
        /// </para>
        /// </summary>
        /// <param name="names">The construction and building element names to refuse. Nulls ignored.</param>
        /// <param name="refusal">Why, for reporting. A default is used when null.</param>
        /// <returns>How many entries were refused.</returns>
        public int RefuseSeededDefinitions(IEnumerable<string> names, string refusal = null)
        {
            if (names == null)
            {
                return 0;
            }

            HashSet<string> names_Temp = new HashSet<string>(names.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (names_Temp.Count == 0)
            {
                return 0;
            }

            //The content read must have happened first, or a refusal set here would be overwritten by the
            //classification that runs on the next lookup.
            EnsureSeedClassification();

            string refusal_Temp = refusal ?? "Definition named after a physical aperture, so it states an instance rather than a reusable definition and was not reused.";

            int result = 0;

            foreach (ConstructionEntry constructionEntry in constructions)
            {
                if (constructionEntry.Name != null && names_Temp.Contains(constructionEntry.Name) && constructionEntry.Definition != null)
                {
                    constructionEntry.Definition = null;
                    constructionEntry.Refusal = refusal_Temp;
                    result++;
                }
            }

            foreach (BuildingElementEntry buildingElementEntry in buildingElements)
            {
                if (buildingElementEntry.Name != null && names_Temp.Contains(buildingElementEntry.Name) && buildingElementEntry.Definition != null)
                {
                    buildingElementEntry.Definition = null;
                    buildingElementEntry.Refusal = refusal_Temp;
                    result++;
                }
            }

            return result;
        }

        /// <summary>The building this cache was read from.</summary>
        public TBD.Building Building { get; }

        /// <summary>
        /// The day types every aperture control this export writes is assigned to, in building order.
        /// </summary>
        public List<TBD.dayType> DayTypes
        {
            get { return new List<TBD.dayType>(dayTypes); }
        }

        /// <summary>
        /// The names of <see cref="DayTypes"/> - the day-type membership half of an
        /// <see cref="ApertureTypeDefinition"/> this export authors.
        /// </summary>
        public List<string> DayTypeNames
        {
            get { return new List<string>(dayTypeNames); }
        }

        /// <summary>
        /// Every schedule in the building paired with its 24 values, in building order, including any this
        /// export registered - the input to value-based schedule reuse, exactly as
        /// <c>Query.SchedulesWithValues</c> supplies it when there is no cache.
        /// </summary>
        public List<KeyValuePair<TBD.schedule, int[]>> SchedulesWithValues()
        {
            return new List<KeyValuePair<TBD.schedule, int[]>>(schedules);
        }

        /// <summary>Remembers a schedule this export created, so the next opening reuses it.</summary>
        public void RegisterSchedule(TBD.schedule schedule, IEnumerable<int> values)
        {
            if (schedule == null)
            {
                return;
            }

            schedules.Add(new KeyValuePair<TBD.schedule, int[]>(schedule, values?.ToArray()));
        }

        /// <summary>
        /// Reserves a schedule name WITHOUT registering anything as reusable. Called the moment a created
        /// schedule is named: if the subsequent write or read-back fails, the schedule remains in the TBD
        /// but must never be handed out as reusable - while its name still occupies the namespace, so the
        /// next creation cannot accidentally choose it again.
        /// </summary>
        public void ReserveScheduleName(string name)
        {
            if (name != null)
            {
                scheduleNames_Reserved.Add(name);
            }
        }

        /// <summary>
        /// Every schedule name this export must treat as occupied: the reusable schedules above PLUS any
        /// reserved by <see cref="ReserveScheduleName(string)"/>. A schedule whose write failed contributes
        /// its name from here and nothing else.
        /// </summary>
        public List<string> ScheduleNames()
        {
            List<string> result = schedules.Select(x => x.Key?.name).Where(x => x != null).ToList();
            foreach (string name in scheduleNames_Reserved)
            {
                if (!result.Contains(name))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        /// <summary>
        /// Every aperture type name in the building, including this export's own creations and including
        /// types that may not be reused - a name occupies the namespace whatever its content, so a new
        /// type must not be given it.
        /// </summary>
        public List<string> ApertureTypeNames()
        {
            return apertureTypes.Select(x => x.Name).ToList();
        }

        /// <summary>
        /// The reusable aperture type for <paramref name="apertureTypeDefinition"/> at
        /// <paramref name="ordinal"/>, or null.
        /// <para>
        /// Matching consults REUSABLE entries only. Identity is the definition; the name takes no part.
        /// The ordinal is part of the key because TAS keeps one entry per type on an element, so an
        /// element's second identical opening needs a second, distinct type - which is itself shared with
        /// every other element's second identical opening.
        /// </para>
        /// </summary>
        public TBD.ApertureType FindApertureType(ApertureTypeDefinition apertureTypeDefinition, int ordinal)
        {
            if (apertureTypeDefinition == null)
            {
                return null;
            }

            int ordinal_Temp = ordinal < 1 ? 1 : ordinal;

            foreach (ApertureTypeEntry apertureTypeEntry in apertureTypes)
            {
                if (apertureTypeEntry.Definition == null || apertureTypeEntry.Ordinal != ordinal_Temp)
                {
                    continue;
                }

                if (apertureTypeEntry.Definition.Equals(apertureTypeDefinition))
                {
                    return apertureTypeEntry.ApertureType;
                }
            }

            return null;
        }

        /// <summary>
        /// The aperture type carrying <paramref name="name"/>, or null - the cached equivalent of a full
        /// <c>building.ApertureTypes()</c> scan, and it includes this export's own creations.
        /// </summary>
        public TBD.ApertureType FindApertureTypeByName(string name)
        {
            if (name == null)
            {
                return null;
            }

            return apertureTypes.Find(x => x.Name == name)?.ApertureType;
        }

        /// <summary>Why a seeded aperture type may not be reused, or null.</summary>
        public string ApertureTypeRefusal(string name)
        {
            return name == null ? null : apertureTypes.Find(x => x.Name == name)?.Refusal;
        }

        /// <summary>
        /// Remembers an aperture type this export created and VERIFIED, so the next opening reuses it.
        /// <para>
        /// A type reserved at creation time by <see cref="ReserveApertureType(TBD.ApertureType, int, string)"/>
        /// is upgraded in place, identified by the SAME COM reference rather than by name - generated names
        /// are unique per session, but reference identity is what makes "only the very object a reservation
        /// was made for can lift it" explicit.
        /// </para>
        /// </summary>
        public void RegisterApertureType(TBD.ApertureType apertureType, ApertureTypeDefinition apertureTypeDefinition, int ordinal)
        {
            if (apertureType == null)
            {
                return;
            }

            ApertureTypeEntry apertureTypeEntry = apertureTypes.Find(x => ReferenceEquals(x.ApertureType, apertureType));
            if (apertureTypeEntry != null)
            {
                apertureTypeEntry.Definition = apertureTypeDefinition;
                apertureTypeEntry.Ordinal = ordinal < 1 ? 1 : ordinal;
                apertureTypeEntry.Refusal = null;
                return;
            }

            apertureTypes.Add(new ApertureTypeEntry
            {
                ApertureType = apertureType,
                Name = apertureType.name,
                Definition = apertureTypeDefinition,
                Ordinal = ordinal < 1 ? 1 : ordinal,
                Refusal = null
            });
        }

        /// <summary>
        /// Reserves a created aperture type's name WITHOUT making it reusable. Called the moment a created
        /// type is named: if the subsequent write or read-back verification fails, the type remains in the
        /// TBD but must never be handed to the next opening as a reusable definition - while its name still
        /// occupies the namespace, so the next creation cannot accidentally choose it again. A null
        /// definition is what <see cref="FindApertureType(ApertureTypeDefinition, int)"/> never matches.
        /// </summary>
        public void ReserveApertureType(TBD.ApertureType apertureType, int ordinal, string refusal = null)
        {
            if (apertureType == null)
            {
                return;
            }

            if (apertureTypes.Exists(x => ReferenceEquals(x.ApertureType, apertureType)))
            {
                return;
            }

            apertureTypes.Add(new ApertureTypeEntry
            {
                ApertureType = apertureType,
                Name = apertureType.name,
                Definition = null,
                Ordinal = ordinal < 1 ? 1 : ordinal,
                Refusal = refusal ?? "Aperture type created by this export whose write did not complete, so it is not reusable."
            });
        }

        /// <summary>
        /// Every construction name in the building, including this export's own creations and including
        /// constructions that may not be reused - a name occupies the namespace whatever its content, so a
        /// new construction must not be given it.
        /// </summary>
        public List<string> ConstructionNames()
        {
            return constructions.Select(x => x.Name).ToList();
        }

        /// <summary>
        /// The reusable aperture construction holding exactly <paramref name="constructionDefinition"/>, or
        /// null.
        /// <para>
        /// Matching consults REUSABLE entries only, and identity is the definition: the type, the ordered
        /// layers with their materials and widths, the additional heat transfer, the description and which
        /// half of a window it is. The NAME takes no part - which is the whole of the fix here. The previous
        /// behaviour on this path adopted any construction whose name matched, whatever its layers; once
        /// names are derived from the reusable SAM <c>ApertureConstruction</c> rather than from the
        /// aperture's GUID, that would hand one window another window's glazing.
        /// </para>
        /// </summary>
        public TBD.Construction FindConstruction(ConstructionDefinition constructionDefinition)
        {
            if (constructionDefinition == null || !constructionDefinition.Proven)
            {
                return null;
            }

            EnsureSeedClassification();

            foreach (ConstructionEntry constructionEntry in constructions)
            {
                if (constructionEntry.Definition == null)
                {
                    continue;
                }

                if (constructionEntry.Definition.Equals(constructionDefinition))
                {
                    return constructionEntry.Construction;
                }
            }

            return null;
        }

        /// <summary>Why a seeded aperture construction may not be reused, or null.</summary>
        public string ConstructionRefusal(string name)
        {
            EnsureSeedClassification();

            return name == null ? null : constructions.Find(x => x.Name == name)?.Refusal;
        }

        /// <summary>
        /// Remembers an aperture construction this export created and finished writing, so the next window
        /// reuses it. A construction reserved at creation time is upgraded in place, identified by the SAME
        /// COM reference rather than by name.
        /// </summary>
        public void RegisterConstruction(TBD.Construction construction, ConstructionDefinition constructionDefinition)
        {
            if (construction == null)
            {
                return;
            }

            ConstructionEntry constructionEntry = constructions.Find(x => ReferenceEquals(x.Construction, construction));
            if (constructionEntry != null)
            {
                constructionEntry.Definition = constructionDefinition;
                constructionEntry.Refusal = null;
                return;
            }

            constructions.Add(new ConstructionEntry
            {
                Construction = construction,
                Name = construction.name,
                Definition = constructionDefinition,
                Refusal = null
            });
        }

        /// <summary>
        /// Reserves a created construction's name WITHOUT making it reusable - called the moment a created
        /// construction is named, so that a write which does not complete leaves an object that can never be
        /// handed to the next window, while its name still occupies the namespace.
        /// </summary>
        public void ReserveConstruction(TBD.Construction construction, string refusal = null)
        {
            if (construction == null)
            {
                return;
            }

            if (constructions.Exists(x => ReferenceEquals(x.Construction, construction)))
            {
                return;
            }

            constructions.Add(new ConstructionEntry
            {
                Construction = construction,
                Name = construction.name,
                Definition = null,
                Refusal = refusal ?? "Construction created by this export whose write did not complete, so it is not reusable."
            });
        }

        /// <summary>
        /// Every building element name in the building, including this export's own creations and including
        /// elements that may not be shared.
        /// </summary>
        public List<string> BuildingElementNames()
        {
            return buildingElements.Select(x => x.Name).ToList();
        }

        /// <summary>
        /// The reusable aperture building element matching exactly
        /// <paramref name="buildingElementDefinition"/>, or null.
        /// <para>
        /// Matching consults REUSABLE entries only, and identity is the definition - window or door, pane or
        /// frame, <c>BEType</c>, colour, construction content and the ordered openings with their
        /// occurrences. On a hit the caller assigns the element to this aperture's <c>zoneSurface</c>es and
        /// writes NOTHING to the element itself: every other aperture referencing it would see the write.
        /// </para>
        /// </summary>
        public TBD.buildingElement FindApertureBuildingElement(BuildingElementDefinition buildingElementDefinition)
        {
            if (buildingElementDefinition == null || !buildingElementDefinition.Proven)
            {
                return null;
            }

            EnsureSeedClassification();

            foreach (BuildingElementEntry buildingElementEntry in buildingElements)
            {
                if (buildingElementEntry.Definition == null)
                {
                    continue;
                }

                if (buildingElementEntry.Definition.Equals(buildingElementDefinition))
                {
                    return buildingElementEntry.BuildingElement;
                }
            }

            return null;
        }

        /// <summary>Why a seeded aperture building element may not be shared, or null.</summary>
        public string BuildingElementRefusal(string name)
        {
            EnsureSeedClassification();

            return name == null ? null : buildingElements.Find(x => x.Name == name)?.Refusal;
        }

        /// <summary>
        /// Remembers an aperture building element this export created and finished writing - construction
        /// assigned, colour and <c>BEType</c> set, and its openings written - so the next equivalent aperture
        /// shares it. An element reserved at creation time is upgraded in place, identified by the SAME COM
        /// reference rather than by name.
        /// </summary>
        public void RegisterApertureBuildingElement(TBD.buildingElement buildingElement, BuildingElementDefinition buildingElementDefinition)
        {
            if (buildingElement == null)
            {
                return;
            }

            BuildingElementEntry buildingElementEntry = buildingElements.Find(x => ReferenceEquals(x.BuildingElement, buildingElement));
            if (buildingElementEntry != null)
            {
                buildingElementEntry.Definition = buildingElementDefinition;
                buildingElementEntry.Refusal = null;
                return;
            }

            buildingElements.Add(new BuildingElementEntry
            {
                BuildingElement = buildingElement,
                Name = buildingElement.name,
                Definition = buildingElementDefinition,
                Refusal = null
            });
        }

        /// <summary>
        /// Reserves a created aperture building element's name WITHOUT making it shareable - called the
        /// moment a created element is named. An element whose openings were only partly written must never
        /// be handed to the next aperture as though it carried them all, but its name still occupies the
        /// namespace.
        /// </summary>
        public void ReserveApertureBuildingElement(TBD.buildingElement buildingElement, string refusal = null)
        {
            if (buildingElement == null)
            {
                return;
            }

            if (buildingElements.Exists(x => ReferenceEquals(x.BuildingElement, buildingElement)))
            {
                return;
            }

            buildingElements.Add(new BuildingElementEntry
            {
                BuildingElement = buildingElement,
                Name = buildingElement.name,
                Definition = null,
                Refusal = refusal ?? "Building element created by this export whose write did not complete, so it is not shareable."
            });
        }

        /// <summary>
        /// The aperture types <paramref name="buildingElement"/> carried BEFORE this export touched it,
        /// each as its name paired with its definition (null where it may not be reused), in element order.
        /// Read once per element and remembered.
        /// </summary>
        public List<KeyValuePair<string, ApertureTypeDefinition>> ExistingAssignments(TBD.buildingElement buildingElement)
        {
            return new List<KeyValuePair<string, ApertureTypeDefinition>>(EnsureAssignments(buildingElement));
        }

        /// <summary>
        /// The same pre-existing assignments as <see cref="ExistingAssignments(TBD.buildingElement)"/>, in
        /// the same order, as the COM objects themselves - so an index into one indexes the other.
        /// </summary>
        public List<TBD.ApertureType> ExistingApertureTypes(TBD.buildingElement buildingElement)
        {
            EnsureAssignments(buildingElement);

            if (buildingElement != null && assignments_ExistingApertureTypes.TryGetValue(Key(buildingElement), out List<TBD.ApertureType> result))
            {
                return new List<TBD.ApertureType>(result);
            }

            return new List<TBD.ApertureType>();
        }

        /// <summary>
        /// Whether <paramref name="buildingElement"/> carries an aperture type of that name - the cached
        /// equivalent of the assigned-guard's <c>buildingElement.ApertureTypes()</c> scan. Assigning a type
        /// an element already carries adds a SECOND entry in TAS, which would give the element more
        /// openings than the model states, so this guard is load-bearing.
        /// </summary>
        public bool IsAssigned(TBD.buildingElement buildingElement, string name)
        {
            if (buildingElement == null || name == null)
            {
                return false;
            }

            EnsureAssignments(buildingElement);

            return assignments_Current.TryGetValue(Key(buildingElement), out HashSet<string> names) && names.Contains(name);
        }

        /// <summary>Records that this export assigned an aperture type to an element.</summary>
        public void RegisterAssignment(TBD.buildingElement buildingElement, TBD.ApertureType apertureType)
        {
            if (buildingElement == null || apertureType == null)
            {
                return;
            }

            EnsureAssignments(buildingElement);

            string key = Key(buildingElement);
            if (!assignments_Current.TryGetValue(key, out HashSet<string> names))
            {
                names = new HashSet<string>();
                assignments_Current[key] = names;
            }

            names.Add(apertureType.name);
        }

        private List<KeyValuePair<string, ApertureTypeDefinition>> EnsureAssignments(TBD.buildingElement buildingElement)
        {
            if (buildingElement == null)
            {
                return new List<KeyValuePair<string, ApertureTypeDefinition>>();
            }

            string key = Key(buildingElement);
            if (assignments_Existing.TryGetValue(key, out List<KeyValuePair<string, ApertureTypeDefinition>> result))
            {
                return result;
            }

            result = new List<KeyValuePair<string, ApertureTypeDefinition>>();
            List<TBD.ApertureType> result_ApertureTypes = new List<TBD.ApertureType>();
            HashSet<string> names = new HashSet<string>();

            List<TBD.ApertureType> apertureTypes_Assigned = buildingElement.ApertureTypes();
            if (apertureTypes_Assigned != null)
            {
                foreach (TBD.ApertureType apertureType in apertureTypes_Assigned)
                {
                    if (apertureType == null)
                    {
                        continue;
                    }

                    string name = apertureType.name;
                    names.Add(name);

                    //A seeded type is classified once, in the constructor. Falling back to reading it here
                    //covers a type that was not in the building's own list.
                    ApertureTypeEntry apertureTypeEntry = apertureTypes.Find(x => x.Name == name);
                    ApertureTypeDefinition apertureTypeDefinition = apertureTypeEntry != null
                        ? apertureTypeEntry.Definition
                        : apertureType.ApertureTypeDefinition(out string _);

                    result.Add(new KeyValuePair<string, ApertureTypeDefinition>(name, apertureTypeDefinition));
                    result_ApertureTypes.Add(apertureType);
                }
            }

            assignments_Existing[key] = result;
            assignments_ExistingApertureTypes[key] = result_ApertureTypes;
            assignments_Current[key] = names;

            return result;
        }

        private static string Key(TBD.buildingElement buildingElement)
        {
            string guid = buildingElement.GUID;

            return string.IsNullOrWhiteSpace(guid) ? string.Format("name:{0}", buildingElement.name) : guid;
        }
    }
}
