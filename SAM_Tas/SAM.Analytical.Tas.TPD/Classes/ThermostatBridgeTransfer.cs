// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// One room's hourly transfer into the thermostat bridge: the achieved <c>ZoneTemperature</c> series the
    /// TAS Systems simulation returned for the room, and the TBD zone it is to be written to - by guid.
    /// <para>
    /// <b>Both representations are kept.</b> TAS stores a thermostat profile value single precision, so
    /// what the TBD copy can hold is <c>(float)ZoneTemperature[h]</c>. The double is kept alongside so the
    /// transfer delta - read back from TAS against the value the Systems simulation actually answered - is
    /// measured rather than assumed, and <see cref="MaxRepresentationDelta"/> states in advance how large the
    /// single-precision rounding alone can make it.
    /// </para>
    /// <para>
    /// <b>Hours are 0-based and absolute.</b> Index <c>h</c> is hour <c>h</c> of the year, the same hour the
    /// Systems result is keyed by. TAS's yearly profile slot for it is <c>h + 1</c> - measured on licensed
    /// TAS, see <see cref="Modify.WriteThermostatBridge(TBD.profile, ThermostatBridgeTransfer, out int, out double)"/>.
    /// </para>
    /// <para>Free of TAS COM types.</para>
    /// </summary>
    public class ThermostatBridgeTransfer
    {
        private readonly double[] zoneTemperatures;
        private readonly float[] values;

        /// <param name="guid_Space">The analytical room.</param>
        /// <param name="reference_Zone">The TAS building-zone guid the no-IZAM source states for the room.</param>
        /// <param name="startHour">0-based hour of the first value.</param>
        /// <param name="zoneTemperatures">The achieved room-air temperatures, one per hour, already validated finite.</param>
        public ThermostatBridgeTransfer(Guid guid_Space, string reference_Zone, int startHour, IReadOnlyList<double> zoneTemperatures)
        {
            Guid_Space = guid_Space;
            Reference_Zone = reference_Zone;
            Key = Query.ZoneReferenceKey(reference_Zone);
            StartHour = startHour;

            int count = zoneTemperatures == null ? 0 : zoneTemperatures.Count;

            this.zoneTemperatures = new double[count];
            values = new float[count];

            double maxRepresentationDelta = 0;

            for (int i = 0; i < count; i++)
            {
                double zoneTemperature = zoneTemperatures[i];

                this.zoneTemperatures[i] = zoneTemperature;
                values[i] = (float)zoneTemperature;

                double delta = global::System.Math.Abs(zoneTemperature - values[i]);
                if (delta > maxRepresentationDelta)
                {
                    maxRepresentationDelta = delta;
                }
            }

            MaxRepresentationDelta = maxRepresentationDelta;
        }

        /// <summary>The analytical room.</summary>
        public Guid Guid_Space { get; }

        /// <summary>The TAS building-zone guid, as the no-IZAM source stated it.</summary>
        public string Reference_Zone { get; }

        /// <summary><see cref="Reference_Zone"/> in its canonical form - see <see cref="Query.ZoneReferenceKey"/>.</summary>
        public string Key { get; }

        /// <summary>0-based hour of the first value.</summary>
        public int StartHour { get; }

        /// <summary>How many hourly values the transfer carries.</summary>
        public int Count
        {
            get { return values.Length; }
        }

        /// <summary>
        /// The largest difference single-precision storage alone introduces between a Systems value and the
        /// value a TAS thermostat profile can hold. Zero when every Systems value is itself a single.
        /// </summary>
        public double MaxRepresentationDelta { get; }

        /// <summary>The achieved room-air temperature the Systems simulation answered for hour <c>StartHour + index</c>.</summary>
        public double ZoneTemperature(int index)
        {
            return zoneTemperatures[index];
        }

        /// <summary>The single-precision value written to both thermostat limits for hour <c>StartHour + index</c>.</summary>
        public float Value(int index)
        {
            return values[index];
        }

        public override string ToString()
        {
            return string.Format("Room {0} -> TBD zone {1}: {2} hourly value(s) from hour {3}", Guid_Space, Reference_Zone ?? "<none>", Count, StartHour);
        }
    }
}
