using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<ControllerProfilePoint> ControllerProfilePoints(this ControllerProfileData controllerProfileData)
        {
            if (controllerProfileData == null)
            {
                return null;
            }

            List<ControllerProfilePoint> result = new List<ControllerProfilePoint>();

            int count = controllerProfileData.Size();
            for (int i = 1; i <= count; i++)
            {
                ControllerProfilePoint controllerProfilePoint = controllerProfileData.GetAt(i);
                if (controllerProfilePoint != null)
                {
                    result.Add(controllerProfilePoint);
                }
            }

            return result;
        }

    }
}