// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        /// <summary>
        /// Reads each bound room's <c>ZoneTemperature</c> series out of a simulated TPD, resolving every
        /// room through the identity chain its own binding states.
        /// <para>
        /// <b>The chain, and nothing else.</b> For each binding:
        /// <c>Space.Guid -&gt; SystemSpace.Guid -&gt; Reference_SystemZone -&gt;
        /// System.GetComponentByGUID -&gt; the series</c>. No name is compared, no zone list is scanned
        /// per room, and no room is matched by position. The zone the series comes off is checked to be
        /// bound to the zone load the binding names, so a series read from a zone that has quietly been
        /// re-bound is a refusal rather than a plausible-looking number.
        /// </para>
        /// <para>
        /// <b>One series, not eleven.</b> Only <c>SpaceDataType.ZoneTemperature</c> is requested. TAS
        /// exposes eleven space series and the conversion that reads them all does eleven COM round
        /// trips per room for ten of them nobody uses.
        /// </para>
        /// <para>
        /// <b>The period is the requested one.</b> <c>startHour</c> and <c>endHour</c> are 0-based and
        /// inclusive; TAS is 1-based, so <c>+1</c> is applied on the way in and the returned series is
        /// indexed by the 0-based hour. Nothing here assumes 8760.
        /// </para>
        /// </summary>
        /// <param name="path_TPD">The simulated document. Opened read-only and never written.</param>
        /// <param name="systemVentilationBindings">The room bindings the conversion produced.</param>
        /// <param name="startHour">0-based first hour.</param>
        /// <param name="endHour">0-based last hour, inclusive.</param>
        public static SystemZoneTemperatureResults ToSAM_SystemZoneTemperatureResults(
            string path_TPD,
            IEnumerable<SystemVentilationBinding> systemVentilationBindings,
            int startHour,
            int endHour)
        {
            SystemZoneTemperatureResults result = new SystemZoneTemperatureResults(startHour, endHour);

            if (string.IsNullOrWhiteSpace(path_TPD) || !System.IO.File.Exists(path_TPD))
            {
                result.Refuse(string.Concat("The simulated TPD does not exist: ", path_TPD ?? "<none>"));
                return result;
            }

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD, true))
            {
                TPDDoc tPDDoc = sAMTPDDocument?.TPDDocument;

                if (tPDDoc == null)
                {
                    result.Refuse(string.Concat("The simulated TPD could not be opened: ", path_TPD));
                    return result;
                }

                ToSAM_SystemZoneTemperatureResults(tPDDoc, systemVentilationBindings, result);
            }

            return result;
        }

        /// <summary>
        /// The same read against an already-open document, so a caller that has one need not reopen it.
        /// </summary>
        public static SystemZoneTemperatureResults ToSAM_SystemZoneTemperatureResults(
            this TPDDoc tPDDoc,
            IEnumerable<SystemVentilationBinding> systemVentilationBindings,
            int startHour,
            int endHour)
        {
            SystemZoneTemperatureResults result = new SystemZoneTemperatureResults(startHour, endHour);

            ToSAM_SystemZoneTemperatureResults(tPDDoc, systemVentilationBindings, result);

            return result;
        }

        /// <summary>
        /// The same read against <b>one</b> air system the caller already holds.
        /// <para>
        /// This is the overload the simulation loop uses. Licensed TAS replaces the previous air
        /// system's in-memory result surface when <c>ISystem.Simulate</c> is called for the next one, so
        /// each system's series has to be taken before the loop advances - and taking them through the
        /// document overload would re-index every air system in the document once per system, which is
        /// quadratic in the unit count. Here the system is passed in, so the read is linear in the rooms
        /// it serves and nothing is walked twice.
        /// </para>
        /// </summary>
        public static SystemZoneTemperatureResults ToSAM_SystemZoneTemperatureResults(
            this global::TPD.System system,
            IEnumerable<SystemVentilationBinding> systemVentilationBindings,
            int startHour,
            int endHour)
        {
            SystemZoneTemperatureResults result = new SystemZoneTemperatureResults(startHour, endHour);

            if (system == null)
            {
                result.Refuse("No air system to read zone temperatures from.");
                return result;
            }

            if (systemVentilationBindings == null)
            {
                result.Refuse("No room bindings were supplied, so no series could be resolved by identity.");
                return result;
            }

            Dictionary<string, global::TPD.System> dictionary_System = new Dictionary<string, global::TPD.System>(StringComparer.OrdinalIgnoreCase);

            string reference_System = Query.NativeReference(system);

            if (!string.IsNullOrWhiteSpace(reference_System))
            {
                dictionary_System[reference_System] = system;
            }

            Read(dictionary_System, systemVentilationBindings, result);

            return result;
        }

        private static void ToSAM_SystemZoneTemperatureResults(
            TPDDoc tPDDoc,
            IEnumerable<SystemVentilationBinding> systemVentilationBindings,
            SystemZoneTemperatureResults systemZoneTemperatureResults)
        {
            if (tPDDoc == null)
            {
                systemZoneTemperatureResults.Refuse("No document to read zone temperatures from.");
                return;
            }

            if (systemVentilationBindings == null)
            {
                systemZoneTemperatureResults.Refuse("No room bindings were supplied, so no series could be resolved by identity.");
                return;
            }

            //-------------------------------------------------------------------------------------------
            //One pass over the document's air systems, indexed by their native guid. After this every
            //room resolves in two dictionary probes and one GetComponentByGUID - never by walking the
            //document again.
            //-------------------------------------------------------------------------------------------
            Dictionary<string, global::TPD.System> dictionary_System = new Dictionary<string, global::TPD.System>(StringComparer.OrdinalIgnoreCase);

            List<PlantRoom> plantRooms = tPDDoc.PlantRooms();
            if (plantRooms != null)
            {
                foreach (PlantRoom plantRoom in plantRooms)
                {
                    List<global::TPD.System> systems = plantRoom?.Systems();
                    if (systems == null)
                    {
                        continue;
                    }

                    foreach (global::TPD.System system in systems)
                    {
                        string reference = Query.NativeReference(system);
                        if (!string.IsNullOrWhiteSpace(reference))
                        {
                            dictionary_System[reference] = system;
                        }
                    }
                }
            }

            Read(dictionary_System, systemVentilationBindings, systemZoneTemperatureResults);
        }

        /// <summary>
        /// Reads one series per binding, in ascending room guid so the answer never depends on the
        /// order the bindings arrived in.
        /// </summary>
        private static void Read(
            Dictionary<string, global::TPD.System> dictionary_System,
            IEnumerable<SystemVentilationBinding> systemVentilationBindings,
            SystemZoneTemperatureResults systemZoneTemperatureResults)
        {
            List<SystemVentilationBinding> bindings = new List<SystemVentilationBinding>(systemVentilationBindings);
            bindings.Sort((x, y) => x.Guid_Space.CompareTo(y.Guid_Space));

            foreach (SystemVentilationBinding systemVentilationBinding in bindings)
            {
                if (systemVentilationBinding == null)
                {
                    continue;
                }

                systemZoneTemperatureResults.Add(Read(
                    dictionary_System,
                    systemVentilationBinding,
                    systemZoneTemperatureResults.StartHour,
                    systemZoneTemperatureResults.EndHour));
            }
        }

        private static SystemZoneTemperatureResult Read(
            Dictionary<string, global::TPD.System> dictionary_System,
            SystemVentilationBinding systemVentilationBinding,
            int startHour,
            int endHour)
        {
            if (!systemVentilationBinding.CanResolveResults)
            {
                return new SystemZoneTemperatureResult(
                    systemVentilationBinding.Guid_Space,
                    systemVentilationBinding.Guid_SystemSpace,
                    systemVentilationBinding.Reference_SystemZone,
                    systemVentilationBinding.Reference_ZoneLoad,
                    startHour,
                    endHour,
                    null,
                    "its binding does not name both a native zone and a zone load, so no series can be resolved for it.");
            }

            if (!dictionary_System.TryGetValue(systemVentilationBinding.Reference_System, out global::TPD.System system))
            {
                return new SystemZoneTemperatureResult(
                    systemVentilationBinding.Guid_Space,
                    systemVentilationBinding.Guid_SystemSpace,
                    systemVentilationBinding.Reference_SystemZone,
                    systemVentilationBinding.Reference_ZoneLoad,
                    startHour,
                    endHour,
                    null,
                    string.Format("the simulated document holds no TAS system {0}.", systemVentilationBinding.Reference_System ?? "<none>"));
            }

            SystemZone systemZone;

            try
            {
                systemZone = system.GetComponentByGUID(systemVentilationBinding.Reference_SystemZone) as SystemZone;
            }
            catch (Exception exception)
            {
                return new SystemZoneTemperatureResult(
                    systemVentilationBinding.Guid_Space,
                    systemVentilationBinding.Guid_SystemSpace,
                    systemVentilationBinding.Reference_SystemZone,
                    systemVentilationBinding.Reference_ZoneLoad,
                    startHour,
                    endHour,
                    null,
                    string.Format("resolving native zone {0} threw {1}: {2}", systemVentilationBinding.Reference_SystemZone, exception.GetType().Name, exception.Message));
            }

            if (systemZone == null)
            {
                return new SystemZoneTemperatureResult(
                    systemVentilationBinding.Guid_Space,
                    systemVentilationBinding.Guid_SystemSpace,
                    systemVentilationBinding.Reference_SystemZone,
                    systemVentilationBinding.Reference_ZoneLoad,
                    startHour,
                    endHour,
                    null,
                    string.Format("native zone {0} is not in the simulated document.", systemVentilationBinding.Reference_SystemZone));
            }

            //The zone must still be bound to the load the room's binding names. A zone re-bound between
            //conversion and reading would hand back a real series belonging to another room.
            string reference_ZoneLoad = Query.NativeReference_ZoneLoad(systemZone);

            if (!string.Equals(reference_ZoneLoad, systemVentilationBinding.Reference_ZoneLoad, StringComparison.OrdinalIgnoreCase))
            {
                return new SystemZoneTemperatureResult(
                    systemVentilationBinding.Guid_Space,
                    systemVentilationBinding.Guid_SystemSpace,
                    systemVentilationBinding.Reference_SystemZone,
                    reference_ZoneLoad,
                    startHour,
                    endHour,
                    null,
                    string.Format(
                        "native zone {0} now reports zone load {1} and its binding names {2}.",
                        systemVentilationBinding.Reference_SystemZone,
                        reference_ZoneLoad ?? "<none>",
                        systemVentilationBinding.Reference_ZoneLoad));
            }

            //Only ZoneTemperature. TAS is 1-based, so the request is shifted and the series comes back
            //indexed by the 0-based hour.
            IndexedDoubles indexedDoubles = Create.IndexedDoubles(
                (global::TPD.SystemComponent)systemZone,
                SpaceDataType.ZoneTemperature,
                startHour + 1,
                endHour + 1,
                out string diagnostic);

            return new SystemZoneTemperatureResult(
                systemVentilationBinding.Guid_Space,
                systemVentilationBinding.Guid_SystemSpace,
                systemVentilationBinding.Reference_SystemZone,
                reference_ZoneLoad,
                startHour,
                endHour,
                indexedDoubles,
                diagnostic);
        }
    }
}
