using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Profile ToSAM(this TBD.InternalGain internalGain, TBD.Profiles profiles, ProfileType profileType, string prefix = null)
        {
            if(internalGain == null)
            {
                return null;
            }

            TBD.profile profile = internalGain.GetProfile((int)profiles);
            if(profile == null)
            {
                return null;
            }

            List<double> values = Core.Tas.Query.Values(profile);
            if(values == null)
            {
                return null;
            }

            string name = null;
            if(string.IsNullOrEmpty(prefix))
            {
                name = profile.name;
            }
            else
            {
                name = string.Format("{0} [{1}]", prefix, profile.name);
            }

            Profile result = new Profile(name, profileType, values);
            return result;
        }

        public static Profile ToSAM(this TBD.Thermostat thermostat, TBD.Profiles profiles, ProfileType profileType, string prefix = null)
        {
            if (thermostat == null)
            {
                return null;
            }

            TBD.profile profile = thermostat.GetProfile((int)profiles);
            if (profile == null)
            {
                return null;
            }

            List<double> values = Core.Tas.Query.Values(profile);
            if (values == null)
            {
                return null;
            }

            string name = null;
            if (string.IsNullOrEmpty(prefix))
            {
                name = profile.name;
            }
            else
            {
                name = string.Format("{0} [{1}]", prefix, profile.name);
            }

            Profile result = new Profile(name, profileType, values);
            return result;
        }
    }
}
