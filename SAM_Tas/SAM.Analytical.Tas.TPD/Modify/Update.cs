using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool Update(this ProfileData profileData, ModifiableValue modifiableValue, EnergyCentre energyCentre)
        {
            if(profileData == null || modifiableValue == null)
            {
                return false;
            }

            profileData.Value = modifiableValue.Value;
            profileData.ClearModifiers();
            profileData.AddModifier(modifiableValue.Modifier, energyCentre);

            return true;
        }

        public static bool Update(this SizedVariable sizedVariable, ISizableValue sizableValue, PlantRoom plantRoom)
        {
            return Update(sizedVariable, sizableValue, plantRoom?.GetEnergyCentre());
        }

        public static bool Update(this SizedVariable sizedVariable, ISizableValue sizableValue, global::TPD.System system)
        {
            return Update(sizedVariable, sizableValue, system?.GetPlantRoom()?.GetEnergyCentre());
        }


        public static bool Update(this SizedVariable sizedVariable, ISizableValue sizableValue, EnergyCentre energyCentre)
        {
            if (sizableValue == null || sizedVariable == null)
            {
                return false;
            }

            dynamic @dynamic = sizedVariable;

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
                    dynamic.Value = modifiableValue.Value;
                }
            }
            
            if(sizableValue is DesignConditionSizableValue && energyCentre != null)
            {
                DesignConditionSizableValue designConditionSizableValue = (DesignConditionSizableValue)sizableValue;
                HashSet<string> designConditionNames = designConditionSizableValue.DesignConditionNames;
                if(designConditionNames != null)
                {
                    foreach(string designConditionName in designConditionNames)
                    {
                        if(designConditionName == "Simulation Data")
                        {
                            sizedVariable.AddDesignCondition(energyCentre.GetNullDesignCondition());
                            continue;
                        }
                        else
                        {
                            DesignConditionLoad designConditionLoad = energyCentre.DesignConditionLoad(designConditionName);
                            if (designConditionLoad != null)
                            {
                                sizedVariable.AddDesignCondition(designConditionLoad);
                            }
                        }
                    }
                }
            }

            return true;
        }

        public static bool Update(this SizedFlowVariable sizedFlowVariable, SizedFlowValue sizedFlowValue, EnergyCentre energyCentre)
        {
            if (sizedFlowVariable == null || sizedFlowValue == null)
            {
                return false;
            }

            sizedFlowVariable.Value = sizedFlowValue.Value;
            sizedFlowVariable.SizeFraction = sizedFlowValue.SizeFranction;

            if(sizedFlowValue is DesignConditionSizedFlowValue)
            {
                DesignConditionSizedFlowValue designConditionSizedFlowValue = (DesignConditionSizedFlowValue)sizedFlowValue;

                sizedFlowVariable.Type = designConditionSizedFlowValue.SizingType.ToTPD();

                sizedFlowVariable.SizeValue1 = designConditionSizedFlowValue.SizeValue1;
                sizedFlowVariable.SizeValue2 = designConditionSizedFlowValue.SizeValue2;

                sizedFlowVariable.Method = designConditionSizedFlowValue.SizedFlowMethod.ToTPD();

                HashSet<string> designConditionNames = designConditionSizedFlowValue.DesignConditionNames;
                if (designConditionNames != null)
                {
                    foreach (string designConditionName in designConditionNames)
                    {
                        DesignConditionLoad designConditionLoad = energyCentre.DesignConditionLoad(designConditionName);
                        if (designConditionLoad != null)
                        {
                            sizedFlowVariable.AddDesignCondition(designConditionLoad);
                        }
                    }
                }
            }

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