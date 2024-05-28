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

            if(profileDataModifier is IProfileDataModifierCurve)
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

        public static IndexedModifier ToSAM(IProfileDataModifierTable profileDataModifierTable)
        {
            if(profileDataModifierTable == null)
            {
                return null;
            }

            IndexedModifier result = new IndexedModifier(profileDataModifierTable.Multiplier.ArithmeticOperator().Value, null);

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
    }
}