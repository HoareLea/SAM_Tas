using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSignalControllerResult ToSAM_SystemSignalControllerResult(this PlantController plantController, int start, int end, params SignalControllerDataType[] signalControllerDataTypes)
        {
            if (plantController == null)
            {
                return null;
            }

            IEnumerable<SignalControllerDataType> signalControllerDataTypes_Temp = signalControllerDataTypes == null || signalControllerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SignalControllerDataType)).Cast<SignalControllerDataType>() : signalControllerDataTypes;

            Dictionary<SignalControllerDataType, IndexedDoubles> dictionary = new Dictionary<SignalControllerDataType, IndexedDoubles>();
            foreach (SignalControllerDataType signalControllerDataType in signalControllerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles(plantController, signalControllerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(signalControllerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[signalControllerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Create.Reference(plantController)?.ToString();

            SystemSignalControllerResult result = new SystemSignalControllerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }

        public static SystemSignalControllerResult ToSAM_SystemSignalControllerResult(this Controller controller, int start, int end, params SignalControllerDataType[] signalControllerDataTypes)
        {
            if (controller == null)
            {
                return null;
            }

            IEnumerable<SignalControllerDataType> signalControllerDataTypes_Temp = signalControllerDataTypes == null || signalControllerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SignalControllerDataType)).Cast<SignalControllerDataType>() : signalControllerDataTypes;

            Dictionary<SignalControllerDataType, IndexedDoubles> dictionary = new Dictionary<SignalControllerDataType, IndexedDoubles>();
            foreach (SignalControllerDataType signalControllerDataType in signalControllerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)controller, signalControllerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(signalControllerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[signalControllerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((SystemComponent)controller);

            SystemSignalControllerResult result = new SystemSignalControllerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



