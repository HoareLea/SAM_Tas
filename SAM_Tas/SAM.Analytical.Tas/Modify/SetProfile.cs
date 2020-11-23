using SAM.Core.Tas;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateProfile(TBD.profile profile_TBD, Profile profile, double factor)
        {
            if (profile_TBD == null || profile == null || profile.Count == -1)
                return false;

            if(profile.Count == 1)
            {
                profile_TBD.type = TBD.ProfileTypes.ticValueProfile;
                profile_TBD.factor = System.Convert.ToSingle(factor);
                profile_TBD.value = System.Convert.ToSingle(profile.Values[0]);
                return true;
            }

            if(profile.Count == 24)
            {
                profile_TBD.type = TBD.ProfileTypes.ticHourlyProfile;
                profile_TBD.factor = System.Convert.ToSingle(factor);

                double[] values = profile.Values;
                for (int i = 0; i < values.Length; i++)
                    profile_TBD.hourlyValues[i] = System.Convert.ToSingle(values[i]);
            }

            return null;
        }
    }
}