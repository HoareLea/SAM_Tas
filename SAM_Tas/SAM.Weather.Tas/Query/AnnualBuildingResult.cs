// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections;
using System.Collections.Generic;
using TSD;

namespace SAM.Weather.Tas
{
    public static partial class Query
    {
        public static List<T> AnnualBuildingResult<T>(this BuildingData buildingData, tsdBuildingArray tsdBuildingArray)
        {
            if (buildingData == null)
                return null;

            return BuildingResultValues<T>(buildingData.GetAnnualBuildingResult((int)tsdBuildingArray));
        }

        /// <summary>
        /// A TSD building result - annual or daily - as a list of <typeparamref name="T"/>, in the order TSD returned
        /// it; null when TSD answered nothing enumerable.
        /// </summary>
        internal static List<T> BuildingResultValues<T>(object result_TSD)
        {
            IEnumerable enumerable = result_TSD as IEnumerable;
            if (enumerable == null)
                return null;

            List<T> result = new List<T>();
            foreach(object @object in enumerable)
            {
                if (@object == null)
                {
                    result.Add(default);
                }
                else if (@object is T)
                {
                    result.Add((T)@object);
                }
                else
                {
                    T value;
                    if (!Core.Query.TryConvert(@object, out value))
                        result.Add(default);
                    else
                        result.Add(value);
                }
            }

            return result;
        }
    }
}