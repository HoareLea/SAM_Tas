using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static TBD.IZAM IZAM(this TBD.Building building, Space space, string name, bool from, IEnumerable<TBD.zone> zones, IEnumerable<TBD.dayType> dayTypes)
        {
            string directionText = "To";
            if (from)
                directionText = "From";

            string name_Direction = null;
            if (!space.TryGetValue(string.Format("SAM_IZAM_{0}_Source", directionText), out name_Direction, true))
                return null;

            if (string.IsNullOrWhiteSpace(name_Direction))
                return null;

            TBD.IZAM iZAM = building.AddIZAM(null);

            foreach (TBD.dayType dayType in dayTypes)
            {
                iZAM.SetDayType(dayType, true);
            }

            TBD.zone zone_Source = null;
            TBD.zone zone_Assign = null;

            TBD.zone zone = zones.Match(name_Direction);
            if (zone != null)
            {
                if (from)
                {
                    iZAM.name = string.Format("IZAM_{0}_FROM_{1}", name, name_Direction);
                    zone_Source = zone;
                    zone_Assign = zones.Match(name);
                }
                else
                {
                    iZAM.name = string.Format("IZAM_{0}_FROM_{1}", name_Direction, name);
                    zone_Source = zones.Match(name);
                    zone_Assign = zone;
                }

            }
            else
            {
                iZAM.name = string.Format("IZAM_{0}_{1} OUTSIDE", name, directionText.ToUpper());
                zone_Assign = zones.Match(name);

                if (from)
                    iZAM.fromOutside = 1;
                else
                    iZAM.fromOutside = 0;
            }

            if (zone_Source != null)
                iZAM.SetSourceZone(zone_Source);

            if (zone_Assign != null)
                zone_Assign.AssignIZAM(iZAM, true);

            TBD.profile profile = iZAM.GetProfile();
            profile.type = TBD.ProfileTypes.ticValueProfile;

            double value;
            if (space.TryGetValue(string.Format("SAM_IZAM_{0}_Value", directionText), out value, true))
                profile.value = (float)value;

            double factor;
            if (space.TryGetValue(string.Format("SAM_IZAM_{0}_ValueFactor", directionText), out factor, true))
                profile.factor = (float)factor;

            double setbackValue;
            if (space.TryGetValue(string.Format("SAM_IZAM_{0}_ValueSetBack", directionText), out setbackValue, true))
                profile.setbackValue = (float)setbackValue;

            string scheduleValues = null;
            if (space.TryGetValue(string.Format("SAM_IZAM_{0}_Schedule", directionText), out scheduleValues, true))
            {
                string name_Schedule = string.Format("SAM_IZAM_{0}_Schedule_{1}", directionText, "IZAMSCHED");
                
                List<int> values = scheduleValues.Ints();
                
                TBD.schedule schedule = Schedule(building, name_Schedule, values);
                if (schedule != null)
                    profile.schedule = schedule;
            }


            return iZAM;
        }
    }
}