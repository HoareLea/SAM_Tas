// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Weather;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The design days a run must write into the TBD it is about to size, given the weather that run
        /// treats as authoritative.
        /// <para>
        /// A SAM model's <c>CoolingDesignDays</c> / <c>HeatingDesignDays</c> parameters are not authored
        /// data: <c>Convert.ToSAM(path_TBD, …)</c> DERIVES them from the weather it finds in the TBD it is
        /// importing. So a model that came back from a TBD carries the design days of THAT TBD's weather.
        /// When the next run states a weather of its own, those carried design days belong to the previous
        /// weather, and writing them would size the first new-weather TBD on old-weather design days - the
        /// generation after it would then import the new weather, re-derive, and only then agree. B1 != B2,
        /// B2 == B3.
        /// </para>
        /// <para>
        /// Hence the rule: <b>a run that states its own weather makes that weather authoritative over every
        /// weather-derived design day the caller did not state outright.</b> Design days passed explicitly
        /// (<c>WorkflowSettings.DesignDays_*</c>, or the <c>ToTBD</c> arguments) are engineering intent and
        /// always win - that is the supported way to carry a customised design day, e.g. a heating design
        /// day whose temperature the engineer overrode on <c>SAMAnalytical.DesignDays</c>. A run that states
        /// NO weather of its own leaves the model's design days alone: the model's weather is then the only
        /// weather there is, and its design days were derived from exactly that weather.
        /// </para>
        /// <para>
        /// The model is also the fallback for a slot the authoritative weather could not fill (a
        /// <see cref="WeatherData"/> with no weather years yields no design day) - sizing on the previous
        /// weather's design day beats sizing on none at all, which TAS answers with a zero load.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The model being exported. May be null.</param>
        /// <param name="weatherData_Supplied">The weather the run states for itself, or null when it states none.</param>
        /// <param name="coolingDesignDays_Supplied">Cooling design days the caller stated outright, or null.</param>
        /// <param name="heatingDesignDays_Supplied">Heating design days the caller stated outright, or null.</param>
        /// <param name="coolingDesignDays">The cooling design days to write, or null for none.</param>
        /// <param name="heatingDesignDays">The heating design days to write, or null for none.</param>
        public static void DesignDays_Authoritative(
            AnalyticalModel analyticalModel,
            WeatherData weatherData_Supplied,
            IEnumerable<DesignDay> coolingDesignDays_Supplied,
            IEnumerable<DesignDay> heatingDesignDays_Supplied,
            out List<DesignDay> coolingDesignDays,
            out List<DesignDay> heatingDesignDays)
        {
            coolingDesignDays = DesignDays_NullIfEmpty(coolingDesignDays_Supplied);
            heatingDesignDays = DesignDays_NullIfEmpty(heatingDesignDays_Supplied);

            if (weatherData_Supplied != null)
            {
                if (coolingDesignDays == null)
                {
                    coolingDesignDays = DesignDays_Single(weatherData_Supplied.CoolingDesignDay());
                }

                if (heatingDesignDays == null)
                {
                    heatingDesignDays = DesignDays_Single(weatherData_Supplied.HeatingDesignDay());
                }
            }

            if (analyticalModel == null)
            {
                return;
            }

            if (coolingDesignDays == null && analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.CoolingDesignDays, out SAMCollection<DesignDay> coolingDesignDays_Model))
            {
                coolingDesignDays = DesignDays_NullIfEmpty(coolingDesignDays_Model);
            }

            if (heatingDesignDays == null && analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.HeatingDesignDays, out SAMCollection<DesignDay> heatingDesignDays_Model))
            {
                heatingDesignDays = DesignDays_NullIfEmpty(heatingDesignDays_Model);
            }
        }

        private static List<DesignDay> DesignDays_NullIfEmpty(IEnumerable<DesignDay> designDays)
        {
            if (designDays == null)
            {
                return null;
            }

            List<DesignDay> result = new List<DesignDay>();
            foreach (DesignDay designDay in designDays)
            {
                if (designDay != null)
                {
                    result.Add(designDay);
                }
            }

            return result.Count == 0 ? null : result;
        }

        private static List<DesignDay> DesignDays_Single(DesignDay designDay)
        {
            return designDay == null ? null : new List<DesignDay> { designDay };
        }
    }
}
