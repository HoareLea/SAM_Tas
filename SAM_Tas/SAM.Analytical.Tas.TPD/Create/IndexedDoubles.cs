// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using TPD;
using SAM.Core;
using System.Collections;
using System;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static IndexedDoubles IndexedDoubles(this SystemComponent systemComponent, Enum @enum, int start, int end, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            return IndexedDoubles(systemComponent, @enum, start, end, out string diagnostic, tpdResultsPeriod, tpdCombinerType);
        }

        /// <summary>
        /// Reads one hourly result series off a system component, and <b>says why</b> when it cannot.
        /// <para>
        /// The overload above answers null on failure and always did. That is fine where the caller
        /// treats an absent series as "this component has no such result", and wrong where a series is
        /// required: a COM refusal, an unsimulated document and a component that genuinely carries no
        /// such series all look identical, and the reason TAS gave is thrown away. This overload keeps
        /// the message so a caller that requires the series can report it instead of guessing.
        /// </para>
        /// <para>
        /// <b><c>foreach</c>, not <c>GetValue(int)</c>.</b> Measured: <c>GetResultsData</c> hands back an
        /// array a plain indexed walk runs off the end of. Enumerating it works.
        /// </para>
        /// </summary>
        /// <param name="diagnostic">What TAS said, or why the answer was unusable. Null on success.</param>
        public static IndexedDoubles IndexedDoubles(this SystemComponent systemComponent, Enum @enum, int start, int end, out string diagnostic, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            diagnostic = null;

            if (systemComponent == null)
            {
                diagnostic = "no component to read a result from.";
                return null;
            }

            object @object = null;
            try
            {
                @object = (systemComponent as dynamic).GetResultsData(tpdResultsPeriod, tpdCombinerType, System.Convert.ToInt32(@enum), start, end - start + 1);
            }
            catch (Exception exception)
            {
                diagnostic = string.Format("GetResultsData threw {0}: {1}", exception.GetType().Name, Compact(exception.Message));
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
                diagnostic = "GetResultsData returned nothing enumerable.";
                return null;
            }

            int index = start - 1;
            IndexedDoubles result = new IndexedDoubles();
            foreach (float value in enumerable)
            {
                result[index] = System.Convert.ToDouble(value);
                index++;
            }

            return result;
        }

        public static IndexedDoubles IndexedDoubles(this PlantComponent plantComponent, Enum @enum, int start, int end, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            return IndexedDoubles(plantComponent, @enum, start, end, out string diagnostic, tpdResultsPeriod, tpdCombinerType);
        }

        /// <summary>As the <c>SystemComponent</c> overload, for a plant component.</summary>
        public static IndexedDoubles IndexedDoubles(this PlantComponent plantComponent, Enum @enum, int start, int end, out string diagnostic, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            diagnostic = null;

            if (plantComponent == null)
            {
                diagnostic = "no component to read a result from.";
                return null;
            }

            object @object = null;
            try
            {
                @object = (plantComponent as dynamic).GetResultsData(tpdResultsPeriod, tpdCombinerType, System.Convert.ToInt32(@enum), start, end - start + 1);
            }
            catch (Exception exception)
            {
                diagnostic = string.Format("GetResultsData threw {0}: {1}", exception.GetType().Name, Compact(exception.Message));
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
                diagnostic = "GetResultsData returned nothing enumerable.";
                return null;
            }

            int index = start - 1;
            IndexedDoubles result = new IndexedDoubles();
            foreach (float value in enumerable)
            {
                result[index] = System.Convert.ToDouble(value);
                index++;
            }

            return result;
        }

        public static IndexedDoubles IndexedDoubles(this PlantController plantController, Enum @enum, int start, int end, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            return IndexedDoubles(plantController, @enum, start, end, out string diagnostic, tpdResultsPeriod, tpdCombinerType);
        }

        /// <summary>As the <c>SystemComponent</c> overload, for a plant controller.</summary>
        public static IndexedDoubles IndexedDoubles(this PlantController plantController, Enum @enum, int start, int end, out string diagnostic, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            diagnostic = null;

            if (plantController == null)
            {
                diagnostic = "no controller to read a result from.";
                return null;
            }

            object @object = null;
            try
            {
                @object = (plantController as dynamic).GetResultsData(tpdResultsPeriod, tpdCombinerType, System.Convert.ToInt32(@enum), start, end - start + 1);
            }
            catch (Exception exception)
            {
                diagnostic = string.Format("GetResultsData threw {0}: {1}", exception.GetType().Name, Compact(exception.Message));
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
                diagnostic = "GetResultsData returned nothing enumerable.";
                return null;
            }

            int index = start - 1;
            IndexedDoubles result = new IndexedDoubles();
            foreach (float value in enumerable)
            {
                result[index] = System.Convert.ToDouble(value);
                index++;
            }

            return result;
        }

        public static IndexedDoubles IndexedDoubles(this ZoneComponent zoneComponent, Enum @enum, int start, int end, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            return IndexedDoubles(zoneComponent, @enum, start, end, out string diagnostic, tpdResultsPeriod, tpdCombinerType);
        }

        /// <summary>As the <c>SystemComponent</c> overload, for a zone component.</summary>
        public static IndexedDoubles IndexedDoubles(this ZoneComponent zoneComponent, Enum @enum, int start, int end, out string diagnostic, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            diagnostic = null;

            if (zoneComponent == null)
            {
                diagnostic = "no zone component to read a result from.";
                return null;
            }

            object @object = null;
            try
            {
                @object = (zoneComponent as dynamic).GetResultsData(tpdResultsPeriod, tpdCombinerType, System.Convert.ToInt32(@enum), start, end - start + 1);
            }
            catch (Exception exception)
            {
                diagnostic = string.Format("GetResultsData threw {0}: {1}", exception.GetType().Name, Compact(exception.Message));
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
                diagnostic = "GetResultsData returned nothing enumerable.";
                return null;
            }

            int index = start - 1;
            IndexedDoubles result = new IndexedDoubles();
            foreach (float value in enumerable)
            {
                result[index] = System.Convert.ToDouble(value);
                index++;
            }

            return result;
        }

        /// <summary>A COM message on one line, so a diagnostic stays readable in a note.</summary>
        private static string Compact(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
