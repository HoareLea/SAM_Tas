namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateProfile(TBD.profile profile_TBD, Profile profile, double factor)
        {
            if (profile_TBD == null || profile == null || profile.Count == -1)
                return false;

            profile_TBD.name = profile.Name;
            profile_TBD.description = profile.Name;

            if (profile.Count == 1)
            {
                profile_TBD.type = TBD.ProfileTypes.ticValueProfile;
                profile_TBD.factor = System.Convert.ToSingle(factor);
                profile_TBD.value = System.Convert.ToSingle(profile.GetValues()[0]);
                return true;
            }

            if(profile.Count <= 24)
            {
                profile_TBD.type = TBD.ProfileTypes.ticHourlyProfile;
                profile_TBD.factor = System.Convert.ToSingle(factor);

                for (int i = 0; i <= 23; i++)
                    profile_TBD.hourlyValues[i + 1] = System.Convert.ToSingle(profile[i]);

                return true;
            }

            profile_TBD.type = TBD.ProfileTypes.ticYearlyProfile;
            profile_TBD.factor = System.Convert.ToSingle(factor);

            //object yearlyValues_TBD = profile_TBD.GetYearlyValues();

            //float[] array = Query.Array<float>(yearlyValues_TBD);

            double[] yearlyValues =  profile.GetYearlyValues();
            float[] yearlyValues_float = new float[yearlyValues.Length];
            for (int i = 0; i < yearlyValues_float.Length; i++)
                yearlyValues_float[i] = System.Convert.ToSingle(yearlyValues[i]);

            profile_TBD.SetYearlyValues(yearlyValues_float);

            //for (int i = 0; i < 8759; i++)
            //    profile_TBD.yearlyValues[i] = System.Convert.ToSingle(profile[i]);

            return true;
        }
    }
}