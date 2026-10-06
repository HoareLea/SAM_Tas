// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateACCI(this TBD.Building building, IEnumerable<string> spaceNames)
        {
            if (building == null)
            {
                return false;
            }

            List<TBD.InternalCondition> internalConditions = building.InternalConditions();
            if(internalConditions == null || internalConditions.Count == 0)
            {
                return false;
            }


            List<double> dryBulbTemperatures = Weather.Tas.Query.AnnualParameter<double>(building.GetWeatherYear(), Weather.WeatherDataType.DryBulbTemperature);
            if(dryBulbTemperatures == null || dryBulbTemperatures.Count != 8760)
            {
                return false;
            }

            // Pre-compute the upper/lower-limit arrays for the entire year ONCE outside the
            // internal-condition loop. The mapping is `DryBulbTemperature → Range<double>`, which
            // depends only on weather, not on the IC. Previously each matching IC re-ran the
            // mapping 8760 times AND allocated a `Range<double>` per hour. With ~14 matching ICs
            // that was ~245k function calls and ~245k allocations for values that are constant
            // across ICs.
            float[] upperLimitYear = new float[8760];
            float[] lowerLimitYear = new float[8760];
            bool[] hourHasRange = new bool[8760];
            bool allHoursHaveRange = true;
            bool anyHourHasRange = false;
            for (int h = 0; h < 8760; h++)
            {
                Range<double> range = Weather.Query.DryBulbTemperatureRange(dryBulbTemperatures[h]);
                if (range == null)
                {
                    allHoursHaveRange = false;
                    continue;
                }

                upperLimitYear[h] = System.Convert.ToSingle(range.Max);
                lowerLimitYear[h] = System.Convert.ToSingle(range.Min);
                hourHasRange[h] = true;
                anyHourHasRange = true;
            }

            // Preserve the original "return false when no hours were writable" contract: the
            // previous per-hour loop set `result = true` only inside the if-range-not-null branch,
            // so a year with every DryBulbTemperatureRange null returned false and the path-based
            // overload skipped Save(). Early-exit here keeps that semantics — even if every IC
            // passes the filter and profile guards downstream, there is nothing to write.
            if (!anyHourHasRange)
            {
                return false;
            }

            // O(1) lookup for the per-IC zone-name filter (was `IEnumerable.Contains` per zone).
            HashSet<string> spaceNameSet = (spaceNames != null && spaceNames.Any())
                ? new HashSet<string>(spaceNames)
                : null;

            bool result = false;
            foreach (TBD.InternalCondition internalCondition in internalConditions)
            {
                if(internalCondition.name.EndsWith(" - HDD") || internalCondition.name.EndsWith(" - CDD") || internalCondition.name.EndsWith("New Internal Condition") )
                {
                    continue;
                }

                // BEHAVIOUR-CHANGE (from earlier Stage 2 commit): the original predicate was
                // `spaceNames != null && spaceNames.Count() == 0`, which only ran the filter
                // when the supplied list was EMPTY — meaning callers passing a populated list
                // got NO filtering and callers passing an empty list got everything skipped.
                // The intent (matching the method's parameter name and the sibling overloads)
                // is "apply the filter when names were supplied". Flipped to `.Any()` and now
                // backed by a HashSet for O(1) zone-name membership tests.
                if (spaceNameSet != null)
                {
                    bool contains = false;
                    List<TBD.zone> zones = internalCondition.Zones();
                    if (zones != null)
                    {
                        foreach (TBD.zone zone in zones)
                        {
                            string zoneName = zone?.name;
                            if (zoneName != null && spaceNameSet.Contains(zoneName))
                            {
                                contains = true;
                                break;
                            }
                        }
                    }

                    if (!contains)
                    {
                        continue;
                    }
                }

                TBD.profile profile_UpperLimit = internalCondition.GetThermostat().GetProfile((int)TBD.Profiles.ticUL);
                if (profile_UpperLimit == null)
                {
                    continue;
                }

                TBD.profile profile_LowerLimit = internalCondition.GetThermostat().GetProfile((int)TBD.Profiles.ticLL);
                if (profile_LowerLimit == null)
                {
                    continue;
                }

                profile_UpperLimit.type = TBD.ProfileTypes.ticYearlyProfile;
                profile_LowerLimit.type = TBD.ProfileTypes.ticYearlyProfile;

                // HOT PATH FIX: the previous per-hour loop did 8760 COM-property assignments
                // PER PROFILE (×2 profiles per IC). Each `profile.yearlyValues[i] = …` is a
                // marshalled COM round-trip. For ~14 matching ICs that's ~245k COM calls and
                // dominated runtime (user-observed ~60 s for 14 spaces).
                //
                // `TBD.profileClass` exposes a bulk `SetYearlyValues(Object)` method that takes
                // a `float[]` in a SINGLE COM call. It goes through Modify.UpdateYearlyValues,
                // which owns the slot base: TAS ignores element 0 of that array, so passing these
                // 0-based hour arrays straight in wrote every limit one hour early (licensed
                // measurement, 2026-09-11: before the fix every slot matched the weather range of
                // the NEXT hour; after it, every slot matches its own hour). 2 COM round-trips per
                // IC (or 4 in the rare splice path).
                if (allHoursHaveRange)
                {
                    // Fast path: every weather hour mapped to a valid range. Write directly.
                    profile_UpperLimit.UpdateYearlyValues(upperLimitYear);
                    profile_LowerLimit.UpdateYearlyValues(lowerLimitYear);
                }
                else
                {
                    // Sparse path: some hours had no range (DryBulbTemperatureRange returned null).
                    // Preserve the previous per-hour `continue` semantics by reading the existing
                    // array, splicing only the valid hours, and writing back. ToFloatArray answers
                    // 0-based hours (slot k + 1 -> hour k), the same base UpdateYearlyValues takes.
                    float[] upperArray = ToFloatArray(profile_UpperLimit.GetYearlyValues());
                    float[] lowerArray = ToFloatArray(profile_LowerLimit.GetYearlyValues());
                    for (int h = 0; h < 8760; h++)
                    {
                        if (!hourHasRange[h])
                        {
                            continue;
                        }

                        upperArray[h] = upperLimitYear[h];
                        lowerArray[h] = lowerLimitYear[h];
                    }
                    profile_UpperLimit.UpdateYearlyValues(upperArray);
                    profile_LowerLimit.UpdateYearlyValues(lowerArray);
                }

                result = true;
            }

            return result;
        }

        // Coerce the boxed SAFEARRAY returned by TBD.profile.GetYearlyValues() into a 0-based
        // float[] of hours. Licensed TAS answers a Single[*] bounded 1..8760 (not a float[]), so
        // the bound-aware copy below is the path that actually runs: slot k + 1 -> hour k.
        private static float[] ToFloatArray(object yearlyValues)
        {
            if (yearlyValues == null)
            {
                return new float[8760];
            }

            // Fast path: marshalled to a zero-based float[]. The vast majority of TBD interop
            // calls produce exactly this.
            if (yearlyValues is float[] floats && floats.Length >= 8760)
            {
                return floats;
            }

            // Defensive fallback: a System.Array projection of a SAFEARRAY can have a non-zero
            // lower bound (e.g. [1..8760] is valid in COM and shows up for some TAS APIs). Use
            // GetLowerBound / GetUpperBound and copy element-by-element so this path can't
            // throw IndexOutOfRangeException on a 1-based SAFEARRAY.
            float[] result = new float[8760];
            if (yearlyValues is System.Array array)
            {
                int lowerBound = array.GetLowerBound(0);
                int upperBound = array.GetUpperBound(0);
                int sourceLength = upperBound - lowerBound + 1;
                int copyLength = global::System.Math.Min(sourceLength, 8760);
                for (int i = 0; i < copyLength; i++)
                {
                    result[i] = global::System.Convert.ToSingle(array.GetValue(lowerBound + i));
                }
            }
            return result;
        }

        public static bool UpdateACCI(this SAMTBDDocument sAMTBDDocument, IEnumerable<string> spaceNames)
        {
            if (sAMTBDDocument == null)
                return false;

            return UpdateACCI(sAMTBDDocument.TBDDocument?.Building, spaceNames);
        }

        public static bool UpdateACCI(string path_TBD, IEnumerable<string> spaceNames)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
            {
                return false;
            }

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateACCI(sAMTBDDocument, spaceNames);
                if (result)
                {
                    sAMTBDDocument.Save();
                }
            }

            return result;
        }
    }
}