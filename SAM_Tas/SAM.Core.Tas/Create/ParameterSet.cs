using SAM.Core;
using System;

namespace SAM.Core.Tas
{
    public static partial class Create
    {
        public static ParameterSet ParameterSet(this Setting setting, object @object, Type type_Destination)
        {
            if (@object == null || type_Destination == null)
                return null;

            MapCluster mapCluster;
            if (!setting.TryGetValue(ActiveSetting.Name.ParameterMap, out mapCluster))
                return null;

            if (mapCluster == null)
                return null;

            return Core.Create.ParameterSet(@object, type_Destination.Assembly, type_Destination, mapCluster);
        }

        public static ParameterSet ParameterSet(this Setting setting, object @object, Type type_Source, Type type_Destination)
        {
            if (@object == null || type_Destination == null)
                return null;

            MapCluster mapCluster;
            if (!setting.TryGetValue(ActiveSetting.Name.ParameterMap, out mapCluster))
                return null;

            if (mapCluster == null)
                return null;

            return Core.Create.ParameterSet(@object, type_Destination.Assembly, type_Source.FullName, type_Destination.FullName, mapCluster);
        }
    }
}