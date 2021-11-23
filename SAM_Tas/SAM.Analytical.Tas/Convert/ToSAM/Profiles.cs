using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<Profile> ToSAM_Profiles(this TBD.InternalCondition internalCondition)
        {
            if(internalCondition == null)
            {
                return null;
            }

            List<Profile> result = new List<Profile>();

            TBD.InternalGain internalGain = internalCondition.GetInternalGain();
            if(internalGain != null)
            {
                Profile profile = null;

                profile = ToSAM(internalGain, TBD.Profiles.ticI, ProfileType.Infiltration);
                if(profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(internalGain, TBD.Profiles.ticLG, ProfileType.Lighting);
                if (profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(internalGain, TBD.Profiles.ticOLG, ProfileType.Occupancy);
                if (profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(internalGain, TBD.Profiles.ticOSG, ProfileType.Occupancy);
                if (profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(internalGain, TBD.Profiles.ticESG, ProfileType.EquipmentSensible);
                if (profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(internalGain, TBD.Profiles.ticELG, ProfileType.EquipmentLatent);
                if (profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(internalGain, TBD.Profiles.ticCOG, ProfileType.Pollutant);
                if (profile != null)
                {
                    result.Add(profile);
                }
            }

            TBD.Thermostat thermostat = internalCondition.GetThermostat();
            if(thermostat != null)
            {
                Profile profile = null;

                profile = ToSAM(thermostat, TBD.Profiles.ticUL, ProfileType.Cooling);
                if (profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(thermostat, TBD.Profiles.ticLL, ProfileType.Heating);
                if (profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(thermostat, TBD.Profiles.ticHLL, ProfileType.Humidification);
                if (profile != null)
                {
                    result.Add(profile);
                }

                profile = ToSAM(thermostat, TBD.Profiles.ticHUL, ProfileType.Dehumidification);
                if (profile != null)
                {
                    result.Add(profile);
                }

            }

            return result;
        }

        public static List<Profile> ToSAM_Profiles(this TBD.Building building)
        {
            List<TBD.InternalCondition> internalConditions_TBD = building?.InternalConditions();
            if(internalConditions_TBD == null|| internalConditions_TBD.Count == 0)
            {
                return null;
            }

            List<Profile> result = new List<Profile>();
            foreach(TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
            {
                List<Profile> profiles = internalCondition_TBD?.ToSAM_Profiles();
                if(profiles == null || profiles.Count ==0 )
                {
                    continue;
                }

                profiles.ForEach(x => result.Add(x));
            }


            return result;
        }
    }
}
