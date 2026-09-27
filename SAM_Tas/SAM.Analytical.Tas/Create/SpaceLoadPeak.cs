// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        /// <summary>
        /// The TSD channels read as heat-balance terms at a peak. Each is a zone result Tas reports hourly, signed
        /// as Tas signs it (+ gain to the room air). Aperture flows and IZAM channels are deliberately not here:
        /// Tas reports them as -1 at valid timesteps (seen on Bathroom_2), so they are not reliable values.
        /// </summary>
        private static readonly Dictionary<LoadPeakComponent, tsdZoneArray> loadPeakComponents = new()
        {
            { LoadPeakComponent.Solar, tsdZoneArray.solarGain },
            { LoadPeakComponent.Lighting, tsdZoneArray.lightingGain },
            { LoadPeakComponent.OccupancySensible, tsdZoneArray.occupantSensibleGain },
            { LoadPeakComponent.EquipmentSensible, tsdZoneArray.equipmentSensibleGain },
            { LoadPeakComponent.InfiltrationVentilation, tsdZoneArray.infVentGain },
            { LoadPeakComponent.AirMovement, tsdZoneArray.airMovementGain },
            { LoadPeakComponent.BuildingHeatTransfer, tsdZoneArray.buildingHeatTransfer },
            { LoadPeakComponent.ExternalConductionOpaque, tsdZoneArray.externalConductionOpaque },
            { LoadPeakComponent.ExternalConductionGlazing, tsdZoneArray.externalConductionGlazing },
            { LoadPeakComponent.AirHandlingUnit, tsdZoneArray.AHUGain },
            { LoadPeakComponent.OccupancyLatent, tsdZoneArray.occupancyLatentGain },
            { LoadPeakComponent.EquipmentLatent, tsdZoneArray.equipmentLatentGain },
        };

        /// <summary>
        /// One peak read from a TSD, in SAM's engine-neutral form (see <see cref="Analytical.SpaceLoadPeak"/>).
        /// </summary>
        /// <param name="zoneData">The zone in the data set the peak belongs to: the design-day data for a
        /// design-day peak, the building (annual) data for an annual peak.</param>
        /// <param name="loadPeakBasis">Which simulation the peak is from.</param>
        /// <param name="load">The peak from <c>GetPeakZoneGains</c>, W, &gt;= 0.</param>
        /// <param name="hourIndex">The peak's 1-based TSD hourly index from <c>GetPeakZoneGains</c>; 0 when there is no peak.</param>
        /// <param name="buildingData">Annual peaks only: the source of the outdoor state at the peak. The
        /// design-day data sets have no building (weather) results, so a design-day peak has no outdoor state.</param>
        /// <param name="designDayName">Design-day peaks only.</param>
        /// <returns>
        /// The peak; a real zero (<c>Load</c> 0, no time, state or components) when the load is 0 and Tas reports
        /// no peak hour; <c>null</c> (unavailable) for anything that is not a valid Tas peak.
        /// </returns>
        public static SpaceLoadPeak SpaceLoadPeak(this ZoneData zoneData, LoadPeakBasis loadPeakBasis, double load, int hourIndex, BuildingData buildingData = null, string designDayName = null)
        {
            if (zoneData == null || loadPeakBasis == LoadPeakBasis.Undefined || double.IsNaN(load) || double.IsInfinity(load) || load < 0)
            {
                return null;
            }

            SpaceLoadPeak result = new SpaceLoadPeak(loadPeakBasis, load);
            if (loadPeakBasis == LoadPeakBasis.DesignDay)
            {
                result.DesignDayName = designDayName;
            }

            int? hourOfDay = Query.ZeroBasedHourOfDay(hourIndex);
            if (hourOfDay == null)
            {
                //No peak hour: a real zero only if the load says so too. A load without an hour is not a peak.
                return load == 0 ? result : null;
            }

            result.HourOfDay = hourOfDay;
            if (loadPeakBasis == LoadPeakBasis.AnnualSimulation)
            {
                result.HourOfYear = Query.ZeroBasedHourOfYear(hourIndex);
            }

            result.DryBulbTemperature = zoneData.GetHourlyZoneResult(hourIndex, (short)tsdZoneArray.dryBulbTemp);
            result.ResultantTemperature = zoneData.GetHourlyZoneResult(hourIndex, (short)tsdZoneArray.resultantTemp);
            result.RelativeHumidity = zoneData.GetHourlyZoneResult(hourIndex, (short)tsdZoneArray.relativeHumidity);
            result.HumidityRatio = zoneData.GetHourlyZoneResult(hourIndex, (short)tsdZoneArray.humidityRatio);

            if (loadPeakBasis == LoadPeakBasis.AnnualSimulation && buildingData != null)
            {
                result.OutdoorDryBulbTemperature = buildingData.GetHourlyBuildingResult(hourIndex, (int)tsdBuildingArray.externalTemperature);
                result.OutdoorRelativeHumidity = buildingData.GetHourlyBuildingResult(hourIndex, (int)tsdBuildingArray.externalHumidity);
            }

            foreach (KeyValuePair<LoadPeakComponent, tsdZoneArray> keyValuePair in loadPeakComponents)
            {
                result.SetComponent(keyValuePair.Key, zoneData.GetHourlyZoneResult(hourIndex, (short)keyValuePair.Value));
            }

            return result;
        }
    }
}
