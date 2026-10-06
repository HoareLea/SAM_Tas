// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Managed stand-ins for the four TBD COM objects an internal-condition round trip touches.
    /// <para>
    /// <c>Interop.TBD</c> is an ordinary managed interop assembly and it is referenced here with
    /// <c>EmbedInteropTypes=false</c>, exactly as <c>SAM.Analytical.Tas</c> references it - so
    /// <c>TBD.profile</c> and friends are plain interfaces a plain C# class can implement. Nothing below
    /// instantiates a coclass, so no TAS licence, no TAS install and no COM server is involved: these
    /// fakes are what let the REAL <c>Convert.ToSAM</c> and <c>Modify.UpdateInternalCondition</c> run
    /// inside a unit test instead of being mirrored by hand.
    /// </para>
    /// <para>
    /// The only members that need explaining are <c>hourlyValues</c>/<c>yearlyValues</c>: they are COM
    /// parameterised properties, which C# cannot declare. C# surfaces them as their accessor methods
    /// instead, so they are implemented as <c>get_hourlyValues(int)</c> / <c>set_hourlyValues(int, float)</c>.
    /// Both are 1-BASED, matching TAS - <c>Modify.Update</c> writes <c>hourlyValues[i + 1]</c>.
    /// </para>
    /// <para>
    /// The bulk yearly pair behaves as licensed TAS was <b>measured</b> to (2026-09-10 <c>yearly</c> probe,
    /// re-confirmed through the production writer 2026-09-11): <c>SetYearlyValues</c> ignores element 0,
    /// stores element <c>i</c> in slot <c>i</c> and repeats the last element into any slot the array does not
    /// reach; <c>GetYearlyValues</c> answers a <c>Single[*]</c> bounded 1..8760. The previous fake copied from
    /// element 0 into a 0-based array, which is the one convention TAS does not have - it made a writer that
    /// shifts every hour look exact.
    /// </para>
    /// </summary>
    internal class FakeProfile : TBD.profile
    {
        private const int HoursPerYear = 8760;

        // 1-based, 25 slots so index 24 is addressable.
        private readonly float[] hourly = new float[25];

        // 1-based, 8761 slots so index 8760 is addressable. Slot 0 is never read.
        private readonly float[] yearly = new float[HoursPerYear + 1];

        public float factor { get; set; }
        public float value { get; set; }
        public TBD.ProfileTypes type { get; set; }
        public float setbackValue { get; set; }
        public TBD.Profiles profile { get; set; }
        public int useDaylightAdjustment { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string function { get; set; }
        public string units { get; set; }
        public TBD.schedule schedule { get; set; }

        public float get_hourlyValues(int index)
        {
            return hourly[index];
        }

        public void set_hourlyValues(int index, float value)
        {
            hourly[index] = value;
        }

        public float get_yearlyValues(int index)
        {
            return yearly[index];
        }

        public void set_yearlyValues(int index, float value)
        {
            yearly[index] = value;
        }

        public object GetYearlyValues()
        {
            System.Array result = System.Array.CreateInstance(typeof(float), new[] { HoursPerYear }, new[] { 1 });
            for (int i = 1; i <= HoursPerYear; i++)
            {
                result.SetValue(yearly[i], i);
            }

            return result;
        }

        public void SetYearlyValues(object values)
        {
            float[] values_Float = values as float[];
            if (values_Float == null || values_Float.Length == 0)
            {
                return;
            }

            for (int i = 1; i <= HoursPerYear; i++)
            {
                yearly[i] = i < values_Float.Length ? values_Float[i] : values_Float[values_Float.Length - 1];
            }
        }

        /// <summary>
        /// TAS's own definition, and the whole point of this fixture: the extreme is the factor times the
        /// extreme of the VALUES - the peak of the effective curve, not the authored magnitude.
        /// </summary>
        public float GetExtremeValue(bool maximum)
        {
            List<double> values = Values();
            if (values.Count == 0)
            {
                return 0;
            }

            double extreme = values[0];
            foreach (double value_Temp in values)
            {
                if (maximum ? value_Temp > extreme : value_Temp < extreme)
                {
                    extreme = value_Temp;
                }
            }

            return factor * (float)extreme;
        }

        /// <summary>The raw schedule values this profile carries, by its own type. Test-side helper.</summary>
        public List<double> Values()
        {
            List<double> result = new List<double>();
            switch (type)
            {
                case TBD.ProfileTypes.ticValueProfile:
                    result.Add(value);
                    break;

                case TBD.ProfileTypes.ticHourlyProfile:
                    for (int i = 1; i <= 24; i++)
                    {
                        result.Add(hourly[i]);
                    }
                    break;

                case TBD.ProfileTypes.ticYearlyProfile:
                    for (int i = 1; i <= HoursPerYear; i++)
                    {
                        result.Add(yearly[i]);
                    }
                    break;
            }

            return result;
        }

        /// <summary>The effective curve TAS simulates: magnitude times schedule, hour by hour.</summary>
        public List<double> EffectiveValues()
        {
            List<double> result = new List<double>();
            foreach (double value_Temp in Values())
            {
                result.Add(factor * value_Temp);
            }

            return result;
        }
    }

    internal class FakeInternalGain : TBD.InternalGain
    {
        private readonly Dictionary<int, FakeProfile> profiles = new Dictionary<int, FakeProfile>();

        public string name { get; set; }
        public string description { get; set; }
        public float lightingRadProp { get; set; }
        public float occupantRadProp { get; set; }
        public float equipmentRadProp { get; set; }
        public float lightingViewCoefficient { get; set; }
        public float occupantViewCoefficient { get; set; }
        public float equipmentViewCoefficient { get; set; }
        public float personGain { get; set; }
        public float freshAirRate { get; set; }
        public float domesticHotWater { get; set; }
        public float targetIlluminance { get; set; }
        public int activityID { get; set; }

        /// <summary>
        /// Only the slots a test asks for exist, mirroring a TBD where a gain slot can be absent - and
        /// keeping every test focused on the one slot it is about.
        /// </summary>
        public FakeProfile Enable(TBD.Profiles slot)
        {
            FakeProfile result;
            if (!profiles.TryGetValue((int)slot, out result))
            {
                result = new FakeProfile { profile = slot };
                profiles[(int)slot] = result;
            }

            return result;
        }

        public FakeProfile Get(TBD.Profiles slot)
        {
            FakeProfile result;
            return profiles.TryGetValue((int)slot, out result) ? result : null;
        }

        public TBD.profile GetProfile(int profile)
        {
            return Get((TBD.Profiles)profile);
        }
    }

    internal class FakeThermostat : TBD.Thermostat
    {
        private readonly Dictionary<int, FakeProfile> profiles = new Dictionary<int, FakeProfile>();

        public string name { get; set; }
        public string description { get; set; }
        public int proportionalControl { get; set; }
        public float controlRange { get; set; }
        public float radiantProportion { get; set; }

        public FakeProfile Enable(TBD.Profiles slot)
        {
            FakeProfile result;
            if (!profiles.TryGetValue((int)slot, out result))
            {
                result = new FakeProfile { profile = slot };
                profiles[(int)slot] = result;
            }

            return result;
        }

        public TBD.profile GetProfile(int profile)
        {
            FakeProfile result;
            return profiles.TryGetValue(profile, out result) ? result : null;
        }

        public float GetMinimumDeadBand()
        {
            return 0;
        }
    }

    internal class FakeEmitter : TBD.Emitter
    {
        public string name { get; set; }
        public string description { get; set; }
        public TBD.EmitterTypes emitterType { get; set; }
        public int airCon { get; set; }
        public float radiantProportion { get; set; }
        public float viewCoefficient { get; set; }
        public float offOutsideTemp { get; set; }
        public float maxOutsideTemp { get; set; }
        public float designDeltaT { get; set; }

        public TBD.profile GetProfile()
        {
            return null;
        }
    }

    internal class FakeInternalCondition : TBD.InternalCondition
    {
        public FakeInternalGain InternalGain = new FakeInternalGain();
        public FakeThermostat Thermostat = new FakeThermostat();
        public FakeEmitter HeatingEmitter = new FakeEmitter();
        public FakeEmitter CoolingEmitter = new FakeEmitter();

        public string name { get; set; }
        public string description { get; set; }
        public int includeSolarInMRT { get; set; }

        public float GetUpperLimit()
        {
            return 0;
        }

        public float GetLowerLimit()
        {
            return 0;
        }

        public TBD.InternalGain GetInternalGain()
        {
            return InternalGain;
        }

        public TBD.Thermostat GetThermostat()
        {
            return Thermostat;
        }

        public TBD.dayType GetDayType(int index)
        {
            return null;
        }

        public TBD.Emitter GetCoolingEmitter()
        {
            return CoolingEmitter;
        }

        public TBD.Emitter GetHeatingEmitter()
        {
            return HeatingEmitter;
        }

        public int SetDayType(TBD.dayType dayType, bool bAdd)
        {
            return 0;
        }

        public TBD.zone GetZone(int index)
        {
            return null;
        }

        public string CalculateDataChecksum()
        {
            return null;
        }
    }
}
