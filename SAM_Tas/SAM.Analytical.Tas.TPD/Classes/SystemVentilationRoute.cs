// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// The whole answer of one Part O Iteration 3 ventilation run, in one object: the thermal source it
    /// stood on, the TAS Systems document it built, what that simulation left behind, the identity chain
    /// from every analytical room to its native zone, every leg's design airflow, and each room's zone
    /// temperature series.
    /// <para>
    /// <b>Why it is one object and not a handful of return values.</b> Everything a later stage needs
    /// has to come from here, so that nothing downstream ever has to guess a TSD basename, match a
    /// display name, scan a directory for the file that was written, or reconstruct which room belongs
    /// to which air handling unit by looking at the TPD again. Each of those is a place where a wrong
    /// answer looks exactly like a right one.
    /// </para>
    /// <para>
    /// <b>There is no partial route.</b> When any required stage fails - the conversion did not
    /// reconcile against the source graph, or a room's zone temperature is missing, short or not finite -
    /// <see cref="IsComplete"/> is false and the payload is <b>empty</b>: no bindings, no results, no
    /// document path. That is enforced in the constructor rather than left to a convention, so a caller
    /// that reads the payload without reading <see cref="Refusals"/> cannot get half a route. It is the
    /// same rule PR1's own materialisation follows, for the same reason.
    /// </para>
    /// <para>
    /// <b>A measured TAS success is not the gate.</b> <c>ISystem.Simulate</c> answering <c>"Done"</c> is
    /// recorded in <see cref="SimulationEvidence"/> as positive evidence, and on its own it decides
    /// nothing: the reconciliation and the complete zone temperature do.
    /// </para>
    /// <para>Free of TAS COM types.</para>
    /// </summary>
    public class SystemVentilationRoute
    {
        private readonly RecirculationCoolingResults recirculationCoolingResults;

        /// <summary>
        /// PR5B (SAM#111): a complete route whose document carries recirculation cooling branches, with
        /// the evidence of what each branch did hour by hour - read off a separate plant-room pass on a
        /// copy of the same document, and already checked (no heating, no cooling below the gate,
        /// recirculation within its law's range, ventilation at design). Null for a route with none.
        /// </summary>
        public SystemVentilationRoute(
            NoIzamThermalSource noIzamThermalSource,
            string path_TPD,
            SimulationEvidence simulationEvidence,
            IEnumerable<SystemVentilationBinding> bindings,
            IEnumerable<SystemVentilationConnectionBinding> connectionBindings,
            SystemZoneTemperatureResults systemZoneTemperatureResults,
            RecirculationCoolingResults recirculationCoolingResults,
            IEnumerable<string> refusals,
            IEnumerable<string> notes)
            : this(noIzamThermalSource, path_TPD, simulationEvidence, bindings, connectionBindings, systemZoneTemperatureResults, refusals, notes)
        {
            if (IsComplete)
            {
                this.recirculationCoolingResults = recirculationCoolingResults;
            }
        }

        /// <summary>
        /// SAM#123: a complete route whose document carries manufacturer-guidance cooling units, with what each
        /// unit did hour by hour. Null for a route with none.
        /// </summary>
        public SystemVentilationRoute(
            NoIzamThermalSource noIzamThermalSource,
            string path_TPD,
            SimulationEvidence simulationEvidence,
            IEnumerable<SystemVentilationBinding> bindings,
            IEnumerable<SystemVentilationConnectionBinding> connectionBindings,
            SystemZoneTemperatureResults systemZoneTemperatureResults,
            RecirculationCoolingResults recirculationCoolingResults,
            GuidanceCoolingResults guidanceCoolingResults,
            IEnumerable<string> refusals,
            IEnumerable<string> notes)
            : this(noIzamThermalSource, path_TPD, simulationEvidence, bindings, connectionBindings, systemZoneTemperatureResults, recirculationCoolingResults, refusals, notes)
        {
            if (IsComplete)
            {
                GuidanceCoolingResults = guidanceCoolingResults;
            }
        }

        /// <summary>SAM#123: the manufacturer-guidance evidence, or null where the document carries no such unit.</summary>
        public GuidanceCoolingResults GuidanceCoolingResults { get; }

        /// <summary>The recirculation cooling evidence, or null where the document carries no branch.</summary>
        public RecirculationCoolingResults RecirculationCoolingResults
        {
            get { return recirculationCoolingResults; }
        }

        private readonly List<SystemVentilationBinding> bindings = new List<SystemVentilationBinding>();
        private readonly List<SystemVentilationConnectionBinding> connectionBindings = new List<SystemVentilationConnectionBinding>();
        private readonly List<string> refusals = new List<string>();
        private readonly List<string> notes = new List<string>();

        private readonly SystemZoneTemperatureResults systemZoneTemperatureResults;
        private readonly string path_TPD;

        /// <summary>Built on first use by <see cref="Binding"/>. The route is immutable once built.</summary>
        private Dictionary<Guid, SystemVentilationBinding> binding_By_Space;

        public SystemVentilationRoute(
            NoIzamThermalSource noIzamThermalSource,
            string path_TPD,
            SimulationEvidence simulationEvidence,
            IEnumerable<SystemVentilationBinding> bindings,
            IEnumerable<SystemVentilationConnectionBinding> connectionBindings,
            SystemZoneTemperatureResults systemZoneTemperatureResults,
            IEnumerable<string> refusals,
            IEnumerable<string> notes)
        {
            NoIzamThermalSource = noIzamThermalSource;
            SimulationEvidence = simulationEvidence;

            if (refusals != null)
            {
                this.refusals.AddRange(refusals);
            }

            if (notes != null)
            {
                this.notes.AddRange(notes);
            }

            bool complete = this.refusals.Count == 0
                && noIzamThermalSource != null
                && noIzamThermalSource.IsComplete
                && simulationEvidence != null
                && simulationEvidence.Completed
                && systemZoneTemperatureResults != null
                && systemZoneTemperatureResults.IsComplete
                && !string.IsNullOrWhiteSpace(path_TPD);

            if (!complete)
            {
                //Fail closed, structurally. A refused route hands back nothing a caller could act on -
                //not a binding, not a result, and not the path of the document that failed to reconcile.
                if (this.refusals.Count == 0)
                {
                    this.refusals.Add("The route did not complete, and no stage said why. This is itself a defect.");
                }

                IsComplete = false;
                return;
            }

            this.path_TPD = path_TPD;
            this.systemZoneTemperatureResults = systemZoneTemperatureResults;

            if (bindings != null)
            {
                this.bindings.AddRange(bindings);
            }

            if (connectionBindings != null)
            {
                this.connectionBindings.AddRange(connectionBindings);
            }

            this.bindings.Sort((x, y) => x.Guid_Space.CompareTo(y.Guid_Space));
            this.connectionBindings.Sort(CompareConnectionBindings);

            IsComplete = true;
        }

        /// <summary>The building the route stood on, and the room-to-TAS-zone identity map with it.</summary>
        public NoIzamThermalSource NoIzamThermalSource { get; }

        /// <summary>
        /// The TAS Systems document this route produced. <b>Null on a refused route</b>: a document that
        /// did not reconcile is not a deliverable, and a caller that could reach for it would be reading
        /// numbers nothing had checked.
        /// </summary>
        public string Path_TPD
        {
            get { return path_TPD; }
        }

        /// <summary>
        /// What the Systems simulation left behind, including TAS's own diagnostic verbatim in
        /// <c>NativeDiagnostic</c>. Kept even on a refused route, because it is the diagnosis.
        /// </summary>
        public SimulationEvidence SimulationEvidence { get; }

        /// <summary>
        /// One row per room: <c>Space.Guid -&gt; SystemSpace.Guid -&gt; AirSystem.Guid -&gt; native zone
        /// -&gt; native zone load -&gt; native system</c>, with the room's supply and extract duties.
        /// Ordered by <c>Space.Guid</c>. Empty on a refused route.
        /// </summary>
        public List<SystemVentilationBinding> Bindings
        {
            get { return new List<SystemVentilationBinding>(bindings); }
        }

        /// <summary>
        /// One row per PR1 ventilation connection: which leg, between which rooms, carrying which design
        /// airflow, on which native carrier. Ordered by type then connection guid. Empty on a refused
        /// route.
        /// </summary>
        public List<SystemVentilationConnectionBinding> ConnectionBindings
        {
            get { return new List<SystemVentilationConnectionBinding>(connectionBindings); }
        }

        /// <summary>
        /// Every room's <c>ZoneTemperature</c> series for the requested period. Null on a refused route.
        /// </summary>
        public SystemZoneTemperatureResults SystemZoneTemperatureResults
        {
            get { return systemZoneTemperatureResults; }
        }

        /// <summary>Every reason this route is not usable. Ordered, and never empty on a refusal.</summary>
        public List<string> Refusals
        {
            get { return new List<string>(refusals); }
        }

        /// <summary>What the route did. Never a substitute for a refusal.</summary>
        public List<string> Notes
        {
            get { return new List<string>(notes); }
        }

        /// <summary>
        /// Whether this route is a usable answer: a complete thermal source, an evidenced simulation, a
        /// conversion that reconciled against the source graph, and a complete finite zone temperature
        /// for every room.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        /// One room's binding, by analytical room guid. Null on a refused route.
        /// <para>
        /// Indexed, not scanned: this is how a later stage walks its rooms, and a linear scan here
        /// would make that walk quadratic on the room count.
        /// </para>
        /// </summary>
        public SystemVentilationBinding Binding(Guid guid_Space)
        {
            if (binding_By_Space == null)
            {
                binding_By_Space = new Dictionary<Guid, SystemVentilationBinding>();

                foreach (SystemVentilationBinding systemVentilationBinding in bindings)
                {
                    binding_By_Space[systemVentilationBinding.Guid_Space] = systemVentilationBinding;
                }
            }

            return binding_By_Space.TryGetValue(guid_Space, out SystemVentilationBinding result) ? result : null;
        }

        public override string ToString()
        {
            return IsComplete
                ? string.Format(
                    "Ventilation route: {0} room(s), {1} leg(s), zone temperature for hours {2}..{3}, {4}",
                    bindings.Count,
                    connectionBindings.Count,
                    systemZoneTemperatureResults.StartHour,
                    systemZoneTemperatureResults.EndHour,
                    path_TPD)
                : string.Format("Ventilation route REFUSED ({0} reason(s)).", refusals.Count);
        }

        private static int CompareConnectionBindings(SystemVentilationConnectionBinding x, SystemVentilationConnectionBinding y)
        {
            int result = x.ConnectionType.CompareTo(y.ConnectionType);

            return result != 0 ? result : x.Guid_SystemConnection.CompareTo(y.Guid_SystemConnection);
        }
    }
}
