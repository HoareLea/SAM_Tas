using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSolarPanelResult ToSAM_SystemSolarPanelResult(this SolarPanel solarPanel, int start, int end, params SolarPanelDataType[] solarPanelDataTypes)
        {
            if (solarPanel == null)
            {
                return null;
            }

            IEnumerable<SolarPanelDataType> solarPanelDataTypes_Temp = solarPanelDataTypes == null || solarPanelDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SolarPanelDataType)).Cast<SolarPanelDataType>() : solarPanelDataTypes;

            Dictionary<SolarPanelDataType, IndexedDoubles> dictionary = new Dictionary<SolarPanelDataType, IndexedDoubles>();
            foreach (SolarPanelDataType solarPanelDataType in solarPanelDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)solarPanel, solarPanelDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(solarPanelDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[solarPanelDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)solarPanel);

            SystemSolarPanelResult result = new SystemSolarPanelResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
