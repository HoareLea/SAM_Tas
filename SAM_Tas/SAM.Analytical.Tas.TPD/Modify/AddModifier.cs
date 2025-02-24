using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool AddModifier(this ProfileData profileData, IModifier modifier, EnergyCentre energyCentre)
        {
            if(profileData == null || modifier == null)
            {
                return false;
            }

            bool result = false;

            if (modifier is ISimpleModifier)
            {
                return AddModifier(profileData, (ISimpleModifier)modifier, energyCentre);
            }
            else if (modifier is ComplexModifier)
            {
                result = false;

                List<IModifier> modifiers = ((ComplexModifier)modifier).Modifiers;
                if(modifiers != null)
                {
                    foreach(IModifier modifier_Temp in modifiers)
                    {
                        bool added = AddModifier(profileData, modifier_Temp, energyCentre);
                        if(added)
                        {
                            result = true;
                        }
                    }
                }

            }
            else
            {
                throw new System.NotImplementedException();
            }

            return result;
        }

        public static bool AddModifier(this ProfileData profileData, ISimpleModifier simpleModifier, EnergyCentre energyCentre)
        {
            if(profileData == null || simpleModifier == null)
            {
                return false;
            }

            if (simpleModifier is CurveModifier)
            {
                return profileData.AddModifier((CurveModifier)simpleModifier, energyCentre);
            }

            if (simpleModifier is TableModifier)
            {
                return profileData.AddModifier((TableModifier)simpleModifier, energyCentre);
            }

            if (simpleModifier is DailyModifier)
            {
                return profileData.AddModifier((DailyModifier)simpleModifier, energyCentre);
            }

            if (simpleModifier is IndexedDoublesModifier)
            {
                return profileData.AddModifier((IndexedDoublesModifier)simpleModifier, energyCentre);
            }

            if (simpleModifier is LuaModifier)
            {
                return profileData.AddModifier((LuaModifier)simpleModifier, energyCentre);
            }

            if (simpleModifier is ScheduleModifier)
            {
                return profileData.AddModifier((ScheduleModifier)simpleModifier, energyCentre);
            }

            return false;
        }

        public static bool AddModifier(this ProfileData profileData, CurveModifier curveModifier, EnergyCentre energyCentre)
        {
            if (profileData == null || curveModifier == null)
            {
                return false;
            }

            ProfileDataModifierCurve result = profileData.AddModifierCurve();
            result.Multiplier = curveModifier.ArithmeticOperator.ToTPD();

            result.Name = curveModifier.Name;
            result.CurveType = curveModifier.CurveModifierType.ToTPD();

            CurveModifierVariableType[] curveModifierVariableTypes = curveModifier.CurveModifierVariableTypes;
            for (int i = 0; i < curveModifierVariableTypes.Length; i++)
            {
                result.SetVariable(i + 1, curveModifierVariableTypes[i].ToTPD());
            }

            double[] parameters = curveModifier.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                result.SetParameter(i + 1, parameters[i]);
            }

            return true;
        }

        public static bool AddModifier(this ProfileData profileData, TableModifier tableModifier, EnergyCentre energyCentre)
        {
            if (profileData == null || tableModifier == null)
            {
                return false;
            }

            ProfileDataModifierTable profileDataModifierTable = profileData.AddModifierTable();
            profileDataModifierTable.Multiplier = tableModifier.ArithmeticOperator.ToTPD();
            profileDataModifierTable.Clear();

            IEnumerable<string> headers = tableModifier.Headers;

            if (headers.Count() == 2)
            {
                //profileDataModifierTable.Multiplier = tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;

                if (System.Enum.TryParse(headers.ElementAt(0), true, out tpdProfileDataVariableType tpdProfileDataVariableType))
                {
                    profileDataModifierTable.SetVariable(1, tpdProfileDataVariableType);
                }

                for (int i = 0; i < tableModifier.RowCount; i++)
                {
                    Dictionary<int, double> values = tableModifier.GetDictionary(i);
                    if(values == null || !values.ContainsKey(0) || !values.ContainsKey(1))
                    {
                        continue;
                    }

                    profileDataModifierTable.AddPoint(values[0], values[1]);
                }
            }
            else
            {
                //TODO: Check and validate

                int count_x = tableModifier.RowCount;
                if (count_x == -1)
                {
                    count_x = 0;
                }

                List<double> columnValues = null;

                columnValues = tableModifier.GetColumnValues(1)?.Distinct()?.ToList();
                int count_y = columnValues == null ? 0 : columnValues.Count;

                columnValues = tableModifier.GetColumnValues(2)?.Distinct()?.ToList();
                int count_z = columnValues == null ? 0 : columnValues.Count;

                profileDataModifierTable.SetSize(count_x, count_y, count_z);

                int columnCount = -1;

                if (headers != null)
                {
                    columnCount = headers.Count();

                    for (int i = 0; i < columnCount - 1; i++)
                    {
                        string text = headers.ElementAt(i);
                        if (System.Enum.TryParse(text, true, out tpdProfileDataVariableType tpdProfileDataVariableType))
                        {
                            profileDataModifierTable.SetVariable(i + 1, tpdProfileDataVariableType);
                        }
                    }
                }

                if (count_x == 0)
                {
                    count_x++;
                }

                if (count_y == 0)
                {
                    count_y++;
                }

                if (count_z == 0)
                {
                    count_z++;
                }

                for (int i = 0; i < tableModifier.RowCount; i++)
                {
                    Dictionary<int, double> values = tableModifier.GetDictionary(i);

                    int x = i + 1;
                    int y = 1;
                    int z = 1;

                    if (columnCount > 1)
                    {
                        profileDataModifierTable.SetAxisValue(1, i + 1, values[0]);
                        if (columnCount > 2)
                        {
                            profileDataModifierTable.SetAxisValue(2, i + 1, values[1]);
                            if (columnCount > 3)
                            {
                                profileDataModifierTable.SetAxisValue(3, i + 1, values[2]);
                            }
                        }
                    }

                    profileDataModifierTable.SetDataValue(x, y, z, values[values.Keys.Max()]);
                }
            }

            return true;
        }

        public static bool AddModifier(this ProfileData profileData, DailyModifier dailyModifier, EnergyCentre energyCentre)
        {
            if (profileData == null || dailyModifier == null)
            {
                return false;
            }

            ProfileDataModifierHourly profileDataModifierHourly = profileData.AddModifierHourly();
            profileDataModifierHourly.Multiplier = dailyModifier.ArithmeticOperator.ToTPD();

            int index = 1;

            ProfileDataModifierHourlyDay profileDataModifierHourlyDay = profileDataModifierHourly.GetDay(index);
            while (profileDataModifierHourlyDay != null)
            {
                string name = profileDataModifierHourlyDay.GetDayType().Name;

                for (int i = 0; i < 24; i++)
                {
                    double value = dailyModifier.GetValue(name, i);
                    if (!double.IsNaN(value))
                    {
                        profileDataModifierHourlyDay.SetValue(i + 1, value);
                    }
                }

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

            return true;
        }

        public static bool AddModifier(this ProfileData profileData, IndexedDoublesModifier indexedDoublesModifier, EnergyCentre energyCentre)
        {
            if (profileData == null || indexedDoublesModifier == null)
            {
                return false;
            }

            ProfileDataModifierYearly profileDataModifierYearly = profileData.AddModifierYearly();
            profileDataModifierYearly.Multiplier = indexedDoublesModifier.ArithmeticOperator.ToTPD();

            for(int i = 0; i < 8760; i++)
            {
                if(indexedDoublesModifier.Values.TryGetValue(i, out double value))
                {
                    profileDataModifierYearly.SetYearlyValue(i + 1, value);
                }
            }

            return true;
        }

        public static bool AddModifier(this ProfileData profileData, LuaModifier luaModifier, EnergyCentre energyCentre)
        {
            if (profileData == null || luaModifier == null)
            {
                return false;
            }

            ProfileDataModifierLua profileDataModifierLua = profileData.AddModifierLua();
            profileDataModifierLua.Multiplier = luaModifier.ArithmeticOperator.ToTPD();

            return true;
        }

        public static bool AddModifier(this ProfileData profileData, ScheduleModifier scheduleModifier, EnergyCentre energyCentre)
        {
            if (profileData == null || scheduleModifier == null)
            {
                return false;
            }

            ProfileDataModifierSchedule profileDataModifierSchedule = profileData.AddModifierSchedule();
            profileDataModifierSchedule.Multiplier = scheduleModifier.ArithmeticOperator.ToTPD();

            profileDataModifierSchedule.Setback = scheduleModifier.Setback;

            profileDataModifierSchedule.Schedule = energyCentre.PlantSchedule(scheduleModifier.Schedule?.Name);

            return true;
        }

    }
}