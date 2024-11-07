using TPD;
using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static IReference Reference(this Controller controller)
        {
            if(controller == null)
            {
                return null;
            }

            int index = -1;

            global::TPD.System system = controller.GetSystem();
            if(system == null)
            {
                return null;
            }

            PlantRoom plantRoom = system.GetPlantRoom();
            if(plantRoom == null)
            {
                return null;
            }

            List<ObjectReference> objectReferences = new List<ObjectReference>() 
            { 
                new ObjectReference(typeof(PlantRoom), Guid.Parse(plantRoom.GUID())), 
                new ObjectReference(typeof(global::TPD.System), Guid.Parse(system.GUID)) 
            };

            List<Controller> controllers = null;

            Type type = null;

            ComponentGroup componentGroup = controller.GetGroup();
            if(componentGroup != null)
            {
                controllers = componentGroup.Controllers();

                objectReferences.Add(new ObjectReference(typeof(ComponentGroup), Guid.Parse((componentGroup as dynamic).Guid)));
            }
            else
            {
                controllers = system.Controllers();
            }

            for (int i = 0; i < controllers.Count; i++)
            {
                if (controller == controllers[i])
                {
                    index = i;
                    break;
                }
            }

            if(index == -1)
            {
                return null;
            }

            objectReferences.Add(new ObjectReference(typeof(Controller), index));

            return new PathReference(objectReferences);
        }

        public static IReference Reference(this PlantController plantController)
        {
            if (plantController == null)
            {
                return null;
            }

            int index = -1;

            PlantRoom plantRoom = plantController.GetPlantRoom();
            if (plantRoom == null)
            {
                return null;
            }

            List<ObjectReference> objectReferences = new List<ObjectReference>()
            {
                new ObjectReference(typeof(PlantRoom), Guid.Parse(plantRoom.GUID()))
            };

            List<PlantController> plantControllers = null;

            Type type = null;

            PlantComponentGroup plantComponentGroup = plantController.GetGroup();
            if (plantComponentGroup != null)
            {
                plantControllers = plantComponentGroup.PlantControllers();

                objectReferences.Add(new ObjectReference(typeof(ComponentGroup), Guid.Parse((plantComponentGroup as dynamic).Guid)));
            }
            else
            {
                plantControllers = plantRoom.PlantControllers();
            }

            for (int i = 0; i < plantControllers.Count; i++)
            {
                if (plantController == plantControllers[i])
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return null;
            }

            objectReferences.Add(new ObjectReference(typeof(PlantController), index));

            return new PathReference(objectReferences);
        }

    }
}
