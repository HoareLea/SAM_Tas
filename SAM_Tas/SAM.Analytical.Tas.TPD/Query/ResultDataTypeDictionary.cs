using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Core.Tas.TPD
{
    public static partial class Query
    {
        public static Dictionary<ResultDataType, IndexedDoubles> ResultDataTypeDictionary(string path, ResultPeriod resultPeriod, IEnumerable<ResultDataType> resultDataTypes, IEnumerable<int> plantRoomIndexes = null, bool detailedCategory = false, bool regulatedEnergyOnly = false, bool perUnitArea = false)
        {
            if(string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return null;
            }

            if(resultPeriod == ResultPeriod.Undefined || resultDataTypes == null || resultDataTypes.Count() == 0)
            {
                return null;
            }

            Dictionary<ResultDataType, IndexedDoubles> result = null;

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path))
            {
                TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                if (tPDDoc == null)
                {
                    return null;
                }

                result = ResultDataTypeDictionary(tPDDoc.EnergyCentre, resultPeriod, resultDataTypes, plantRoomIndexes, detailedCategory, regulatedEnergyOnly, perUnitArea);
            }

            return result;
        }

        public static Dictionary<ResultDataType, IndexedDoubles> ResultDataTypeDictionary(this EnergyCentre energyCentre, ResultPeriod resultPeriod, IEnumerable<ResultDataType> resultDataTypes, IEnumerable<int> plantRoomIndexes = null, bool detailedCategory = false, bool regulatedEnergyOnly = false, bool perUnitArea = false)
        {
            if (energyCentre == null || resultPeriod == ResultPeriod.Undefined || resultDataTypes == null || resultDataTypes.Count() == 0)
            {
                return null;
            }

            PlantRoom[] plantRooms_Selected = null;

            if(plantRoomIndexes != null && plantRoomIndexes.Count() != 0)
            {
                List<PlantRoom> plantRooms = energyCentre.PlantRooms();

                plantRooms_Selected = plantRoomIndexes.ToList().ConvertAll(x => plantRooms[x]).ToArray();
            }

            WrResultSet wrResultSet = (WrResultSet)energyCentre.GetResultSet(resultPeriod.ToTPD(), perUnitArea ? 1 : 0, detailedCategory ? 1 : 0, regulatedEnergyOnly ? 1 : 0, plantRooms_Selected);
            if (wrResultSet == null)
            {
                return null;
            }



            Dictionary<ResultDataType, IndexedDoubles> result = new Dictionary<ResultDataType, IndexedDoubles>();
            foreach (ResultDataType resultDataType in resultDataTypes)
            {
                tpdResultVectorType tpdResultVectorType = resultDataType.ToTPD();

                IndexedDoubles indexedDoubles = new IndexedDoubles();

                int count = wrResultSet.GetVectorSize(tpdResultVectorType);
                for (int j = 1; j <= count; j++)
                {
                    WrResultItem wrResultItem = (WrResultItem)wrResultSet.GetResultItem(tpdResultVectorType, j);
                    if (wrResultItem != null)
                    {
                        Array array = (Array)wrResultItem.GetValues();
                        if (array == null || array.Length == 0)
                        {
                            continue;
                        }

                        for (int i = 0; i < array.Length; i++)
                        {
                            indexedDoubles[i] += (double)array.GetValue(i);
                        }
                    }
                }

                result[resultDataType] = indexedDoubles;
            }


            return result;
        }
    }
}