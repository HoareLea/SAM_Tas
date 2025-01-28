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
            profileData.ClearModifiers();
            profileData.AddModifier(modifiableValue.Modifier);

            return true;
        }

        public static bool Update(this SizedVariable sizedVariable, ISizableValue sizableValue)
        {
            if (sizableValue == null || sizedVariable == null)
            {
                return false;
            }

            dynamic @dynamic = sizableValue;

            if(sizableValue is UnlimitedValue)
            {
                sizedVariable.Type = tpdSizedVariable.tpdSizedVariableNone;
                return true;
            }


            if(sizableValue is SizableValue)
            {
                SizableValue sizableValue_Temp = (SizableValue)sizableValue;

                sizedVariable.Type = sizableValue_Temp.SizingType.ToTPD();
                sizedVariable.Method = sizableValue_Temp.SizeMethod.ToTPD();
                sizedVariable.SizeFraction = sizableValue_Temp.SizeFraction;

                ModifiableValue modifiableValue = sizableValue_Temp.ModifiableValue;
                if(modifiableValue != null)
                {
                    try
                    {
                        dynamic.Value = modifiableValue.Value;
                    }
                    catch
                    {

                    }
                }
            }
            
            if(sizedVariable is DesignConditionSizableValue)
            {
                DesignConditionSizableValue designConditionSizableValue = (DesignConditionSizableValue)sizedVariable;
                HashSet<string> designCondtionSizedValues = designConditionSizableValue.DesignConditionNames;
                if(designCondtionSizedValues != null)
                {
                    //TODO: Implement
                }
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

        public static bool Update(this ControllerProfileData controllerProfileData, GFunction gFunction)
        {
            if (controllerProfileData == null || gFunction == null)
            {
                return false;
            }

            return true;
        }

    }
}