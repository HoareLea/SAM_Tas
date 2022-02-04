using System;

namespace SAM.Core.Tas
{
    public static partial class Create
    {
        public static ParameterSet ParameterSet(this Setting setting, object @object, Type type_Destination)
        {
            if (@object == null || type_Destination == null)
                return null;

            TypeMap typeMap;
            if (!setting.TryGetValue(ActiveSetting.Name.ParameterMap, out typeMap))
                return null;

            if (typeMap == null)
                return null;

            return Core.Create.ParameterSet(@object, type_Destination.Assembly, type_Destination, typeMap);
        }

        public static ParameterSet ParameterSet(this Setting setting, object @object, Type type_Source, Type type_Destination)
        {
            if (@object == null || type_Destination == null)
                return null;

            TypeMap typeMap;
            if (!setting.TryGetValue(ActiveSetting.Name.ParameterMap, out typeMap))
                return null;

            if (typeMap == null)
                return null;

            return Core.Create.ParameterSet(@object, type_Destination.Assembly, Core.Query.FullTypeName(type_Source), Core.Query.FullTypeName(type_Destination), typeMap);
        }
    }
}