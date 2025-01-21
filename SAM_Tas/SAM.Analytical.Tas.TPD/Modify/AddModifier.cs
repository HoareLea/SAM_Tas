using SAM.Core;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool AddModifier(this ProfileData profileData, IModifier modifier)
        {
            if(profileData == null || modifier == null)
            {
                return false;
            }

            bool result = false;

            if (modifier is ISimpleModifier)
            {
                return AddModifier(profileData, (ISimpleModifier)modifier);
            }
            else if (modifier is ComplexModifier)
            {
                result = false;

                List<IModifier> modifiers = ((ComplexModifier)modifier).Modifiers;
                if(modifiers != null)
                {
                    foreach(IModifier modifier_Temp in modifiers)
                    {
                        bool added = AddModifier(profileData, modifier_Temp);
                        if(added)
                        {
                            result = true;
                        }
                    }
                }

            }
            else
            {
                throw new System.NotImplementedException();
            }

            return result;
        }

        public static bool AddModifier(this ProfileData profileData, ISimpleModifier simpleModifier)
        {
            if(profileData == null || simpleModifier == null)
            {
                return false;
            }

            if (simpleModifier is PolynomialModifier)
            {
                return profileData.AddModifier((PolynomialModifier)simpleModifier);
            }

            if (simpleModifier is TableModifier)
            {
                return profileData.AddModifier((TableModifier)simpleModifier);
            }

            if (simpleModifier is ProfileModifier)
            {
                return profileData.AddModifier((ProfileModifier)simpleModifier);
            }

            if (simpleModifier is LuaModifier)
            {
                return profileData.AddModifier((LuaModifier)simpleModifier);
            }

            return false;
        }

        public static bool AddModifier(this ProfileData profileData, PolynomialModifier polynomialModifier)
        {
            if (profileData == null || polynomialModifier == null)
            {
                return false;
            }

            ProfileDataModifierCurve profileDataModifierCurve = profileData.AddModifierCurve();
            profileDataModifierCurve.Multiplier = polynomialModifier.ArithmeticOperator.ToTPD();
            
            
            return true;
        }

        public static bool AddModifier(this ProfileData profileData, TableModifier tableModifier)
        {
            if (profileData == null || tableModifier == null)
            {
                return false;
            }

            ProfileDataModifierTable profileDataModifierTable = profileData.AddModifierTable();
            profileDataModifierTable.Multiplier = tableModifier.ArithmeticOperator.ToTPD();


            return true;
        }

        public static bool AddModifier(this ProfileData profileData, ProfileModifier profileModifier)
        {
            if (profileData == null || profileModifier == null)
            {
                return false;
            }

            ProfileDataModifierHourly profileDataModifierHourly = profileData.AddModifierHourly();
            profileDataModifierHourly.Multiplier = profileModifier.ArithmeticOperator.ToTPD();

            return true;
        }

        public static bool AddModifier(this ProfileData profileData, LuaModifier luaModifier)
        {
            if (profileData == null || luaModifier == null)
            {
                return false;
            }

            ProfileDataModifierLua profileDataModifierLua = profileData.AddModifierLua();
            profileDataModifierLua.Multiplier = luaModifier.ArithmeticOperator.ToTPD();

            return true;
        }

    }
}