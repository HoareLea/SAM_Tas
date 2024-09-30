using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Core.Tas.TPD
{
    public static partial class Query
    {
        public static List<SystemEnergyCentreResult> SystemEnergyCentreResults(string path, ResultPeriod resultPeriod, IEnumerable<SystemEnergyCentreDataType> systemEnergyCentreDataTypes, IEnumerable<int> plantRoomIndexes = null, bool detailedCategory = false, bool regulatedEnergyOnly = false, bool perUnitArea = false)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return null;
            }

            if (resultPeriod == ResultPeriod.Undefined || systemEnergyCentreDataTypes == null || systemEnergyCentreDataTypes.Count() == 0)
            {
                return null;
            }

            List<SystemEnergyCentreResult> result = null;

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path))
            {
                TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                if (tPDDoc == null)
                {
                    return null;
                }

                result = SystemEnergyCentreResults(tPDDoc.EnergyCentre, resultPeriod, systemEnergyCentreDataTypes, plantRoomIndexes, detailedCategory, regulatedEnergyOnly, perUnitArea);
            }

            return result;
        }

        public static List<SystemEnergyCentreResult> SystemEnergyCentreResults(this EnergyCentre energyCentre, ResultPeriod resultPeriod, IEnumerable<SystemEnergyCentreDataType> systemEnergyCentreDataTypes, IEnumerable<int> plantRoomIndexes = null, bool detailedCategory = false, bool regulatedEnergyOnly = false, bool perUnitArea = false)
        {
            if (energyCentre == null || resultPeriod == ResultPeriod.Undefined || systemEnergyCentreDataTypes == null || systemEnergyCentreDataTypes.Count() == 0)
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

            List<SystemEnergyCentreResult> result = new List<SystemEnergyCentreResult>();
            foreach (SystemEnergyCentreDataType systemEnergyCentreDataType in systemEnergyCentreDataTypes)
            {
                tpdResultVectorType tpdResultVectorType = systemEnergyCentreDataType.ToTPD();

                List<SystemEnergyCentreValues> systemEnergyCentreValuesList = new List<SystemEnergyCentreValues>(); 

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

                        IndexedDoubles indexedDoubles = new IndexedDoubles();

                        for (int i = 0; i < array.Length; i++)
                        {
                            indexedDoubles[i] = (double)array.GetValue(i);
                        }

                        string name = wrResultItem.GetPlantComponentName();
                        if(string.IsNullOrEmpty(name))
                        {
                            name = wrResultItem.GetFuelSource()?.Name;
                        }

                        string unitName = wrResultItem.GetUnitString();

                        SystemEnergyCentreValues systemEnergyCentreValues = new SystemEnergyCentreValues(name, wrResultItem.Category, unitName, indexedDoubles);
                        systemEnergyCentreValuesList.Add(systemEnergyCentreValues);
                    }
                }

                result.Add(new SystemEnergyCentreResult(energyCentre.Name, energyCentre.Name, Analytical.Tas.TPD.Query.Source(), systemEnergyCentreDataType, systemEnergyCentreValuesList));
            }


            return result;
        }
    }
}