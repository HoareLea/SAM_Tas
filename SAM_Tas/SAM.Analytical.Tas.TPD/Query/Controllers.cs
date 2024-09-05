using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<Controller> Controllers(this global::TPD.System system)
        {
            if (system == null)
            {
                return null;
            }

            List<Controller> result = new List<Controller>();

            int count = system.GetControllerCount();
            for (int i = 1; i <= count; i++)
            {
                Controller controller = system.GetController(i);
                if(controller != null)
                {
                    result.Add(controller);
                }
            }

            return result;
        }

        public static List<Controller> Controllers(this IComponentGroup componentGroup)
        {
            if (componentGroup == null)
            {
                return null;
            }

            List<Controller> result = new List<Controller>();

            int count = componentGroup.GetControllerCount();
            for (int i = 1; i <= count; i++)
            {
                Controller controller = componentGroup.GetController(i);
                if (controller != null)
                {
                    result.Add(controller);
                }
            }

            return result;
        }


        public class MyController
        {
            public double X { get; }
            public double Y { get; }

            public MyController(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public class MyControllerGroup : MyController
        {
            public MyControllerGroup(double x, double y)
                :base(x, y)
            {

            }
        }

        public static void Test(this IComponentGroup componentGroup)
        {
            int multiplicity = componentGroup.GetMultiplicity();

            //List represents controllers placed on schematic (location is equivalent to the real location of controller on schematic)
            List<MyControllerGroup> myControllerGroups = new List<MyControllerGroup>();

            //List represents "hidden from canvas" controllers (one controller for every single zone)
            List<MyController> myControllers = new List<MyController>();

            int count = componentGroup.GetControllerCount();
            for (int i = 1; i <= count; i++)
            {
                Controller controller = componentGroup.GetController(i);
                if (controller == null)
                {
                    continue;
                }
                TasPosition tasPosition = controller.GetPosition();

                switch (controller.ControlType)
                {
                    case tpdControlType.tpdControlGroup:
                        myControllerGroups.Add(new MyControllerGroup(tasPosition.x, tasPosition.y));
                        break;

                    default:
                        myControllers.Add(new MyController(tasPosition.x, tasPosition.y));
                        break;

                }
            }
            

            if (myControllers.Count == myControllerGroups.Count * multiplicity)
            {
                //Always true
            }

            //In here:
            //Every single MyControllerGroup shall have list of MyController which represents Controller for each Zone
            // How to find MyControllerGroup for given MyController

        }
    }
}