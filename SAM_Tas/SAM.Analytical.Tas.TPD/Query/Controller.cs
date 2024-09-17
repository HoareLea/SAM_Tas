using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static Controller Controller(this PlantRoom plantRoom, string reference)
        {
            if (plantRoom == null || string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            PathReference pathReference = Core.Convert.ComplexReference(reference) as PathReference;
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
            if (objectReference?.TypeName == Core.Query.FullTypeName(typeof(PlantRoom)))
            {
                if(Core.Query.TryConvert(objectReference.Reference?.ToString(), out System.Guid guid))
                {
                    if (global::System.Guid.Parse(plantRoom.GUID()) != guid)
                    {
                        return null;
                    }
                }

                objectReferences.RemoveAt(0);
                objectReference = objectReferences[0];
            }

            global::TPD.System system = null;
            if (objectReference.TypeName == Core.Query.FullTypeName(typeof(global::TPD.System)))
            {
                if (Core.Query.TryConvert(objectReference.Reference?.ToString(), out System.Guid guid))
                {
                    system = plantRoom.Systems().Find(x => global::System.Guid.Parse(x.GUID) == guid);
                }
                
                objectReferences.RemoveAt(0);
                objectReference = objectReferences[0];
            }

            if(system == null)
            {
                return null;
            }

            ComponentGroup componentGroup = null;
            if (objectReference.TypeName == Core.Query.FullTypeName(typeof(ComponentGroup)))
            {
                if (Core.Query.TryConvert(objectReference.Reference?.ToString(), out System.Guid guid))
                {
                    List<SystemComponent> systemComponents = SystemComponents<SystemComponent>(system);
                    systemComponents.RemoveAll(x => !(x is ComponentGroup));

                    componentGroup = systemComponents.Find(x => global::System.Guid.Parse((x as dynamic).Guid) == guid) as ComponentGroup;
                }

                objectReferences.RemoveAt(0);
                objectReference = objectReferences[0];
            }

            if (objectReference.TypeName != Core.Query.FullTypeName(typeof(Controller)))
            {
                return null;
            }

            if(system == null && componentGroup == null)
            {
                return null;
            }

            if (!Core.Query.TryConvert(objectReference.Reference?.ToString(), out int index))
            {
                return null;
            }

            if (componentGroup != null)
            {
                return componentGroup.Controllers().ElementAt(index);
            }

            return system.Controllers().ElementAt(index);
        }
    }
}