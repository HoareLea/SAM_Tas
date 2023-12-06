using TPD;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static IndexedDoubles IndexedDoubles(this SystemComponent systemComponent, ResultDataType resultDataType, int start, int end, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            if(systemComponent == null)
            {
                return null;
            }

            object @object = systemComponent.GetResultsData(tpdResultsPeriod, tpdCombinerType, (int)resultDataType, start, end - start);
            if(@object == null)
            {
                return null;
            }


            IndexedDoubles result = new IndexedDoubles();

            return result;
        }

        public static IndexedDoubles IndexedDoubles(this ZoneComponent zoneComponent, ResultDataType resultDataType, int start, int end, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            if (zoneComponent == null)
            {
                return null;
            }

            object @object = ((SystemComponent)zoneComponent).GetResultsData(tpdResultsPeriod, tpdCombinerType, (int)resultDataType, start, end - start);
            if (@object == null)
            {
                return null;
            }


            IndexedDoubles result = new IndexedDoubles();

            return result;
        }
    }
}
