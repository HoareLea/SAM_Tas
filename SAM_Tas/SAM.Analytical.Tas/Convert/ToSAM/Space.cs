// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Space ToSAM(this TAS3D.Zone zone)
        {
            if (zone == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, zone);

            Space space = new Space(zone.name, null);
            space.Add(parameterSet);

            return space;
        }

        public static Space ToSAM(this TSD.ZoneData zoneData, IEnumerable<SpaceDataType> spaceDataTypes = null)
        {
            return ToSAM(zoneData, spaceDataTypes, null);
        }

        /// <summary>
        /// The space for one TSD zone, with its hourly series taken from <paramref name="zoneResultSeries"/> - this
        /// zone's entry of a <see cref="Query.ZoneResultSeries"/> read over days 1..365 - where it holds the series,
        /// and from <c>GetAnnualZoneResult</c> otherwise. The two are the same values: a float widened to double,
        /// 8760 of them.
        /// </summary>
        internal static Space ToSAM(this TSD.ZoneData zoneData, IEnumerable<SpaceDataType> spaceDataTypes, Dictionary<TSD.tsdZoneArray, float[]> zoneResultSeries)
        {
            ParameterSet parameterSet = Create.ParameterSet_Space(ActiveSetting.Setting, zoneData);

            if(spaceDataTypes != null)
            {
                foreach(SpaceDataType spaceDataType in spaceDataTypes)
                {
                    List<double> values = null;

                    TSD.tsdZoneArray? tsdZoneArray = spaceDataType.TsdZoneArray();
                    if (zoneResultSeries != null && tsdZoneArray != null && zoneResultSeries.TryGetValue(tsdZoneArray.Value, out float[] series))
                    {
                        values = new List<double>(series.Length);
                        foreach (float value in series)
                        {
                            values.Add(value);
                        }
                    }
                    else
                    {
                        values = zoneData.AnnualZoneResult<double>(spaceDataType);
                    }

                    if (values == null)
                        continue;

                    JsonArray jArray = new JsonArray();
                    values.ForEach(x => jArray.Add(x));

                    parameterSet.Add(spaceDataType.Text(), jArray);
                }
            }

            Space space = new Space(zoneData.name, null);
            space.Add(parameterSet);

            // Stamp the zone identity explicitly, as the TBD conversion below does. TAS retains the TBD zone's
            // guid on the zone in the TSD, so this is the same identity the design side carries - the
            // surface-result attachment in Modify.AddResults already relies on that equality.
            // The generic Create.ParameterSet_Space above cannot supply it: the TypeMap entry is registered,
            // but the mapper reads the source property by the SAM-side parameter name ("Zone Guid") rather
            // than the TAS-side one ("zoneGUID"), so the value is silently never carried across.
            // Blank stays blank - a space with no stated identity must fall back to the unique-name rule in
            // SimulationSpaceMap rather than state an empty key, which that class refuses instead of matching.
            if (!string.IsNullOrWhiteSpace(zoneData.zoneGUID))
            {
                space.SetValue(SpaceParameter.ZoneGuid, zoneData.zoneGUID);
            }

            return space;
        }

        /// <summary>
        /// Backwards-compatible overload - forwards to the <c>ProfileReuseIndex</c> overload with no index, so
        /// callers compiled against the previous <c>ToSAM(TBD.zone, out List&lt;InternalCondition&gt;)</c>
        /// signature keep working. An added optional parameter changes that signature's arity and breaks them
        /// at runtime.
        /// </summary>
        public static Space ToSAM(this TBD.zone zone, out List<InternalCondition> internalConditions)
        {
            return ToSAM(zone, out internalConditions, null);
        }

        /// <summary>
        /// Import one TBD zone as a SAM <see cref="Space"/>, together with the internal conditions assigned to it.
        /// </summary>
        /// <param name="zone">The TBD zone to import.</param>
        /// <param name="internalConditions">The zone's internal conditions, imported alongside the space.</param>
        /// <param name="profileReuseIndex">
        /// The conversion-wide profile reuse index, or null for today's per-zone profile naming. Threaded
        /// straight through to <see cref="Convert.ToSAM(TBD.InternalCondition, double, ProfileReuseIndex)"/> -
        /// the zone itself takes no part in profile identity.
        /// </param>
        public static Space ToSAM(this TBD.zone zone, out List<InternalCondition> internalConditions, ProfileReuseIndex profileReuseIndex)
        {
            internalConditions = null;

            if(zone == null)
            {
                return null;
            }

            Space result = new Space(zone.name);

            double area = zone.floorArea;

            result.SetValue(Analytical.SpaceParameter.Area, area);
            result.SetValue(Analytical.SpaceParameter.Volume, zone.volume);
            result.SetValue(SpaceParameter.ZoneGuid, zone.GUID);

            // Round-trip the TBD zone colour (export writes it back from SpaceParameter.Color).
            result.SetValue(Analytical.SpaceParameter.Color, new SAMColor(zone.colour.ToColor()));

            List<TBD.InternalCondition> internalConditions_TBD = zone.InternalConditions();
            if(internalConditions_TBD != null)
            {
                internalConditions = new List<InternalCondition>();

                //Kept in step with internalConditions so the zone's own condition can be paired back to the
                //TBD one the metadata fingerprint is compared against.
                List<TBD.InternalCondition> internalConditions_TBD_Imported = new List<TBD.InternalCondition>();

                foreach(TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
                {
                    InternalCondition internalCondition = internalCondition_TBD.ToSAM(area, profileReuseIndex);
                    if(internalCondition == null)
                    {
                        continue;
                    }

                    internalConditions.Add(internalCondition);
                    internalConditions_TBD_Imported.Add(internalCondition_TBD);
                }

                RestoreVentilationRequirement(result, zone, internalConditions, internalConditions_TBD_Imported);
            }

            return result;
        }

        /// <summary>
        /// The SAM name for the diagnostic a refused zone-metadata section leaves on the space. Present ONLY
        /// when a section was found and rejected - a normal import, and a TAS-authored model with no section
        /// at all, stamp nothing.
        /// </summary>
        public const string SpaceParameter_VentilationMetadataNote = "SAM Zone Metadata Note";

        /// <summary>
        /// Replaces what the native import inferred about ventilation with the airflow REQUIREMENT the export
        /// recorded in the zone description, where there is one and it still matches what TAS states.
        /// <para>
        /// This is the whole COM part of the mechanism: read the description, read the two native fields off
        /// the zone's own internal condition, and hand both to the COM-free
        /// <see cref="Modify.RestoreVentilationRequirement(InternalCondition, SAMZoneMetadata, double, double, out string)"/>,
        /// which decides and applies. A refusal is stamped on the space so a stale file is visible in the
        /// model rather than only in a debugger.
        /// </para>
        /// </summary>
        private static void RestoreVentilationRequirement(Space space, TBD.zone zone, List<InternalCondition> internalConditions, List<TBD.InternalCondition> internalConditions_TBD)
        {
            SAMZoneMetadata metadata = SAMZoneMetadata.Parse(zone?.description);
            if (metadata == null)
            {
                return;
            }

            int index = Query.PrimaryInternalConditionIndex(internalConditions);
            if (index < 0 || index >= internalConditions_TBD.Count)
            {
                return;
            }

            TBD.InternalGain internalGain = internalConditions_TBD[index]?.GetInternalGain();

            double freshAirRate = double.NaN;
            double ventilationFactor = double.NaN;
            if (internalGain != null)
            {
                freshAirRate = internalGain.freshAirRate;

                TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticV);
                if (profile_TBD != null)
                {
                    ventilationFactor = profile_TBD.factor;
                }
            }

            Modify.RestoreVentilationRequirement(internalConditions[index], metadata, freshAirRate, ventilationFactor, out string note);

            if (!string.IsNullOrEmpty(note))
            {
                space?.SetValue(SpaceParameter_VentilationMetadataNote, note);
            }
        }
    }
}
