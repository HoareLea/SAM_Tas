// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// The one object the explicit Part O ventilation conversion carries: what the PR1 graph
    /// <b>intends</b>, what the conversion <b>produced</b>, and whether the two agree.
    /// <para>
    /// <b>Why intent and result live together.</b> The reconciliation the route turns on is not "did the
    /// conversion finish" but "is what TAS now holds the graph PR1 designed". That comparison needs both
    /// sides at once, and a caller that could read one without the other could publish a payload nothing
    /// had checked. Here <see cref="Reconcile"/> is the only way to reach
    /// <see cref="IsReconciled"/>, and it refuses on the first thing that does not add up.
    /// </para>
    /// <para>
    /// <b>Everything is indexed by identity.</b> Rooms by <c>SystemSpace.Guid</c>, legs by
    /// <c>SystemConnection.Guid</c> and by duty-carrier guid, native references by source guid. Nothing
    /// is found by scanning and nothing is found by name, so a conversion of five thousand rooms does
    /// the same work per room as a conversion of five. <see cref="LookupCount"/> counts every dictionary
    /// probe so a scaling test can assert that rather than time it.
    /// </para>
    /// <para>
    /// <b>Order independence.</b> Every collection this object hands back is sorted by identity, never
    /// by insertion order, so the same graph converted from a differently enumerated source produces the
    /// same answer object.
    /// </para>
    /// <para>Free of TAS COM types: a context can be built, driven and reconciled without a TAS licence.</para>
    /// </summary>
    public class SystemVentilationConversionContext
    {
        /// <summary>
        /// How close a native flow read back has to be to the duty PR1 stated. A
        /// <c>SizedFlowVariable.Value</c> is stored single precision - an authored 128.0 read back as
        /// 128.00001525878906 - so the comparison is relative, with an absolute floor for zero.
        /// </summary>
        public const double FlowRateTolerance_Relative = 1e-5;

        /// <summary>The absolute floor of <see cref="FlowRateTolerance_Relative"/>, in l/s.</summary>
        public const double FlowRateTolerance_Absolute = 1e-6;

        private readonly Dictionary<Guid, SystemVentilationRoomIntent> roomIntents = new Dictionary<Guid, SystemVentilationRoomIntent>();
        private readonly Dictionary<Guid, SystemVentilationLegIntent> legIntents = new Dictionary<Guid, SystemVentilationLegIntent>();
        private readonly Dictionary<Guid, Guid> connection_By_DutyCarrier = new Dictionary<Guid, Guid>();
        private readonly Dictionary<Guid, Guid> airSystem_By_AirHandlingUnit = new Dictionary<Guid, Guid>();

        private readonly Dictionary<Guid, string> reference_By_Source = new Dictionary<Guid, string>();
        private readonly Dictionary<Guid, string> reference_System_By_AirSystem = new Dictionary<Guid, string>();

        private readonly Dictionary<Guid, RoomPairing> roomPairings = new Dictionary<Guid, RoomPairing>();
        private readonly Dictionary<Guid, SystemVentilationBinding> bindings = new Dictionary<Guid, SystemVentilationBinding>();
        private readonly Dictionary<Guid, SystemVentilationConnectionBinding> connectionBindings = new Dictionary<Guid, SystemVentilationConnectionBinding>();

        private readonly List<string> refusals = new List<string>();
        private readonly List<string> notes = new List<string>();

        /// <summary>
        /// Built on first use by <see cref="LegIntents_AirSystem"/>, and dropped whenever a leg is added
        /// or its duty carrier is set, so it can never answer from a graph that has since changed.
        /// </summary>
        private Dictionary<Guid, List<SystemVentilationLegIntent>> legIntents_By_AirSystem;

        private int lookupCount;
        private int count_NativeSystems;
        private bool reconciled;
        private bool reconcileAttempted;

        /// <summary>Creates an empty context. <c>Create.SystemVentilationConversionContext</c> fills it.</summary>
        public SystemVentilationConversionContext()
        {
        }

        // ------------------------------------------------------- PR5B: recirculation cooling branches

        private readonly Dictionary<Guid, Analytical.Systems.MechanicalVentilationRecirculationCooling> recirculationCooling_By_AirSystem = new Dictionary<Guid, Analytical.Systems.MechanicalVentilationRecirculationCooling>();
        private readonly HashSet<Guid> recirculationConnections = new HashSet<Guid>();
        private readonly HashSet<Guid> recirculationComponents = new HashSet<Guid>();

        /// <summary>
        /// PR5B (SAM#111): one air system's internal recirculation cooling branch, as SAM_Systems
        /// materialised it. Its connections are never ventilation legs and its dampers never carry a
        /// ventilation duty - both are told apart from the ventilation by these identities alone.
        /// </summary>
        public bool Add(Analytical.Systems.MechanicalVentilationRecirculationCooling mechanicalVentilationRecirculationCooling)
        {
            if (mechanicalVentilationRecirculationCooling == null)
            {
                return false;
            }

            if (recirculationCooling_By_AirSystem.ContainsKey(mechanicalVentilationRecirculationCooling.Guid_AirSystem))
            {
                Refuse(string.Format(
                    "Air system {0} is stated to carry two recirculation cooling branches.",
                    mechanicalVentilationRecirculationCooling.Guid_AirSystem));

                return false;
            }

            recirculationCooling_By_AirSystem[mechanicalVentilationRecirculationCooling.Guid_AirSystem] = mechanicalVentilationRecirculationCooling;

            foreach (Guid guid in mechanicalVentilationRecirculationCooling.Guids_Connection)
            {
                recirculationConnections.Add(guid);
            }

            foreach (Guid guid in mechanicalVentilationRecirculationCooling.Guids_Component)
            {
                recirculationComponents.Add(guid);
            }

            return true;
        }

        private readonly Dictionary<Guid, Analytical.Systems.MechanicalVentilationGuidanceCooling> guidanceCooling_By_AirSystem = new Dictionary<Guid, Analytical.Systems.MechanicalVentilationGuidanceCooling>();

        /// <summary>
        /// SAM#123: one air system's manufacturer-guidance cooling unit, as SAM_Systems materialised it -
        /// the exchanger, the supply DX coil after it, both fans, the stat room and the rooms' design and
        /// elevated airflows - grounded natively by <see cref="Modify.GroundGuidanceCooling"/>.
        /// </summary>
        public bool Add(Analytical.Systems.MechanicalVentilationGuidanceCooling mechanicalVentilationGuidanceCooling)
        {
            if (mechanicalVentilationGuidanceCooling == null)
            {
                return false;
            }

            if (guidanceCooling_By_AirSystem.ContainsKey(mechanicalVentilationGuidanceCooling.Guid_AirSystem) || recirculationCooling_By_AirSystem.ContainsKey(mechanicalVentilationGuidanceCooling.Guid_AirSystem))
            {
                Refuse(string.Format(
                    "Air system {0} is stated to carry a manufacturer-guidance cooling unit alongside another cooling unit.",
                    mechanicalVentilationGuidanceCooling.Guid_AirSystem));

                return false;
            }

            guidanceCooling_By_AirSystem[mechanicalVentilationGuidanceCooling.Guid_AirSystem] = mechanicalVentilationGuidanceCooling;

            return true;
        }

        /// <summary>Every manufacturer-guidance cooling unit, ordered by air system guid.</summary>
        public List<Analytical.Systems.MechanicalVentilationGuidanceCooling> GuidanceCoolings
        {
            get
            {
                List<Guid> guids = new List<Guid>(guidanceCooling_By_AirSystem.Keys);
                guids.Sort();

                return guids.ConvertAll(x => guidanceCooling_By_AirSystem[x]);
            }
        }

        /// <summary>The manufacturer-guidance cooling unit of one air system, or null where it has none.</summary>
        public Analytical.Systems.MechanicalVentilationGuidanceCooling GuidanceCooling(Guid guid_AirSystem)
        {
            lookupCount++;

            return guidanceCooling_By_AirSystem.TryGetValue(guid_AirSystem, out Analytical.Systems.MechanicalVentilationGuidanceCooling result) ? result : null;
        }

        /// <summary>Every recirculation cooling branch, ordered by air system guid.</summary>
        public List<Analytical.Systems.MechanicalVentilationRecirculationCooling> RecirculationCoolings
        {
            get
            {
                List<Guid> guids = new List<Guid>(recirculationCooling_By_AirSystem.Keys);
                guids.Sort();

                List<Analytical.Systems.MechanicalVentilationRecirculationCooling> result = new List<Analytical.Systems.MechanicalVentilationRecirculationCooling>();
                foreach (Guid guid in guids)
                {
                    result.Add(recirculationCooling_By_AirSystem[guid]);
                }

                return result;
            }
        }

        /// <summary>The recirculation cooling branch of one air system, or null where it has none.</summary>
        public Analytical.Systems.MechanicalVentilationRecirculationCooling RecirculationCooling(Guid guid_AirSystem)
        {
            lookupCount++;

            return recirculationCooling_By_AirSystem.TryGetValue(guid_AirSystem, out Analytical.Systems.MechanicalVentilationRecirculationCooling result) ? result : null;
        }

        /// <summary>Whether a connection belongs to a recirculation cooling branch - and is therefore not a ventilation leg.</summary>
        public bool IsRecirculationConnection(Guid guid_SystemConnection)
        {
            lookupCount++;

            return recirculationConnections.Contains(guid_SystemConnection);
        }

        /// <summary>Whether a component belongs to a recirculation cooling branch.</summary>
        public bool IsRecirculationComponent(Guid guid_SystemComponent)
        {
            lookupCount++;

            return recirculationComponents.Contains(guid_SystemComponent);
        }

        /// <summary>
        /// What <see cref="Modify.GroundVentilationFans"/> does with a fan's native <c>HeatGainFactor</c> -
        /// PR5A (SAM#111 plan §D/§K.3). <c>ClearToZero</c> (the B0 control) by default; set from
        /// <c>Create.SystemVentilationConversionContext</c>'s own parameter, which
        /// <c>Create.SystemVentilationRoute</c> exposes to its caller.
        /// </summary>
        public SystemVentilationFanHeatGainPolicy FanHeatGainPolicy { get; set; } = SystemVentilationFanHeatGainPolicy.ClearToZero;

        /// <summary>
        /// Optional coarse progress sink, set by <c>Create.SystemVentilationRoute</c> from its caller. Null
        /// (the default) reports nothing and costs one null check per air system.
        /// </summary>
        internal Action<SystemVentilationRouteProgress> Progress { get; set; }

        /// <summary>Reports one progress event. A throwing subscriber can never stop the conversion.</summary>
        internal void ReportProgress(SystemVentilationRouteStage stage, int current, int total)
        {
            ReportProgress(Progress, stage, current, total);
        }

        internal static void ReportProgress(Action<SystemVentilationRouteProgress> progress, SystemVentilationRouteStage stage, int current, int total)
        {
            if (progress == null)
            {
                return;
            }

            try
            {
                progress(new SystemVentilationRouteProgress(stage, current, total));
            }
            catch (Exception)
            {
                //Progress is advisory.
            }
        }

        // ------------------------------------------------------------------------------- the intent

        /// <summary>Every intended room, ordered by <c>SystemSpace.Guid</c>.</summary>
        public List<SystemVentilationRoomIntent> RoomIntents
        {
            get
            {
                List<SystemVentilationRoomIntent> result = new List<SystemVentilationRoomIntent>(roomIntents.Values);
                result.Sort((x, y) => x.Guid_SystemSpace.CompareTo(y.Guid_SystemSpace));
                return result;
            }
        }

        /// <summary>Every intended leg, ordered by type then <c>SystemConnection.Guid</c>.</summary>
        public List<SystemVentilationLegIntent> LegIntents
        {
            get
            {
                List<SystemVentilationLegIntent> result = new List<SystemVentilationLegIntent>(legIntents.Values);
                result.Sort(CompareLegIntents);
                return result;
            }
        }

        /// <summary>The intended <c>AirHandlingUnit.Guid</c> to <c>AirSystem.Guid</c> pairing, from PR1.</summary>
        public Dictionary<Guid, Guid> AirSystemByAirHandlingUnit
        {
            get { return new Dictionary<Guid, Guid>(airSystem_By_AirHandlingUnit); }
        }

        /// <summary>Adds one intended air handling unit to air system pairing.</summary>
        public bool Add(Guid guid_AirHandlingUnit, Guid guid_AirSystem)
        {
            if (guid_AirHandlingUnit == Guid.Empty || guid_AirSystem == Guid.Empty)
            {
                return false;
            }

            if (airSystem_By_AirHandlingUnit.TryGetValue(guid_AirHandlingUnit, out Guid guid_Existing) && guid_Existing != guid_AirSystem)
            {
                Refuse(string.Format(
                    "Air handling unit {0} is stated to have materialised as two different air systems, {1} and {2}.",
                    guid_AirHandlingUnit,
                    guid_Existing,
                    guid_AirSystem));

                return false;
            }

            airSystem_By_AirHandlingUnit[guid_AirHandlingUnit] = guid_AirSystem;
            return true;
        }

        /// <summary>Adds one intended room.</summary>
        public bool Add(SystemVentilationRoomIntent systemVentilationRoomIntent)
        {
            if (systemVentilationRoomIntent == null || systemVentilationRoomIntent.Guid_SystemSpace == Guid.Empty)
            {
                return false;
            }

            if (roomIntents.ContainsKey(systemVentilationRoomIntent.Guid_SystemSpace))
            {
                Refuse(string.Format(
                    "The source graph states system space {0} twice.",
                    systemVentilationRoomIntent.Guid_SystemSpace));

                return false;
            }

            roomIntents[systemVentilationRoomIntent.Guid_SystemSpace] = systemVentilationRoomIntent;
            return true;
        }

        /// <summary>Adds one intended leg.</summary>
        public bool Add(SystemVentilationLegIntent systemVentilationLegIntent)
        {
            if (systemVentilationLegIntent == null || systemVentilationLegIntent.Guid_SystemConnection == Guid.Empty)
            {
                return false;
            }

            if (legIntents.ContainsKey(systemVentilationLegIntent.Guid_SystemConnection))
            {
                Refuse(string.Format(
                    "The source graph states connection {0} twice.",
                    systemVentilationLegIntent.Guid_SystemConnection));

                return false;
            }

            legIntents[systemVentilationLegIntent.Guid_SystemConnection] = systemVentilationLegIntent;
            legIntents_By_AirSystem = null;

            if (systemVentilationLegIntent.Guid_DutyCarrier != Guid.Empty)
            {
                connection_By_DutyCarrier[systemVentilationLegIntent.Guid_DutyCarrier] = systemVentilationLegIntent.Guid_SystemConnection;
            }

            return true;
        }

        /// <summary>
        /// Replaces one leg intent with the same intent naming the duty carrier now materialised for it.
        /// </summary>
        public bool SetDutyCarrier(Guid guid_SystemConnection, Guid guid_DutyCarrier)
        {
            lookupCount++;

            if (!legIntents.TryGetValue(guid_SystemConnection, out SystemVentilationLegIntent systemVentilationLegIntent) || systemVentilationLegIntent == null)
            {
                return false;
            }

            if (guid_DutyCarrier == Guid.Empty)
            {
                return false;
            }

            if (connection_By_DutyCarrier.TryGetValue(guid_DutyCarrier, out Guid guid_Existing) && guid_Existing != guid_SystemConnection)
            {
                Refuse(string.Format(
                    "Duty carrier {0} is claimed by connections {1} and {2}; a leg's duty cannot be shared.",
                    guid_DutyCarrier,
                    guid_Existing,
                    guid_SystemConnection));

                return false;
            }

            legIntents[guid_SystemConnection] = systemVentilationLegIntent.WithDutyCarrier(guid_DutyCarrier);
            connection_By_DutyCarrier[guid_DutyCarrier] = guid_SystemConnection;
            legIntents_By_AirSystem = null;

            return true;
        }

        /// <summary>
        /// The intended legs of one air system, ordered by type then connection guid.
        /// <para>
        /// Indexed rather than filtered. The conversion asks this once per air system, and a filter
        /// over the whole leg collection would make that <c>systems x legs</c> - a quarter of a
        /// million passes over eight thousand legs on a five thousand room scheme, for an answer a
        /// dictionary gives directly.
        /// </para>
        /// </summary>
        public List<SystemVentilationLegIntent> LegIntents_AirSystem(Guid guid_AirSystem)
        {
            lookupCount++;

            if (legIntents_By_AirSystem == null)
            {
                legIntents_By_AirSystem = new Dictionary<Guid, List<SystemVentilationLegIntent>>();

                foreach (SystemVentilationLegIntent systemVentilationLegIntent in LegIntents)
                {
                    if (!legIntents_By_AirSystem.TryGetValue(systemVentilationLegIntent.Guid_AirSystem, out List<SystemVentilationLegIntent> systemVentilationLegIntents))
                    {
                        systemVentilationLegIntents = new List<SystemVentilationLegIntent>();
                        legIntents_By_AirSystem[systemVentilationLegIntent.Guid_AirSystem] = systemVentilationLegIntents;
                    }

                    systemVentilationLegIntents.Add(systemVentilationLegIntent);
                }
            }

            return legIntents_By_AirSystem.TryGetValue(guid_AirSystem, out List<SystemVentilationLegIntent> result)
                ? new List<SystemVentilationLegIntent>(result)
                : new List<SystemVentilationLegIntent>();
        }

        /// <summary>The intended room for a materialised <c>SystemSpace</c>, or null.</summary>
        public SystemVentilationRoomIntent RoomIntent(Guid guid_SystemSpace)
        {
            lookupCount++;

            return roomIntents.TryGetValue(guid_SystemSpace, out SystemVentilationRoomIntent result) ? result : null;
        }

        /// <summary>The intended leg for a PR1 connection, or null.</summary>
        public SystemVentilationLegIntent LegIntent(Guid guid_SystemConnection)
        {
            lookupCount++;

            return legIntents.TryGetValue(guid_SystemConnection, out SystemVentilationLegIntent result) ? result : null;
        }

        /// <summary>The intended leg a materialised duty carrier belongs to, or null.</summary>
        public SystemVentilationLegIntent LegIntentByDutyCarrier(Guid guid_DutyCarrier)
        {
            lookupCount++;

            if (!connection_By_DutyCarrier.TryGetValue(guid_DutyCarrier, out Guid guid_SystemConnection))
            {
                return null;
            }

            return LegIntent(guid_SystemConnection);
        }

        // ------------------------------------------------------------------- what the conversion did

        /// <summary>
        /// Records that one source object was materialised as one native object. Called at the pairing
        /// point, where both sides are in hand - never reconstructed afterwards.
        /// </summary>
        public bool RecordPairing(Guid guid_Source, string reference_Native)
        {
            if (guid_Source == Guid.Empty || string.IsNullOrWhiteSpace(reference_Native))
            {
                return false;
            }

            reference_By_Source[guid_Source] = reference_Native;
            return true;
        }

        /// <summary>The native reference a source object was materialised as, or null.</summary>
        public string Reference(Guid guid_Source)
        {
            lookupCount++;

            return reference_By_Source.TryGetValue(guid_Source, out string result) ? result : null;
        }

        /// <summary>Records the native <c>System</c> one intended air system became.</summary>
        public bool RecordAirSystem(Guid guid_AirSystem, string reference_System)
        {
            if (guid_AirSystem == Guid.Empty || string.IsNullOrWhiteSpace(reference_System))
            {
                return false;
            }

            reference_System_By_AirSystem[guid_AirSystem] = reference_System;
            return true;
        }

        /// <summary>
        /// Records that one native air system was created in the document, whether or not the source
        /// graph intended it. The count is what catches an extra TAS system nothing asked for.
        /// </summary>
        public void RecordNativeSystem()
        {
            count_NativeSystems++;
        }

        /// <summary>How many native air systems the conversion created.</summary>
        public int Count_NativeSystems
        {
            get { return count_NativeSystems; }
        }

        /// <summary>
        /// Records the native side of one room, <b>at the pairing point</b>, where the analytical room,
        /// the native zone and the zone load the conversion has just bound to it are all in hand. The
        /// finished <see cref="SystemVentilationBinding"/> is assembled later by
        /// <see cref="CompleteRoomBindings"/>, once the room's extract leg has reported what its native
        /// damper actually holds - nothing about the room is reconstructed by searching afterwards.
        /// </summary>
        /// <param name="designFlowRate_Supply_Lps">
        /// What the native zone holds after the write, read back off it - not what was asked for.
        /// </param>
        public bool RecordRoomPairing(
            Guid guid_SystemSpace,
            string reference_SystemZone,
            string reference_ZoneLoad,
            string reference_System,
            double? designFlowRate_Supply_Lps)
        {
            if (guid_SystemSpace == Guid.Empty)
            {
                return false;
            }

            if (roomPairings.ContainsKey(guid_SystemSpace))
            {
                Refuse(string.Format(
                    "System space {0} was paired with a native zone twice, so two native zones claim one room.",
                    guid_SystemSpace));

                return false;
            }

            roomPairings[guid_SystemSpace] = new RoomPairing(
                reference_SystemZone,
                reference_ZoneLoad,
                reference_System,
                designFlowRate_Supply_Lps);

            return true;
        }

        /// <summary>
        /// Assembles one <see cref="SystemVentilationBinding"/> per paired room, joining the room's own
        /// native identities to the extract duty its extract leg's damper reported. Runs once, after
        /// every leg has been bound.
        /// </summary>
        public void CompleteRoomBindings()
        {
            Dictionary<Guid, double> extract_By_SystemSpace = new Dictionary<Guid, double>();

            foreach (SystemVentilationConnectionBinding systemVentilationConnectionBinding in connectionBindings.Values)
            {
                if (systemVentilationConnectionBinding.ConnectionType != SystemVentilationConnectionType.Extract)
                {
                    continue;
                }

                extract_By_SystemSpace[systemVentilationConnectionBinding.Guid_SystemSpace_From] = systemVentilationConnectionBinding.DesignFlowRate_Lps;
            }

            List<Guid> guids = new List<Guid>(roomPairings.Keys);
            guids.Sort();

            foreach (Guid guid_SystemSpace in guids)
            {
                RoomPairing roomPairing = roomPairings[guid_SystemSpace];

                SystemVentilationRoomIntent systemVentilationRoomIntent = RoomIntent(guid_SystemSpace);

                Record(new SystemVentilationBinding(
                    systemVentilationRoomIntent == null ? Guid.Empty : systemVentilationRoomIntent.Guid_Space,
                    guid_SystemSpace,
                    systemVentilationRoomIntent == null ? Guid.Empty : systemVentilationRoomIntent.Guid_AirSystem,
                    roomPairing.Reference_SystemZone,
                    roomPairing.Reference_ZoneLoad,
                    roomPairing.Reference_System,
                    roomPairing.DesignFlowRate_Supply_Lps,
                    extract_By_SystemSpace.TryGetValue(guid_SystemSpace, out double extract_Lps) ? (double?)extract_Lps : null));
            }
        }

        /// <summary>Records a completed room binding. A second binding for one room is a refusal.</summary>
        public bool Record(SystemVentilationBinding systemVentilationBinding)
        {
            if (systemVentilationBinding == null)
            {
                return false;
            }

            if (bindings.ContainsKey(systemVentilationBinding.Guid_SystemSpace))
            {
                Refuse(string.Format(
                    "System space {0} was bound twice, so two native zones claim one room.",
                    systemVentilationBinding.Guid_SystemSpace));

                return false;
            }

            bindings[systemVentilationBinding.Guid_SystemSpace] = systemVentilationBinding;
            return true;
        }

        /// <summary>Records a completed leg binding. A second binding for one leg is a refusal.</summary>
        public bool Record(SystemVentilationConnectionBinding systemVentilationConnectionBinding)
        {
            if (systemVentilationConnectionBinding == null)
            {
                return false;
            }

            if (connectionBindings.ContainsKey(systemVentilationConnectionBinding.Guid_SystemConnection))
            {
                Refuse(string.Format(
                    "Connection {0} was bound twice, so one leg was materialised more than once.",
                    systemVentilationConnectionBinding.Guid_SystemConnection));

                return false;
            }

            connectionBindings[systemVentilationConnectionBinding.Guid_SystemConnection] = systemVentilationConnectionBinding;
            return true;
        }

        /// <summary>Every completed room binding, ordered by <c>Space.Guid</c>.</summary>
        public List<SystemVentilationBinding> Bindings
        {
            get
            {
                List<SystemVentilationBinding> result = new List<SystemVentilationBinding>(bindings.Values);
                result.Sort((x, y) => x.Guid_Space.CompareTo(y.Guid_Space));
                return result;
            }
        }

        /// <summary>Every completed leg binding, ordered by type then <c>SystemConnection.Guid</c>.</summary>
        public List<SystemVentilationConnectionBinding> ConnectionBindings
        {
            get
            {
                List<SystemVentilationConnectionBinding> result = new List<SystemVentilationConnectionBinding>(connectionBindings.Values);
                result.Sort(CompareConnectionBindings);
                return result;
            }
        }

        // -------------------------------------------------------------------------- refusals / notes

        /// <summary>Every reason this conversion is not the graph PR1 designed.</summary>
        public List<string> Refusals
        {
            get { return new List<string>(refusals); }
        }

        /// <summary>What was reconciled, for the audit trail.</summary>
        public List<string> Notes
        {
            get { return new List<string>(notes); }
        }

        /// <summary>Records a refusal. A context that has refused cannot later be talked into success.</summary>
        public void Refuse(string refusal)
        {
            if (!string.IsNullOrWhiteSpace(refusal))
            {
                refusals.Add(refusal);
                reconciled = false;
            }
        }

        /// <summary>Records a stage that passed.</summary>
        public void Note(string note)
        {
            if (!string.IsNullOrWhiteSpace(note))
            {
                notes.Add(note);
            }
        }

        /// <summary>How many indexed lookups the conversion has made, for the scaling evidence.</summary>
        public int LookupCount
        {
            get { return lookupCount; }
        }

        /// <summary>
        /// Whether <see cref="Reconcile"/> has been run and every check passed. False until it has been
        /// run: an unreconciled conversion is never a usable one.
        /// </summary>
        public bool IsReconciled
        {
            get { return reconciled && refusals.Count == 0; }
        }

        // ------------------------------------------------------------------------------ reconciliation

        /// <summary>
        /// Compares what the conversion produced against what the PR1 graph intended, and refuses on
        /// every disagreement rather than the first, so one run reports the whole story.
        /// <para>What is checked, in order:</para>
        /// <list type="number">
        /// <item><description>every intended air handling unit became exactly one air system, and no air
        /// system serves two units - no collapse;</description></item>
        /// <item><description>the document holds no air system the source graph did not
        /// ask for;</description></item>
        /// <item><description>every intended room was bound exactly once, and no room was
        /// lost;</description></item>
        /// <item><description>each room's binding names the air system its intent named - a room in the
        /// wrong unit is a refusal, not a note;</description></item>
        /// <item><description>every native zone and zone-load reference is present and
        /// distinct;</description></item>
        /// <item><description>every intended leg was bound exactly once, per type;</description></item>
        /// <item><description>every flow read back off the native carrier equals the duty PR1 stated,
        /// within single-precision tolerance.</description></item>
        /// </list>
        /// </summary>
        /// <returns>True only when nothing disagreed.</returns>
        public bool Reconcile()
        {
            reconcileAttempted = true;
            reconciled = false;

            ReconcileAirSystems();
            ReconcileRooms();
            ReconcileLegs();

            reconciled = refusals.Count == 0;

            if (reconciled)
            {
                Note(string.Format(
                    "Conversion reconciled against the source graph: {0} air system(s), {1} room(s), "
                    + "{2} supply / {3} extract / {4} transfer leg(s), every design flow matched on its "
                    + "native carrier.",
                    airSystem_By_AirHandlingUnit.Count,
                    roomIntents.Count,
                    Count(SystemVentilationConnectionType.Supply),
                    Count(SystemVentilationConnectionType.Extract),
                    Count(SystemVentilationConnectionType.Transfer)));
            }

            return reconciled;
        }

        /// <summary>Whether <see cref="Reconcile"/> has been run at all.</summary>
        public bool ReconcileAttempted
        {
            get { return reconcileAttempted; }
        }

        /// <summary>How many intended legs of one type the source graph states.</summary>
        public int Count(SystemVentilationConnectionType systemVentilationConnectionType)
        {
            int result = 0;
            foreach (SystemVentilationLegIntent systemVentilationLegIntent in legIntents.Values)
            {
                if (systemVentilationLegIntent.ConnectionType == systemVentilationConnectionType)
                {
                    result++;
                }
            }

            return result;
        }

        private void ReconcileAirSystems()
        {
            if (airSystem_By_AirHandlingUnit.Count == 0)
            {
                Refuse("The source graph states no air handling unit, so there is nothing to convert.");
                return;
            }

            Dictionary<string, Guid> airSystem_By_Reference = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<Guid, Guid> keyValuePair in airSystem_By_AirHandlingUnit)
            {
                if (!reference_System_By_AirSystem.TryGetValue(keyValuePair.Value, out string reference_System) || string.IsNullOrWhiteSpace(reference_System))
                {
                    Refuse(string.Format(
                        "Air handling unit {0} was to become air system {1}, and no native TAS system was produced for it.",
                        keyValuePair.Key,
                        keyValuePair.Value));

                    continue;
                }

                if (airSystem_By_Reference.TryGetValue(reference_System, out Guid guid_Other) && guid_Other != keyValuePair.Value)
                {
                    Refuse(string.Format(
                        "Air systems {0} and {1} both became native TAS system {2}, so two air handling units "
                        + "collapsed into one.",
                        guid_Other,
                        keyValuePair.Value,
                        reference_System));

                    continue;
                }

                airSystem_By_Reference[reference_System] = keyValuePair.Value;
            }

            if (count_NativeSystems != airSystem_By_AirHandlingUnit.Count)
            {
                Refuse(string.Format(
                    "The source graph states {0} air handling unit(s) and the document holds {1} native TAS "
                    + "air system(s).",
                    airSystem_By_AirHandlingUnit.Count,
                    count_NativeSystems));
            }
        }

        private void ReconcileRooms()
        {
            Dictionary<string, Guid> space_By_ZoneReference = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Guid> space_By_LoadReference = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (SystemVentilationRoomIntent systemVentilationRoomIntent in RoomIntents)
            {
                if (!systemVentilationRoomIntent.IsComplete)
                {
                    Refuse(string.Format(
                        "Room {0} (system space {1}) does not state the TAS zone guid its analytical space was "
                        + "stamped with, so it cannot be bound by identity.",
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationRoomIntent.Guid_SystemSpace));

                    continue;
                }

                if (!bindings.TryGetValue(systemVentilationRoomIntent.Guid_SystemSpace, out SystemVentilationBinding systemVentilationBinding) || systemVentilationBinding == null)
                {
                    Refuse(string.Format(
                        "Room {0} (system space {1}) is in the source graph and no native zone was bound for it.",
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationRoomIntent.Guid_SystemSpace));

                    continue;
                }

                if (!systemVentilationBinding.IsComplete)
                {
                    Refuse(string.Format(
                        "Room {0} produced an incomplete binding: {1}.",
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationBinding));

                    continue;
                }

                if (systemVentilationBinding.Guid_Space != systemVentilationRoomIntent.Guid_Space)
                {
                    Refuse(string.Format(
                        "System space {0} was bound to analytical room {1}, and the source graph states room {2}.",
                        systemVentilationRoomIntent.Guid_SystemSpace,
                        systemVentilationBinding.Guid_Space,
                        systemVentilationRoomIntent.Guid_Space));
                }

                if (systemVentilationBinding.Guid_AirSystem != systemVentilationRoomIntent.Guid_AirSystem)
                {
                    Refuse(string.Format(
                        "Room {0} was bound into air system {1}, and the source graph puts it in {2}.",
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationBinding.Guid_AirSystem,
                        systemVentilationRoomIntent.Guid_AirSystem));
                }

                if (reference_System_By_AirSystem.TryGetValue(systemVentilationRoomIntent.Guid_AirSystem, out string reference_System)
                    && !string.Equals(reference_System, systemVentilationBinding.Reference_System, StringComparison.OrdinalIgnoreCase))
                {
                    Refuse(string.Format(
                        "Room {0} was bound into native TAS system {1}, and its air system became {2}.",
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationBinding.Reference_System ?? "<none>",
                        reference_System));
                }

                if (!systemVentilationBinding.CanResolveResults)
                {
                    Refuse(string.Format(
                        "Room {0} was bound to native zone {1} with no zone load, so no result can be resolved for it.",
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationBinding.Reference_SystemZone ?? "<none>"));

                    continue;
                }

                if (!string.Equals(systemVentilationBinding.Reference_ZoneLoad, systemVentilationRoomIntent.Reference_ZoneLoad, StringComparison.OrdinalIgnoreCase))
                {
                    Refuse(string.Format(
                        "Room {0} was bound to zone load {1}, and its analytical space names TAS zone {2}.",
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationBinding.Reference_ZoneLoad,
                        systemVentilationRoomIntent.Reference_ZoneLoad));
                }

                if (space_By_ZoneReference.TryGetValue(systemVentilationBinding.Reference_SystemZone, out Guid guid_Other_Zone))
                {
                    Refuse(string.Format(
                        "Rooms {0} and {1} were both bound to native zone {2}.",
                        guid_Other_Zone,
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationBinding.Reference_SystemZone));
                }
                else
                {
                    space_By_ZoneReference[systemVentilationBinding.Reference_SystemZone] = systemVentilationRoomIntent.Guid_Space;
                }

                if (space_By_LoadReference.TryGetValue(systemVentilationBinding.Reference_ZoneLoad, out Guid guid_Other_Load))
                {
                    Refuse(string.Format(
                        "Rooms {0} and {1} were both bound to zone load {2}.",
                        guid_Other_Load,
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationBinding.Reference_ZoneLoad));
                }
                else
                {
                    space_By_LoadReference[systemVentilationBinding.Reference_ZoneLoad] = systemVentilationRoomIntent.Guid_Space;
                }

                CompareFlow(
                    systemVentilationRoomIntent.DesignFlowRate_Supply_Lps,
                    systemVentilationBinding.DesignFlowRate_Supply_Lps,
                    string.Format("Room {0} supply", systemVentilationRoomIntent.Guid_Space));

                CompareFlow(
                    systemVentilationRoomIntent.DesignFlowRate_Extract_Lps,
                    systemVentilationBinding.DesignFlowRate_Extract_Lps,
                    string.Format("Room {0} extract", systemVentilationRoomIntent.Guid_Space));
            }

            foreach (SystemVentilationBinding systemVentilationBinding in Bindings)
            {
                if (!roomIntents.ContainsKey(systemVentilationBinding.Guid_SystemSpace))
                {
                    Refuse(string.Format(
                        "Native zone {0} was bound to system space {1}, which the source graph does not state.",
                        systemVentilationBinding.Reference_SystemZone ?? "<none>",
                        systemVentilationBinding.Guid_SystemSpace));
                }
            }
        }

        private void ReconcileLegs()
        {
            Dictionary<string, Guid> connection_By_Controller = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in LegIntents)
            {
                if (!systemVentilationLegIntent.IsComplete)
                {
                    Refuse(string.Format(
                        "Leg {0} is incomplete in the source graph: {1}.",
                        systemVentilationLegIntent.Guid_SystemConnection,
                        systemVentilationLegIntent));

                    continue;
                }

                if (!connectionBindings.TryGetValue(systemVentilationLegIntent.Guid_SystemConnection, out SystemVentilationConnectionBinding systemVentilationConnectionBinding)
                    || systemVentilationConnectionBinding == null)
                {
                    Refuse(string.Format(
                        "{0} leg {1} is in the source graph and nothing was materialised for it.",
                        systemVentilationLegIntent.ConnectionType,
                        systemVentilationLegIntent.Guid_SystemConnection));

                    continue;
                }

                if (!systemVentilationConnectionBinding.IsComplete)
                {
                    Refuse(string.Format(
                        "Leg {0} produced an incomplete binding: {1}.",
                        systemVentilationLegIntent.Guid_SystemConnection,
                        systemVentilationConnectionBinding));

                    continue;
                }

                if (systemVentilationConnectionBinding.ConnectionType != systemVentilationLegIntent.ConnectionType)
                {
                    Refuse(string.Format(
                        "Leg {0} was materialised as {1} and the source graph states {2}.",
                        systemVentilationLegIntent.Guid_SystemConnection,
                        systemVentilationConnectionBinding.ConnectionType,
                        systemVentilationLegIntent.ConnectionType));
                }

                if (systemVentilationConnectionBinding.Guid_SystemSpace_From != systemVentilationLegIntent.Guid_SystemSpace_From
                    || systemVentilationConnectionBinding.Guid_SystemSpace_To != systemVentilationLegIntent.Guid_SystemSpace_To)
                {
                    Refuse(string.Format(
                        "Leg {0} was materialised between {1} and {2}, and the source graph runs it between {3} and {4}.",
                        systemVentilationLegIntent.Guid_SystemConnection,
                        systemVentilationConnectionBinding.Guid_SystemSpace_From,
                        systemVentilationConnectionBinding.Guid_SystemSpace_To,
                        systemVentilationLegIntent.Guid_SystemSpace_From,
                        systemVentilationLegIntent.Guid_SystemSpace_To));
                }

                if (systemVentilationConnectionBinding.Guid_SpaceAirMovement != systemVentilationLegIntent.Guid_SpaceAirMovement)
                {
                    Refuse(string.Format(
                        "Transfer leg {0} was materialised from air movement {1}, and the source graph names {2}.",
                        systemVentilationLegIntent.Guid_SystemConnection,
                        systemVentilationConnectionBinding.Guid_SpaceAirMovement,
                        systemVentilationLegIntent.Guid_SpaceAirMovement));
                }

                if (systemVentilationLegIntent.RequiresDutyCarrier)
                {
                    if (string.IsNullOrWhiteSpace(systemVentilationConnectionBinding.Reference_FlowController))
                    {
                        Refuse(string.Format(
                            "{0} leg {1} carries {2} l/s and no native damper was materialised to hold it.",
                            systemVentilationLegIntent.ConnectionType,
                            systemVentilationLegIntent.Guid_SystemConnection,
                            systemVentilationLegIntent.DesignFlowRate_Lps));
                    }
                    else if (connection_By_Controller.TryGetValue(systemVentilationConnectionBinding.Reference_FlowController, out Guid guid_Other))
                    {
                        Refuse(string.Format(
                            "Legs {0} and {1} share native damper {2}, so one duty is standing for two legs.",
                            guid_Other,
                            systemVentilationLegIntent.Guid_SystemConnection,
                            systemVentilationConnectionBinding.Reference_FlowController));
                    }
                    else
                    {
                        connection_By_Controller[systemVentilationConnectionBinding.Reference_FlowController] = systemVentilationLegIntent.Guid_SystemConnection;
                    }
                }

                CompareFlow(
                    systemVentilationLegIntent.DesignFlowRate_Lps,
                    systemVentilationConnectionBinding.DesignFlowRate_Lps,
                    string.Format("{0} leg {1}", systemVentilationLegIntent.ConnectionType, systemVentilationLegIntent.Guid_SystemConnection));
            }

            foreach (SystemVentilationConnectionBinding systemVentilationConnectionBinding in ConnectionBindings)
            {
                if (!legIntents.ContainsKey(systemVentilationConnectionBinding.Guid_SystemConnection))
                {
                    Refuse(string.Format(
                        "Connection {0} was materialised and the source graph does not state it.",
                        systemVentilationConnectionBinding.Guid_SystemConnection));
                }
            }
        }

        private void CompareFlow(double? expected_Lps, double? actual_Lps, string what)
        {
            if (!expected_Lps.HasValue && !actual_Lps.HasValue)
            {
                return;
            }

            if (!expected_Lps.HasValue || !actual_Lps.HasValue)
            {
                Refuse(string.Format(
                    "{0}: the source graph states {1} and the native carrier holds {2}.",
                    what,
                    expected_Lps.HasValue ? expected_Lps.Value.ToString() : "no duty",
                    actual_Lps.HasValue ? actual_Lps.Value.ToString() : "no duty"));

                return;
            }

            CompareFlow(expected_Lps.Value, actual_Lps.Value, what);
        }

        private void CompareFlow(double expected_Lps, double actual_Lps, string what)
        {
            if (double.IsNaN(actual_Lps) || double.IsInfinity(actual_Lps))
            {
                Refuse(string.Format("{0}: the native carrier holds {1} l/s.", what, actual_Lps));
                return;
            }

            double tolerance = global::System.Math.Max(FlowRateTolerance_Absolute, global::System.Math.Abs(expected_Lps) * FlowRateTolerance_Relative);

            if (global::System.Math.Abs(expected_Lps - actual_Lps) > tolerance)
            {
                Refuse(string.Format(
                    "{0}: the source graph states {1} l/s and the native carrier holds {2} l/s.",
                    what,
                    expected_Lps,
                    actual_Lps));
            }
        }

        private static int CompareLegIntents(SystemVentilationLegIntent x, SystemVentilationLegIntent y)
        {
            int result = x.ConnectionType.CompareTo(y.ConnectionType);

            return result != 0 ? result : x.Guid_SystemConnection.CompareTo(y.Guid_SystemConnection);
        }

        private static int CompareConnectionBindings(SystemVentilationConnectionBinding x, SystemVentilationConnectionBinding y)
        {
            int result = x.ConnectionType.CompareTo(y.ConnectionType);

            return result != 0 ? result : x.Guid_SystemConnection.CompareTo(y.Guid_SystemConnection);
        }

        /// <summary>
        /// What the conversion held about one room at the moment it paired it with a native zone. Private
        /// because it is an intermediate: the record callers read is <see cref="SystemVentilationBinding"/>.
        /// </summary>
        private sealed class RoomPairing
        {
            public RoomPairing(string reference_SystemZone, string reference_ZoneLoad, string reference_System, double? designFlowRate_Supply_Lps)
            {
                Reference_SystemZone = reference_SystemZone;
                Reference_ZoneLoad = reference_ZoneLoad;
                Reference_System = reference_System;
                DesignFlowRate_Supply_Lps = designFlowRate_Supply_Lps;
            }

            public string Reference_SystemZone { get; }

            public string Reference_ZoneLoad { get; }

            public string Reference_System { get; }

            public double? DesignFlowRate_Supply_Lps { get; }
        }
    }
}
