// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// Binds one native zone to the <b>intended</b> TSD zone load, by identity.
        /// <para>
        /// <b>The measurement this rests on.</b> On licensed TAS, fixture 1's TBD answers
        /// <c>zone[0] "Cell 1" GUID={37FA3D5C-27E0-41D5-8825-366F8DBD66AD}</c>, and the TSD written from
        /// it answers <b>the same guid</b> for the zone load of the same room:
        /// </para>
        /// <code>
        /// load[1] name="Cell 1" GUID={37FA3D5C-27E0-41D5-8825-366F8DBD66AD}
        ///       GetZoneLoadForGuid(own guid)  -&gt; Cell 1
        ///       GetZoneLoadForGuid(bare guid) -&gt; Cell 1
        /// VERDICT: 2 of 2 ZoneLoad GUIDs are also TBD zone GUIDs
        /// </code>
        /// <para>
        /// SAM stamps that guid onto the analytical space as <c>SpaceParameter.ZoneGuid</c> during the
        /// TBD workflow, so the room's intent can name the load it wants and TAS can hand it over. The
        /// braced and the bare form both resolve; the braced form, which is what SAM stores, is used.
        /// </para>
        /// <para>
        /// <b>A zone already carrying the wrong load is corrected, not added to.</b> Leaving a stale
        /// load in place would give the room two results and no way to say which was its own.
        /// </para>
        /// </summary>
        public static bool BindZoneLoad(
            SystemZone systemZone,
            EnergyCentre energyCentre,
            SystemVentilationRoomIntent systemVentilationRoomIntent,
            SystemVentilationConversionContext systemVentilationConversionContext)
        {
            if (systemZone == null || systemVentilationRoomIntent == null || systemVentilationConversionContext == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(systemVentilationRoomIntent.Reference_ZoneLoad))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0} states no TAS zone guid, so its zone load cannot be bound by identity. Its "
                    + "analytical space was never stamped with SpaceParameter.ZoneGuid.",
                    systemVentilationRoomIntent.Guid_Space));

                return false;
            }

            TSDData tSDData = energyCentre?.GetTSDData(1);
            if (tSDData == null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0} cannot be bound to a zone load: the energy centre carries no TSD.",
                    systemVentilationRoomIntent.Guid_Space));

                return false;
            }

            ZoneLoad zoneLoad;

            try
            {
                zoneLoad = tSDData.GetZoneLoadForGuid(systemVentilationRoomIntent.Reference_ZoneLoad);
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0}: resolving TAS zone {1} in the TSD threw {2}: {3}.",
                    systemVentilationRoomIntent.Guid_Space,
                    systemVentilationRoomIntent.Reference_ZoneLoad,
                    exception.GetType().Name,
                    exception.Message));

                return false;
            }

            if (zoneLoad == null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0} names TAS zone {1}, which the TSD does not hold. The thermal source and the "
                    + "systems graph are not the same model.",
                    systemVentilationRoomIntent.Guid_Space,
                    systemVentilationRoomIntent.Reference_ZoneLoad));

                return false;
            }

            dynamic @dynamic = systemZone;

            //Anything already bound that is not the intended load is removed first: a zone carrying two
            //loads has two results and no way to say which is the room's.
            bool bound = false;

            List<ZoneLoad> zoneLoads = Query.ZoneLoads(systemZone);
            if (zoneLoads != null)
            {
                foreach (ZoneLoad zoneLoad_Existing in zoneLoads)
                {
                    if (string.Equals(Query.NativeReference(zoneLoad_Existing), systemVentilationRoomIntent.Reference_ZoneLoad, StringComparison.OrdinalIgnoreCase))
                    {
                        bound = true;
                        continue;
                    }

                    try
                    {
                        @dynamic.RemoveZoneLoad(zoneLoad_Existing);
                    }
                    catch (Exception exception)
                    {
                        systemVentilationConversionContext.Refuse(string.Format(
                            "Room {0}: removing the zone load already on its native zone threw {1}: {2}.",
                            systemVentilationRoomIntent.Guid_Space,
                            exception.GetType().Name,
                            exception.Message));

                        return false;
                    }
                }
            }

            if (!bound)
            {
                try
                {
                    @dynamic.AddZoneLoad(zoneLoad);
                }
                catch (Exception exception)
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Room {0}: AddZoneLoad for TAS zone {1} threw {2}: {3}.",
                        systemVentilationRoomIntent.Guid_Space,
                        systemVentilationRoomIntent.Reference_ZoneLoad,
                        exception.GetType().Name,
                        exception.Message));

                    return false;
                }
            }

            //Verified, not trusted. GetSystemZoneZoneLoad is the explicit zone-to-load direction, and it
            //must now answer the load the room asked for.
            string reference_Bound = Query.NativeReference_ZoneLoad(systemZone);

            if (!string.Equals(reference_Bound, systemVentilationRoomIntent.Reference_ZoneLoad, StringComparison.OrdinalIgnoreCase))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0} asked for zone load {1} and its native zone reports {2} after binding.",
                    systemVentilationRoomIntent.Guid_Space,
                    systemVentilationRoomIntent.Reference_ZoneLoad,
                    reference_Bound ?? "<none>"));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Writes a room's <b>supply</b> duty onto its native zone as an absolute value, and reports what
        /// the zone holds afterwards.
        /// <para>
        /// <b>Both carriers, and the type as well as the value.</b> A <c>SystemZone</c> has exactly two
        /// flow carriers - <c>FlowRate</c> (total supply air) and <c>FreshAir</c> (the outside-air
        /// portion) - and PR1 states one duty for both, refusing a template that splits them. Writing
        /// <c>Value</c> without <c>Type = tpdSizedVariableValue</c> would leave TAS's own sizing rule in
        /// place - <c>tpdSizeFlowACH</c> at 8 air changes an hour on a new zone - and the litres per
        /// second would be silently replaced by whatever that computed. Measured: a 200 m3 zone left on
        /// the ACH rule sized itself to 444.44444444444446 l/s.
        /// </para>
        /// <para>
        /// <b>Unit: l/s.</b> Proven by TAS's own arithmetic - 200 m3 at 8 ACH is 1600 m3/h, 0.4444 m3/s
        /// and 444.444 l/s, and TAS answered 444.44444444444446.
        /// </para>
        /// <para>
        /// <b>A room with no supply leg is written a stated zero</b>, not left alone: an untouched zone
        /// would size itself and invent a supply the design does not have. The value reported back is
        /// null for such a room, because "no supply leg" and "a supply leg of nothing" are different
        /// facts and the binding states which it is.
        /// </para>
        /// </summary>
        /// <param name="designFlowRate_Supply_Lps">
        /// What the zone holds after the write, read back off it - null where the room has no supply leg.
        /// </param>
        public static bool SetSupplyDesignFlowRate(
            SystemZone systemZone,
            SystemVentilationRoomIntent systemVentilationRoomIntent,
            SystemVentilationConversionContext systemVentilationConversionContext,
            out double? designFlowRate_Supply_Lps)
        {
            designFlowRate_Supply_Lps = null;

            if (systemZone == null || systemVentilationRoomIntent == null || systemVentilationConversionContext == null)
            {
                return false;
            }

            double value = systemVentilationRoomIntent.DesignFlowRate_Supply_Lps.GetValueOrDefault();

            SizedFlowVariable sizedFlowVariable_FlowRate = systemZone.FlowRate;
            SizedFlowVariable sizedFlowVariable_FreshAir = systemZone.FreshAir;

            if (sizedFlowVariable_FlowRate == null || sizedFlowVariable_FreshAir == null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0}: its native zone exposes no {1} carrier, so its supply duty cannot be written.",
                    systemVentilationRoomIntent.Guid_Space,
                    sizedFlowVariable_FlowRate == null ? "FlowRate" : "FreshAir"));

                return false;
            }

            try
            {
                sizedFlowVariable_FlowRate.Value = value;
                sizedFlowVariable_FlowRate.Type = tpdSizedVariable.tpdSizedVariableValue;

                sizedFlowVariable_FreshAir.Value = value;
                sizedFlowVariable_FreshAir.Type = tpdSizedVariable.tpdSizedVariableValue;
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0}: writing its supply duty of {1} l/s threw {2}: {3}.",
                    systemVentilationRoomIntent.Guid_Space,
                    value,
                    exception.GetType().Name,
                    exception.Message));

                return false;
            }

            double value_FlowRate = systemZone.FlowRate.Value;
            double value_FreshAir = systemZone.FreshAir.Value;

            if (systemZone.FlowRate.Type != tpdSizedVariable.tpdSizedVariableValue
                || systemZone.FreshAir.Type != tpdSizedVariable.tpdSizedVariableValue)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0}: its native zone did not keep the absolute flow type, so TAS would size the room "
                    + "rather than use the {1} l/s the design states.",
                    systemVentilationRoomIntent.Guid_Space,
                    value));

                return false;
            }

            if (!Query.Equivalent(value_FlowRate, value_FreshAir))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Room {0}: its native zone holds {1} l/s of supply air and {2} l/s of fresh air, which the "
                    + "design does not distinguish.",
                    systemVentilationRoomIntent.Guid_Space,
                    value_FlowRate,
                    value_FreshAir));

                return false;
            }

            if (!systemVentilationRoomIntent.DesignFlowRate_Supply_Lps.HasValue)
            {
                if (!Query.Equivalent(value_FlowRate, 0.0))
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Room {0} has no supply leg and its native zone holds {1} l/s.",
                        systemVentilationRoomIntent.Guid_Space,
                        value_FlowRate));

                    return false;
                }

                return true;
            }

            designFlowRate_Supply_Lps = value_FlowRate;

            return true;
        }
    }
}
