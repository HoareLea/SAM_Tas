using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static IModifier ToSAM(this IEnumerable<ProfileDataModifier> profileDataModifiers)
        { 
            if(profileDataModifiers == null || profileDataModifiers.Count() == 0)
            {
                return null;
            }

            IModifier result = null;

            if(profileDataModifiers.Count() == 1)
            {
                ProfileDataModifier profileDataModifier = profileDataModifiers.ElementAt(0);

                return ToSAM(profileDataModifier);
            }
            else
            {
                List<IModifier> modifiers = new List<IModifier>();
                foreach(ProfileDataModifier profileDataModifier in profileDataModifiers)
                {
                    IModifier modifier = ToSAM(profileDataModifier);
                    if(modifier == null)
                    {
                        continue;
                    }

                    modifiers.Add(modifier);
                }

                return new ComplexModifier(modifiers);
            }
        }

        public static IModifier ToSAM(this ProfileDataModifier profileDataModifier)
        {
            if(profileDataModifier == null)
            {
                return null;
            }

            if (profileDataModifier is IProfileDataModifierCurve)
            {
                return ToSAM((IProfileDataModifierCurve)profileDataModifier);
            }

            if (profileDataModifier is IProfileDataModifierTable)
            {
                return ToSAM((IProfileDataModifierTable)profileDataModifier);
            }

            if (profileDataModifier is IProfileDataModifierHourly)
            {
                return ToSAM((IProfileDataModifierHourly)profileDataModifier);
            }

            if (profileDataModifier is IProfileDataModifierYearly)
            {
                return ToSAM((IProfileDataModifierYearly)profileDataModifier);
            }

            if (profileDataModifier is IProfileDataModifierLua)
            {
                return ToSAM((IProfileDataModifierLua)profileDataModifier);
            }

            return null;
        }

        public static PolynomialModifier ToSAM(IProfileDataModifierCurve profileDataModifierCurve)
        {
            if(profileDataModifierCurve == null)
            {
                return null; 
            }

            PolynomialModifier result = new PolynomialModifier(profileDataModifierCurve.Multiplier.ArithmeticOperator().Value, null);

            return result;
        }

        public static TableModifier ToSAM(IProfileDataModifierTable profileDataModifierTable)
        {
            if(profileDataModifierTable == null)
            {
                return null;
            }

            HashSet<string> names = new HashSet<string>();
            List<int> counts = new List<int>();
            int i = 1;
            while(profileDataModifierTable.GetAxisSize(i) > 0)
            {
                names.Add(profileDataModifierTable.GetVariable(i).ToString());
                counts.Add(profileDataModifierTable.GetAxisSize(i));
                i++;
            }

            while (counts.Count < 3)
            {
                counts.Add(1);
            }

            List<string> headers = new List<string>(names);
            headers.Add("value");

            TableModifier result = new TableModifier(profileDataModifierTable.Multiplier.ArithmeticOperator().Value, headers);
            result.Extrapolate = profileDataModifierTable.Extrapolate == 1;

            if (counts[0] > 0 && names.Count > 0)
            {
                for (int x = 1; x <= counts[0]; x++)
                {
                    for (int y = 1; y <= counts[1]; y++)
                    {
                        for (int z = 1; z <= counts[2]; z++)
                        {
                            Dictionary<string, double> dictionary = new Dictionary<string, double>();
                            dictionary[names.ElementAt(0)] = profileDataModifierTable.GetAxisValue(1, x);
                            if (names.Count > 1)
                            {
                                dictionary[names.ElementAt(1)] = profileDataModifierTable.GetAxisValue(2, y);
                                if (names.Count > 2)
                                {
                                    dictionary[names.ElementAt(2)] = profileDataModifierTable.GetAxisValue(3, z);
                                }
                            }

                            dictionary["value"] = profileDataModifierTable.GetDataValue(x, y, z);
                            result.AddValues(dictionary);
                        }
                    }
                }
            }

            return result;
        }

        public static ProfileModifier ToSAM(IProfileDataModifierHourly profileDataModifierHourly)
        {
            if (profileDataModifierHourly == null)
            {
                return null;
            }

            ProfileModifier result = new ProfileModifier(profileDataModifierHourly.Multiplier.ArithmeticOperator().Value, null);

            return result;
        }

        public static ProfileModifier ToSAM(IProfileDataModifierYearly profileDataModifierYearly)
        {
            if (profileDataModifierYearly == null)
            {
                return null;
            }

            ProfileModifier result = new ProfileModifier(profileDataModifierYearly.Multiplier.ArithmeticOperator().Value, null);

            return result;
        }

        public static LuaModifier ToSAM(IProfileDataModifierLua profileDataModifierLua)
        {
            if (profileDataModifierLua == null)
            {
                return null;
            }

            LuaModifier result = new LuaModifier(profileDataModifierLua.Multiplier.ArithmeticOperator().Value, null);

            return result;
        }
    }
}