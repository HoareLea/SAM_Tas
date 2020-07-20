using SAM.Core;
using System;

namespace SAM.Core.Tas
{
    public static partial class Create
    {
        public static ParameterSet ParameterSet(this Setting setting, object @object, Type type_destination)
        {
            if (@object == null || type_destination == null)
                return null;

            MapCluster mapCluster;
            if (!setting.TryGetValue(ActiveSetting.Name.ParameterMap, out mapCluster))
                return null;

            if (mapCluster == null)
                return null;

            return Core.Create.ParameterSet(@object, type_destination, mapCluster);
        }
    }
}