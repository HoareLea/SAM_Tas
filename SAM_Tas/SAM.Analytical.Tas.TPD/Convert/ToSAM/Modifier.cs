using SAM.Analytical.Systems;
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

            if (profileDataModifier is IProfileDataModifierCurve)
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

            if (profileDataModifier is IProfileDataModifierLua)
            {
                return ToSAM((IProfileDataModifierLua)profileDataModifier);
            }

            if (profileDataModifier is IProfileDataModifierSchedule)
            {
                return ToSAM((IProfileDataModifierSchedule)profileDataModifier);
            }

            return null;
        }

        public static CurveModifier ToSAM(IProfileDataModifierCurve profileDataModifierCurve)
        {
            if(profileDataModifierCurve == null)
            {
                return null; 
            }

            CurveModifierType curveModifierType = profileDataModifierCurve.CurveType.ToSAM();

            int count_Parameters = -1;
            int count_Variables = -1;

            switch (curveModifierType)
            {
                case CurveModifierType.Linear:
                    count_Variables = 1;
                    count_Parameters = 2;
                    break;

                case CurveModifierType.BiLinear:
                    count_Variables = 2;
                    count_Parameters = 3;
                    break;

                case CurveModifierType.TriLinear:
                    count_Variables = 3;
                    count_Parameters = 4;
                    break;

                case CurveModifierType.Quadratic:
                    count_Variables = 1;
                    count_Parameters = 3;
                    break;

                case CurveModifierType.BiQuadratic:
                    count_Variables = 2;
                    count_Parameters = 6;
                    break;

                case CurveModifierType.TriQuadratic:
                    count_Variables = 3;
                    count_Parameters = 10;
                    break;

                case CurveModifierType.Cubic:
                    count_Variables = 1;
                    count_Parameters = 4;
                    break;

                case CurveModifierType.BiCubic:
                    count_Variables = 2;
                    count_Parameters = 10;
                    break;
            }

            if(count_Variables == -1 || count_Parameters == -1)
            {
                return null;
            }

            CurveModifierVariableType[] curveModifierVariableTypes = new CurveModifierVariableType[count_Variables];
            for(int i =0; i < curveModifierVariableTypes.Length; i++)
            {
                curveModifierVariableTypes[i] = profileDataModifierCurve.GetVariable(i + 1).ToSAM();
            }

            double[] parameters = new double[count_Parameters];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = profileDataModifierCurve.GetParameter(i + 1);
            }

            CurveModifier result = new CurveModifier(profileDataModifierCurve.Multiplier.ArithmeticOperator().Value, profileDataModifierCurve.Name, curveModifierType, curveModifierVariableTypes.ToArray(), parameters.ToArray());

            return result;
        }

        public static ScheduleModifier ToSAM(IProfileDataModifierSchedule profileDataModifierSchedule)
        {
            if (profileDataModifierSchedule == null)
            {
                return null;
            }

            ScheduleModifier result = new ScheduleModifier(profileDataModifierSchedule.Multiplier.ArithmeticOperator().Value, profileDataModifierSchedule.Schedule.ToSAM(), profileDataModifierSchedule.Setback);

            return result;
        }

        public static TableModifier ToSAM(IProfileDataModifierTable profileDataModifierTable)
        {
            if(profileDataModifierTable == null)
            {
                return null;
            }

            HashSet<string> names = new HashSet<string>();
            List<int> counts = new List<int>();
            int i = 1;
            while(profileDataModifierTable.GetAxisSize(i) > 0)
            {
                names.Add(profileDataModifierTable.GetVariable(i).ToString());
                counts.Add(profileDataModifierTable.GetAxisSize(i));
                i++;
            }

            while (counts.Count < 3)
            {
                counts.Add(1);
            }

            List<string> headers = new List<string>(names);
            headers.Add("value");

            TableModifier result = new TableModifier(profileDataModifierTable.Multiplier.ArithmeticOperator().Value, headers);
            result.Extrapolate = profileDataModifierTable.Extrapolate == 1;

            if (counts[0] > 0 && names.Count > 0)
            {
                for (int x = 1; x <= counts[0]; x++)
                {
                    for (int y = 1; y <= counts[1]; y++)
                    {
                        for (int z = 1; z <= counts[2]; z++)
                        {
                            Dictionary<string, double> dictionary = new Dictionary<string, double>();
                            dictionary[names.ElementAt(0)] = profileDataModifierTable.GetAxisValue(1, x);
                            if (names.Count > 1)
                            {
                                dictionary[names.ElementAt(1)] = profileDataModifierTable.GetAxisValue(2, y);
                                if (names.Count > 2)
                                {
                                    dictionary[names.ElementAt(2)] = profileDataModifierTable.GetAxisValue(3, z);
                                }
                            }

                            dictionary["value"] = profileDataModifierTable.GetDataValue(x, y, z);
                            result.AddValues(dictionary);
                        }
                    }
                }
            }

            return result;
        }

        public static DailyModifier ToSAM(IProfileDataModifierHourly profileDataModifierHourly)
        {
            if (profileDataModifierHourly == null)
            {
                return null;
            }

            Dictionary<string, double[]> values = new Dictionary<string, double[]>();

            int index = 1;
            ProfileDataModifierHourlyDay profileDataModifierHourlyDay = profileDataModifierHourly.GetDay(index);
            while(profileDataModifierHourlyDay != null)
            {
                double[] values_Day = new double[24];
                for (int i = 1; i <= 24; i++)
                {
                    values_Day[i - 1] = profileDataModifierHourlyDay.GetValue(i);
                }

                values[profileDataModifierHourlyDay.GetDayType().Name] = values_Day; 

                index++;
                try
                {
                    profileDataModifierHourlyDay = profileDataModifierHourly.GetDay(index);
                }
                catch
                {
                    profileDataModifierHourlyDay = null;
                }
            }

            DailyModifier result = new DailyModifier(profileDataModifierHourly.Multiplier.ArithmeticOperator().Value, values);

            return result;
        }

        public static IndexedDoublesModifier ToSAM(IProfileDataModifierYearly profileDataModifierYearly)
        {
            if (profileDataModifierYearly == null)
            {
                return null;
            }

            List<double> values = new List<double>();
            for(int i = 1; i <= 8760; i++)
            {
                values.Add(profileDataModifierYearly.GetYearlyValue(i));
            }

            IndexedDoublesModifier result = new IndexedDoublesModifier(profileDataModifierYearly.Multiplier.ArithmeticOperator().Value, new IndexedDoubles(values));

            return result;
        }

        public static LuaModifier ToSAM(IProfileDataModifierLua profileDataModifierLua)
        {
            if (profileDataModifierLua == null)
            {
                return null;
            }

            LuaModifier result = new LuaModifier(profileDataModifierLua.Multiplier.ArithmeticOperator().Value, profileDataModifierLua.Code);

            return result;
        }
    }
}