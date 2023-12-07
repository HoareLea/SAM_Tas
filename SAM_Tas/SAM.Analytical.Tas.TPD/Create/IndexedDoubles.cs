using TPD;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static IndexedDoubles IndexedDoubles(this SystemComponent systemComponent, FanCoilUnitDataType fanCoilUnitDataType, int start, int end, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            if(systemComponent == null)
            {
                return null;
            }

            object @object = systemComponent.GetResultsData(tpdResultsPeriod, tpdCombinerType, (int)fanCoilUnitDataType, start, end - start);
            if(@object == null)
            {
                return null;
            }


            IndexedDoubles result = new IndexedDoubles();

            return result;
        }

        public static IndexedDoubles IndexedDoubles(this ZoneComponent zoneComponent, FanCoilUnitDataType fanCoilUnitDataType, int start, int end, tpdResultsPeriod tpdResultsPeriod = tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType tpdCombinerType = tpdCombinerType.tpdCombinerTypeMax)
        {
            if (zoneComponent == null)
            {
                return null;
            }

            object @object = ((SystemComponent)zoneComponent).GetResultsData(tpdResultsPeriod, tpdCombinerType, (int)fanCoilUnitDataType, start, end - start);
            if (@object == null)
            {
                return null;
            }


            IndexedDoubles result = new IndexedDoubles();

            return result;
        }
    }
}
