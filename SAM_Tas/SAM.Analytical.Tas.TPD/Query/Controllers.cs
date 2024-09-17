using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<Controller> Controllers(this global::TPD.System system)
        {
            return Controllers(system as ISystem);
        }

        public static List<Controller> Controllers(this ISystem system)
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
                if (controller != null)
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
    }
}