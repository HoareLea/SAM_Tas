using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPhotovoltaicPanelResult ToSAM_SystemPhotovoltaicPanelResult(this PVPanel pVPanel, int start, int end, params PhotovoltaicPanelDataType[] photovoltaicPanelDataTypes)
        {
            if (pVPanel == null)
            {
                return null;
            }

            IEnumerable<PhotovoltaicPanelDataType> photovoltaicPanelDataTypes_Temp = photovoltaicPanelDataTypes == null || photovoltaicPanelDataTypes.Length == 0 ? System.Enum.GetValues(typeof(PhotovoltaicPanelDataType)).Cast<PhotovoltaicPanelDataType>() : photovoltaicPanelDataTypes;

            Dictionary<PhotovoltaicPanelDataType, IndexedDoubles> dictionary = new Dictionary<PhotovoltaicPanelDataType, IndexedDoubles>();
            foreach (PhotovoltaicPanelDataType photovoltaicPanelDataType in photovoltaicPanelDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)pVPanel, photovoltaicPanelDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(photovoltaicPanelDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[photovoltaicPanelDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)pVPanel);

            SystemPhotovoltaicPanelResult result = new SystemPhotovoltaicPanelResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
