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
            if(systemComponent == null)
            {
                return null;
            }


            object @object = null;
            try
            {
                @object = (systemComponent as dynamic).GetResultsData(tpdResultsPeriod, tpdCombinerType, System.Convert.ToInt32(@enum), start, end - start + 1);
            }
            catch (Exception exception)
            {
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
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
            if (plantComponent == null)
            {
                return null;
            }


            object @object = null;
            try
            {
                @object = (plantComponent as dynamic).GetResultsData(tpdResultsPeriod, tpdCombinerType, System.Convert.ToInt32(@enum), start, end - start + 1);
            }
            catch (Exception exception)
            {
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
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
            if (plantController == null)
            {
                return null;
            }


            object @object = null;
            try
            {
                @object = (plantController as dynamic).GetResultsData(tpdResultsPeriod, tpdCombinerType, System.Convert.ToInt32(@enum), start, end - start + 1);
            }
            catch (Exception exception)
            {
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
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
            if (zoneComponent == null)
            {
                return null;
            }

            object @object = null;
            try
            {
                @object = (zoneComponent as dynamic).GetResultsData(tpdResultsPeriod, tpdCombinerType, System.Convert.ToInt32(@enum), start, end - start + 1);
            }
            catch(Exception exception)
            {
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
                return null;
            }

            int index = start - 1;
            IndexedDoubles result = new IndexedDoubles();
            foreach(float value in enumerable)
            {
                result[index] = System.Convert.ToDouble(value);
                index++;
            }

            return result;
        }
    }
}
