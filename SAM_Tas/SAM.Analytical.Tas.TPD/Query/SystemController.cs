using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static ISystemController SystemController<T>(this SystemPlantRoom systemPlantRoom, string reference) where T : ISystemController
        {
            if (systemPlantRoom == null || string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            return SystemController<T>(systemPlantRoom, Core.Convert.ComplexReference(reference));
        }

        public static ISystemController SystemController<T>(this SystemPlantRoom systemPlantRoom, IReference reference)
        {
            PathReference pathReference = reference as PathReference;
            if (pathReference == null)
            {
                return null;
            }

            List<ObjectReference> objectReferences = new List<ObjectReference>(pathReference);
            if (objectReferences.Count < 2)
            {
                return null;
            }

            ObjectReference objectReference = objectReferences[0];
            if (objectReference.TypeName == Core.Query.FullTypeName(typeof(PlantRoom)))
            {
                if (Core.Query.TryConvert(systemPlantRoom.Reference(), out System.Guid guid_SystemPlantRoom))
                {
                    if (Core.Query.TryConvert(objectReference.Reference.ToString(), out System.Guid guid))
                    {
                        if (guid_SystemPlantRoom != guid)
                        {
                            return null;
                        }
                    }
                }

                objectReferences.RemoveAt(0);
                objectReference = objectReferences[0];
            }

            Core.Systems.ISystem system = null;
            if (objectReference.TypeName == Core.Query.FullTypeName(typeof(global::TPD.System)))
            {
                if (Core.Query.TryConvert(objectReference.Reference.ToString(), out System.Guid guid))
                {
                    system = systemPlantRoom.GetSystem<Core.Systems.ISystem>(x => global::System.Guid.Parse(x.Reference()) == guid);
                }

                objectReferences.RemoveAt(0);
                objectReference = objectReferences[0];
            }

            ISystemGroup systemGroup = null;
            if (objectReference.TypeName == Core.Query.FullTypeName(typeof(ComponentGroup)))
            {
                if (Core.Query.TryConvert(objectReference.Reference.ToString(), out System.Guid guid))
                {
                    systemGroup = systemPlantRoom.GetSystemGroups<ISystemGroup>()?.Find(x => global::System.Guid.Parse(x.Reference()) == guid);
                }

                objectReferences.RemoveAt(0);
                objectReference = objectReferences[0];
            }

            if (objectReference.TypeName != Core.Query.FullTypeName(typeof(Controller)))
            {
                return null;
            }

            List<ISystemController> systemControllers = system == null ? systemPlantRoom.GetSystemComponents<ISystemController>() : systemPlantRoom.GetSystemComponents<ISystemController>(system);
            foreach (ISystemController systemController in systemControllers)
            {
                if (systemController?.Reference() == pathReference.ToString())
                {
                    if (systemGroup != null)
                    {
                        if (systemPlantRoom.GetRelatedObjects<ISystemController>(systemGroup)?.Find(x => (x as dynamic).Guid == ((dynamic)systemController).Guid) == null)
                        {
                            continue;
                        }
                    }

                    return systemController;
                }
            }

            return null;
        }
    }
}