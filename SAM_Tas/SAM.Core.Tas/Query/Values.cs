using System.Collections.Generic;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static List<double> Values(this TBD.profile profile)
        {
            if(profile == null)
            {
                return null;
            }

            List<double> result = new List<double>();
            switch (profile.type)
            {
                case TBD.ProfileTypes.ticValueProfile:
                    result.Add(profile.value);
                    break;

                case TBD.ProfileTypes.ticHourlyProfile:
                    for (int i = 0; i <= 23; i++)
                        result.Add(profile.hourlyValues[i + 1]);
                    break;

                case TBD.ProfileTypes.ticYearlyProfile:
                    for (int i = 0; i <= 8759; i++)
                        result.Add(profile.yearlyValues[i + 1]);
                    break;
            }

            return result;
        }
    }
}