// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// Everything the ResultantTemperature thermostat bridge has to be sure of <b>before</b> a single TBD is
    /// copied: which no-IZAM building it stands on, which rooms it bridges, which TBD zone each room is, and
    /// that every room's achieved <c>ZoneTemperature</c> is a complete, finite annual series.
    /// <para>
    /// <b>Everything comes off one <see cref="SystemVentilationRoute"/>.</b> The building to copy is the
    /// route's own <see cref="SystemVentilationRoute.NoIzamThermalSource"/> - the explicitly paired no-IZAM
    /// TBD whose TSD the Systems zone loads were bound to - so no path is derived from a TPD basename, and no
    /// other TBD can be substituted for it. The rooms are the route's own bindings and the series its own
    /// validated results.
    /// </para>
    /// <para>
    /// <b>Identity is guid, twice over.</b> Each room resolves to a TBD zone through the source's
    /// <c>Space.Guid -&gt; SpaceParameter.ZoneGuid</c> map, and that zone guid must equal the
    /// <c>ZoneLoad.GUID</c> the route bound the room's Systems zone to. Two independent statements of the
    /// same identity that disagree are refused, never reconciled. No display name is read anywhere.
    /// </para>
    /// <para>
    /// <b>Annual, exactly.</b> The bridge simulates the copied building over days
    /// <see cref="FirstDay"/>..<see cref="LastDay"/> and a TBD yearly thermostat profile holds
    /// <see cref="HoursPerYear"/> hours, so the only period it can transfer without padding, truncating or
    /// repeating data is hours <see cref="StartHour"/>..<see cref="EndHour"/>. Any other period is refused.
    /// That also keeps the bridge off the one-day period that crashes <c>TSD.exe</c> (SAM-BIM/SAM#115).
    /// </para>
    /// <para>
    /// <b>No partial plan.</b> If one room cannot be bridged the plan is not valid and carries no transfers
    /// at all. Free of TAS COM types.
    /// </para>
    /// </summary>
    public class ThermostatBridgePlan
    {
        /// <summary>Hours in the yearly thermostat profile a TBD holds.</summary>
        public const int HoursPerYear = 8760;

        /// <summary>First simulated day of the copied building.</summary>
        public const int FirstDay = 1;

        /// <summary>Last simulated day of the copied building.</summary>
        public const int LastDay = 365;

        /// <summary>0-based first hour of the only period the bridge transfers.</summary>
        public const int StartHour = 0;

        /// <summary>0-based last hour, inclusive.</summary>
        public const int EndHour = HoursPerYear - 1;

        private readonly List<ThermostatBridgeTransfer> transfers = new List<ThermostatBridgeTransfer>();
        private readonly Dictionary<Guid, ThermostatBridgeTransfer> transfer_By_Space = new Dictionary<Guid, ThermostatBridgeTransfer>();
        private readonly Dictionary<string, ThermostatBridgeTransfer> transfer_By_Key = new Dictionary<string, ThermostatBridgeTransfer>(StringComparer.Ordinal);
        private readonly List<string> refusals = new List<string>();

        public ThermostatBridgePlan(SystemVentilationRoute systemVentilationRoute)
        {
            SystemVentilationRoute = systemVentilationRoute;
            NoIzamThermalSource = systemVentilationRoute?.NoIzamThermalSource;

            List<ThermostatBridgeTransfer> transfers_Temp = Plan(systemVentilationRoute, refusals);

            if (refusals.Count != 0 || transfers_Temp == null || transfers_Temp.Count == 0)
            {
                if (refusals.Count == 0)
                {
                    refusals.Add("The thermostat bridge planned no room and said nothing about why. This is itself a defect.");
                }

                return;
            }

            foreach (ThermostatBridgeTransfer transfer in transfers_Temp)
            {
                transfers.Add(transfer);
                transfer_By_Space[transfer.Guid_Space] = transfer;
                transfer_By_Key[transfer.Key] = transfer;
            }
        }

        /// <summary>The Systems route the bridge stands on.</summary>
        public SystemVentilationRoute SystemVentilationRoute { get; }

        /// <summary>The no-IZAM building the bridge copies - the route's own, never re-derived.</summary>
        public NoIzamThermalSource NoIzamThermalSource { get; }

        /// <summary>One transfer per bridged room, ordered by <c>Space.Guid</c>. Empty on a refused plan.</summary>
        public List<ThermostatBridgeTransfer> Transfers
        {
            get { return new List<ThermostatBridgeTransfer>(transfers); }
        }

        /// <summary>How many rooms the plan bridges.</summary>
        public int Count
        {
            get { return transfers.Count; }
        }

        /// <summary>Every reason the bridge cannot proceed.</summary>
        public List<string> Refusals
        {
            get { return new List<string>(refusals); }
        }

        /// <summary>Whether the bridge may proceed: at least one room, and no refusal.</summary>
        public bool IsValid
        {
            get { return refusals.Count == 0 && transfers.Count != 0; }
        }

        /// <summary>One room's transfer, by analytical room guid. Indexed, never scanned.</summary>
        public ThermostatBridgeTransfer Transfer(Guid guid_Space)
        {
            return transfer_By_Space.TryGetValue(guid_Space, out ThermostatBridgeTransfer result) ? result : null;
        }

        /// <summary>One room's transfer, by TAS zone guid in any spelling. Indexed, never scanned.</summary>
        public ThermostatBridgeTransfer Transfer(string reference_Zone)
        {
            string key = Query.ZoneReferenceKey(reference_Zone);

            return key != null && transfer_By_Key.TryGetValue(key, out ThermostatBridgeTransfer result) ? result : null;
        }

        private static List<ThermostatBridgeTransfer> Plan(SystemVentilationRoute systemVentilationRoute, List<string> refusals)
        {
            if (systemVentilationRoute == null)
            {
                refusals.Add("No Systems route was supplied, so there is no achieved zone temperature to bridge.");
                return null;
            }

            if (!systemVentilationRoute.IsComplete)
            {
                refusals.Add("The Systems route is not complete, so its zone temperatures are not an answer to bridge.");
                refusals.AddRange(systemVentilationRoute.Refusals);
                return null;
            }

            NoIzamThermalSource noIzamThermalSource = systemVentilationRoute.NoIzamThermalSource;

            if (noIzamThermalSource == null)
            {
                refusals.Add("The Systems route states no thermal source, so there is no no-IZAM building to copy.");
                return null;
            }

            //Checked here as well as by the route: the bridge is about to copy this building and simulate it,
            //and a building still carrying its own mechanical ventilation would be reconstructing a state the
            //Systems simulation never had.
            if (!noIzamThermalSource.IsComplete || !noIzamThermalSource.RemovedIZAMs || !noIzamThermalSource.RemovedMechanicalVentilationGains)
            {
                refusals.Add(string.Format(
                    "The thermal source is not proven to be the no-IZAM building the Systems route stood on (complete: {0}, IZAMs removed: {1}, mechanical ventilation gain removed: {2}).",
                    noIzamThermalSource.IsComplete,
                    noIzamThermalSource.RemovedIZAMs,
                    noIzamThermalSource.RemovedMechanicalVentilationGains));

                refusals.AddRange(noIzamThermalSource.Refusals);
                return null;
            }

            SystemZoneTemperatureResults systemZoneTemperatureResults = systemVentilationRoute.SystemZoneTemperatureResults;

            if (systemZoneTemperatureResults == null || !systemZoneTemperatureResults.IsComplete)
            {
                refusals.Add("The Systems route carries no validated zone temperature results.");
                return null;
            }

            if (systemZoneTemperatureResults.StartHour != StartHour || systemZoneTemperatureResults.EndHour != EndHour)
            {
                refusals.Add(string.Format(
                    "The Systems route ran hours {0}..{1}. The bridge simulates the copied building over days {2}..{3} and a TBD yearly "
                    + "thermostat profile holds {4} hours, so only hours {5}..{6} can be transferred without padding, truncating or "
                    + "repeating data.",
                    systemZoneTemperatureResults.StartHour,
                    systemZoneTemperatureResults.EndHour,
                    FirstDay,
                    LastDay,
                    HoursPerYear,
                    StartHour,
                    EndHour));

                return null;
            }

            List<SystemVentilationBinding> systemVentilationBindings = systemVentilationRoute.Bindings;

            if (systemVentilationBindings.Count == 0)
            {
                refusals.Add("The Systems route bound no room, so there is nothing to bridge.");
                return null;
            }

            systemVentilationBindings.Sort((x, y) => x.Guid_Space.CompareTo(y.Guid_Space));

            List<ThermostatBridgeTransfer> result = new List<ThermostatBridgeTransfer>();
            HashSet<Guid> guids_Space = new HashSet<Guid>();
            Dictionary<string, Guid> space_By_Key = new Dictionary<string, Guid>(StringComparer.Ordinal);

            double[] zoneTemperatures = new double[HoursPerYear];

            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationBindings)
            {
                Guid guid_Space = systemVentilationBinding.Guid_Space;

                if (guid_Space == Guid.Empty)
                {
                    refusals.Add("A room binding names no analytical room.");
                    continue;
                }

                if (!guids_Space.Add(guid_Space))
                {
                    refusals.Add(string.Format("Room {0} is bound twice, so which zone temperature belongs to it is not stated.", guid_Space));
                    continue;
                }

                string reference_Zone = noIzamThermalSource.ZoneReference(guid_Space);
                string key = Query.ZoneReferenceKey(reference_Zone);

                if (key == null)
                {
                    refusals.Add(string.Format("Room {0}: the no-IZAM source states no TBD zone for it, so there is no zone to bridge it into.", guid_Space));
                    continue;
                }

                if (!string.Equals(key, Query.ZoneReferenceKey(systemVentilationBinding.Reference_ZoneLoad), StringComparison.Ordinal))
                {
                    refusals.Add(string.Format(
                        "Room {0}: the no-IZAM source states TBD zone {1} and the Systems route bound zone load {2}. The two statements "
                        + "of the room's identity disagree, so which zone its temperature belongs to is ambiguous.",
                        guid_Space,
                        reference_Zone,
                        systemVentilationBinding.Reference_ZoneLoad ?? "<none>"));

                    continue;
                }

                if (space_By_Key.TryGetValue(key, out Guid guid_Other))
                {
                    refusals.Add(string.Format("Rooms {0} and {1} both resolve to TBD zone {2}.", guid_Other, guid_Space, reference_Zone));
                    continue;
                }

                space_By_Key[key] = guid_Space;

                SystemZoneTemperatureResult systemZoneTemperatureResult = systemZoneTemperatureResults.Result(guid_Space);

                if (systemZoneTemperatureResult == null)
                {
                    refusals.Add(string.Format("Room {0}: the Systems route returned no zone temperature series for it.", guid_Space));
                    continue;
                }

                string refusal = systemZoneTemperatureResult.Refusal();
                if (refusal != null)
                {
                    refusals.Add(refusal);
                    continue;
                }

                if (systemZoneTemperatureResult.StartHour != StartHour || systemZoneTemperatureResult.EndHour != EndHour || systemZoneTemperatureResult.Count != HoursPerYear)
                {
                    refusals.Add(string.Format(
                        "Room {0}: its zone temperature covers hours {1}..{2} with {3} value(s); the bridge needs {4} values over hours {5}..{6}.",
                        guid_Space,
                        systemZoneTemperatureResult.StartHour,
                        systemZoneTemperatureResult.EndHour,
                        systemZoneTemperatureResult.Count,
                        HoursPerYear,
                        StartHour,
                        EndHour));

                    continue;
                }

                string refusal_Series = null;

                for (int hour = StartHour; hour <= EndHour; hour++)
                {
                    //TryGetValue, never Values: the latter copies the whole series on every call.
                    if (!systemZoneTemperatureResult.TryGetValue(hour, out double value))
                    {
                        refusal_Series = string.Format("Room {0}: no zone temperature at hour {1}.", guid_Space, hour);
                        break;
                    }

                    //Finite as a double is not enough: a finite double beyond the single range becomes an
                    //infinity the moment TAS stores it.
                    if (double.IsNaN(value) || double.IsInfinity(value) || float.IsInfinity((float)value))
                    {
                        refusal_Series = string.Format("Room {0}: the zone temperature at hour {1} is {2}, which is not a temperature a thermostat can hold.", guid_Space, hour, value);
                        break;
                    }

                    zoneTemperatures[hour - StartHour] = value;
                }

                if (refusal_Series != null)
                {
                    refusals.Add(refusal_Series);
                    continue;
                }

                result.Add(new ThermostatBridgeTransfer(guid_Space, reference_Zone, StartHour, zoneTemperatures));
            }

            //A series for a room nobody bound would otherwise be silently dropped.
            foreach (SystemZoneTemperatureResult systemZoneTemperatureResult in systemZoneTemperatureResults.Results)
            {
                if (!guids_Space.Contains(systemZoneTemperatureResult.Guid_Space))
                {
                    refusals.Add(string.Format(
                        "A zone temperature series came back for room {0}, which the Systems route did not bind.",
                        systemZoneTemperatureResult.Guid_Space));
                }
            }

            return result;
        }

        public override string ToString()
        {
            return IsValid
                ? string.Format("Thermostat bridge plan: {0} room(s), hours {1}..{2}", transfers.Count, StartHour, EndHour)
                : string.Format("Thermostat bridge plan REFUSED ({0} reason(s)).", refusals.Count);
        }
    }
}
