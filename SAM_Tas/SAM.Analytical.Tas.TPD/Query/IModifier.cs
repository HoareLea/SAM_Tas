using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<ProfileDataModifier> ProfileDataModifiers(this IProfileData profileData)
        { 
            if(profileData == null)
            {
                return null;
            }

            int index = 1;

            List<ProfileDataModifier> result = new List<ProfileDataModifier>();

            int count = profileData.GetModifierCount();
            for (int i = index; i <= count; i++)
            {
                ProfileDataModifier profileDataModifier = profileData.GetModifier(i);
                if(profileDataModifier != null)
                {
                    result.Add(profileDataModifier);
                }
            }

            return result;

        }
    }
}