// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Managed stand-ins for the TSD result interfaces, so <c>Convert.ToSAM_Results(SimulationData)</c> - the real
    /// production conversion - runs in <c>dotnet test</c> without TAS. They answer the way TAS does where the
    /// conversion depends on it (probed on C:\TasOut\final1b\open.tsd):
    /// <list type="bullet">
    /// <item>hourly indices are 1-based; index 0 (and any hour outside the data) returns TAS's -1 sentinel;</item>
    /// <item><c>GetPeakZoneGains</c> returns a [3, zones] array, row 1 the peak and row 2 its 1-based hour; a
    /// zone with no positive value reports 0 at index 0;</item>
    /// <item>zones and design data sets are numbered from 1 and 0 respectively, and a missing one is null.</item>
    /// </list>
    /// </summary>
    internal sealed class FakeZoneData : ZoneData
    {
        private readonly Dictionary<(int, int), float> values = new();
        private readonly int firstHour;
        private readonly int lastHour;

        public FakeZoneData(string name, string zoneGuid, int zoneNumber, int firstHour = 1, int lastHour = 8760)
        {
            this.name = name;
            zoneGUID = zoneGuid;
            this.zoneNumber = zoneNumber;
            this.firstHour = firstHour;
            this.lastHour = lastHour;
            floorArea = 4.5f;
            volume = 11.25f;
        }

        public FakeZoneData Set(int hour, tsdZoneArray tsdZoneArray, float value)
        {
            values[(hour, (int)tsdZoneArray)] = value;
            return this;
        }

        public int zoneNumber { get; set; }
        public int nConvWeightingFactors { get; set; }
        public int nRadWeightingFactors { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string zoneGUID { get; set; }
        public float volume { get; set; }
        public float floorArea { get; set; }
        public float convectiveCommonRatio { get; set; }
        public float radiantCommonRatio { get; set; }

        public float GetHourlyZoneResult(int hour, int param)
        {
            if (hour < firstHour || hour > lastHour)
            {
                return -1;
            }

            return values.TryGetValue((hour, param), out float value) ? value : 0;
        }

        public object GetDailyZoneResult(int day, int param)
        {
            float[] result = new float[24];
            for (int i = 0; i < 24; i++)
            {
                result[i] = GetHourlyZoneResult(((day - 1) * 24) + i + 1, param);
            }

            return result;
        }

        /// <summary>The peak as TAS reports it: the largest positive value and its 1-based hour; 0 at 0 if none.</summary>
        public (float, int) Peak(int param)
        {
            float peak = 0;
            int index = 0;
            foreach (KeyValuePair<(int, int), float> keyValuePair in values)
            {
                if (keyValuePair.Key.Item2 == param && keyValuePair.Value > peak)
                {
                    peak = keyValuePair.Value;
                    index = keyValuePair.Key.Item1;
                }
            }

            return (peak, index);
        }

        /// <summary>How many times <c>GetAnnualZoneResult</c> was asked - a day-major reader asks it never.</summary>
        public int AnnualReads { get; set; }

        /// <summary>
        /// The whole-year series as TSD's annual getter returns it: ALWAYS 8760 values, -1 outside the simulated hours
        /// (measured, TSD 2.0.0.1).
        /// </summary>
        public object GetAnnualZoneResult(int param)
        {
            AnnualReads++;

            float[] result = new float[8760];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GetHourlyZoneResult(i + 1, param);
            }

            return result;
        }

        public SurfaceData GetSurfaceData(int index) => null;
        public object GetConvWeightingFactors() => throw new NotSupportedException();
        public object GetRadWeightingFactors() => throw new NotSupportedException();
        public float GetPeakZoneGain(object arrFromVals) => throw new NotSupportedException();
        public IZAMData GetIZAMInData(int index) => null;
        public IZAMData GetIZAMOutData(int index) => null;
        public object GetAnnualSumZoneResult(tsdZoneArray param, int startDay, int endDay, tsdResultsPeriod period, bool reportPerFloorArea) => throw new NotSupportedException();
        public object GetAnnualPeakZoneResult(tsdZoneArray param, int startDay, int endDay, tsdResultsPeriod period, bool reportPerFloorArea) => throw new NotSupportedException();
    }

    internal static class FakeTsd
    {
        public static object PeakZoneGains(IList<FakeZoneData> zoneDatas, object arrFromVals)
        {
            int param = ((short[])arrFromVals)[0];
            object[,] result = new object[3, zoneDatas.Count];
            for (int i = 0; i < zoneDatas.Count; i++)
            {
                (float peak, int index) = zoneDatas[i].Peak(param);
                result[0, i] = zoneDatas[i].zoneGUID;
                result[1, i] = peak;
                result[2, i] = index;
            }

            return result;
        }

        public static ZoneData Zone(IList<FakeZoneData> zoneDatas, int index)
        {
            return index >= 1 && index <= zoneDatas.Count ? zoneDatas[index - 1] : null;
        }
    }

    internal sealed class FakeBuildingData : BuildingData
    {
        private readonly Dictionary<(int, int), float> values = new();

        public List<FakeZoneData> Zones { get; } = new();

        public FakeBuildingData Set(int hour, tsdBuildingArray tsdBuildingArray, float value)
        {
            values[(hour, (int)tsdBuildingArray)] = value;
            return this;
        }

        public string name { get; set; } = "Building";
        public string description { get; set; }
        public int zoneCount { get => Zones.Count; set { } }
        public string GUID { get; set; }

        public ZoneData GetZoneData(int index) => FakeTsd.Zone(Zones, index);
        public object GetPeakZoneGains(object arrFromVals) => FakeTsd.PeakZoneGains(Zones, arrFromVals);

        /// <summary>Last simulated hour, 1-based; later hours answer TSD's -1 padding.</summary>
        public int LastHour { get; set; } = 8760;

        /// <summary>How many times <c>GetAnnualBuildingResult</c> was asked - a day-major reader asks it never.</summary>
        public int AnnualReads { get; set; }

        /// <summary>Every <c>GetDailyBuildingResult</c> call, in order.</summary>
        public List<(int day, int param)> DailyReads { get; } = new();

        public float GetHourlyBuildingResult(int hour, int param)
        {
            if (hour < 1 || hour > LastHour)
            {
                return -1;
            }

            return values.TryGetValue((hour, param), out float value) ? value : 0;
        }

        /// <summary>ALWAYS 8760 values, -1 past the simulated hours (measured, TSD 2.0.0.1).</summary>
        public object GetAnnualBuildingResult(int param)
        {
            AnnualReads++;

            float[] result = new float[8760];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GetHourlyBuildingResult(i + 1, param);
            }

            return result;
        }

        public object GetDailyBuildingResult(int day, int param)
        {
            DailyReads.Add((day, param));

            float[] result = new float[24];
            for (int i = 0; i < 24; i++)
            {
                result[i] = GetHourlyBuildingResult(((day - 1) * 24) + i + 1, param);
            }

            return result;
        }

        public float GetPeakZoneGroupGain(object arrZoneGUIDs, object arrFromVals) => throw new NotSupportedException();
        public object GetPeakZoneGroupGains(object arrZoneGUIDs, object arrFromVals) => throw new NotSupportedException();
        public object GetSumZoneResultForMultipleZones(object arrFromVals, tsdZoneArray param, int startDay, int endDay, tsdResultsPeriod period, bool reportPerFloorArea) => throw new NotSupportedException();
        public ZoneData GetZoneDataByName(string zoneName) => Zones.Find(x => x.name == zoneName);
    }

    internal sealed class FakeHeatingDesignData : HeatingDesignData
    {
        public List<FakeZoneData> Zones { get; } = new();

        public string name { get; set; }
        public string description { get; set; }
        public int firstDay { get; set; }
        public int lastDay { get; set; }
        public string GUID { get; set; }

        public ZoneData GetZoneData(int index) => FakeTsd.Zone(Zones, index);
        public object GetPeakZoneGains(object arrFromVals) => FakeTsd.PeakZoneGains(Zones, arrFromVals);
        public float GetPeakZoneGroupGain(object arrZoneGUIDs, object arrFromVals) => throw new NotSupportedException();
        public object GetPeakZoneGroupGains(object arrZoneGUIDs, object arrFromVals) => throw new NotSupportedException();
        public ZoneData GetZoneDataByName(string zoneName) => Zones.Find(x => x.name == zoneName);
    }

    internal sealed class FakeCoolingDesignData : CoolingDesignData
    {
        public List<FakeZoneData> Zones { get; } = new();

        public string name { get; set; }
        public string description { get; set; }
        public int firstDay { get; set; }
        public int lastDay { get; set; }
        public string GUID { get; set; }

        public ZoneData GetZoneData(int index) => FakeTsd.Zone(Zones, index);
        public object GetPeakZoneGains(object arrFromVals) => FakeTsd.PeakZoneGains(Zones, arrFromVals);
        public float GetPeakZoneGroupGain(object arrZoneGUIDs, object arrFromVals) => throw new NotSupportedException();
        public object GetPeakZoneGroupGains(object arrZoneGUIDs, object arrFromVals) => throw new NotSupportedException();
        public ZoneData GetZoneDataByName(string zoneName) => Zones.Find(x => x.name == zoneName);
    }

    internal sealed class FakeSimulationData : SimulationData
    {
        public FakeBuildingData Building { get; } = new();
        public List<FakeHeatingDesignData> HeatingDesignDatas { get; } = new();
        public List<FakeCoolingDesignData> CoolingDesignDatas { get; } = new();

        public string buildingPath { get; set; }
        public int firstDay { get; set; } = 1;
        public int lastDay { get; set; } = 365;
        public int zoneGroupCount { get; set; }

        public BuildingData GetBuildingData() => Building;
        public HeatingDesignData GetHeatingDesignData(int index) => index >= 0 && index < HeatingDesignDatas.Count ? HeatingDesignDatas[index] : null;
        public CoolingDesignData GetCoolingDesignData(int index) => index >= 0 && index < CoolingDesignDatas.Count ? CoolingDesignDatas[index] : null;
        public ZoneDataGroup GetZoneDataGroup(int index) => null;
        public ZoneDataGroup AddZoneDataGroup() => throw new NotSupportedException();
        public void RemoveZoneDataGroup(short index) => throw new NotSupportedException();
    }
}
