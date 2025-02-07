using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Distribution ToSAM(this IProfileData profileData, bool isEfficiency)
        {
            if (profileData == null)
            {
                return null;
            }

            double value = profileData.Value;

            IModifier modifier = null;

            List<ProfileDataModifier> profileDataModifiers = Query.ProfileDataModifiers(profileData);
            if (profileDataModifiers != null && profileDataModifiers.Count != 0)
            {
                modifier = ToSAM(profileDataModifiers);
            }

            return new Distribution(modifier, value, isEfficiency);
        }
    }
}

