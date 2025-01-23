using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.Systems;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool Update(this ProfileData profileData, ModifiableValue modifiableValue)
        {
            if(profileData == null || modifiableValue == null)
            {
                return false;
            }

            profileData.Value = modifiableValue.Value;
            profileData.AddModifier(modifiableValue.Modifier);

            return true;
        }

        public static bool Update(this SizedVariable sizedVariable, SizableValue sizableValue)
        {
            if (sizableValue == null || sizedVariable == null)
            {
                return false;
            }

            return true;
        }

        public static bool Update(this SizedFlowVariable sizedFlowVariable, SizedFlowValue sizedFlowValue)
        {
            if (sizedFlowVariable == null || sizedFlowValue == null)
            {
                return false;
            }

            sizedFlowVariable.Value = sizedFlowValue.Value;
            sizedFlowVariable.SizeFraction = sizedFlowValue.SizeFranction;

            return true;
        }

    }
}