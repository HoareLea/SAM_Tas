using SAM.Core;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ModifiableValue ToSAM(this IProfileData profileData)
        {
            if (profileData == null)
            {
                return null;
            }

            double value = profileData.Value;

            IModifier modifier = null;

            List<ProfileDataModifier> profileDataModifiers = Query.ProfileDataModifiers(profileData);
            if(profileDataModifiers != null && profileDataModifiers.Count != 0)
            {
                modifier = ToSAM(profileDataModifiers);
            }

            return new ModifiableValue(modifier, value);
        }
    }
}
