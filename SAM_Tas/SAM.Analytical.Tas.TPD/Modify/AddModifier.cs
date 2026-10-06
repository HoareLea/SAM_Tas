// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using System;
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

            if (!TryGetTableData(tableModifier, out TableData tableData))
            {
                return false;
            }

            ProfileDataModifierTable profileDataModifierTable = profileData.AddModifierTable();
            if (profileDataModifierTable == null)
            {
                return false;
            }

            profileDataModifierTable.Multiplier = tableModifier.ArithmeticOperator.ToTPD();
            profileDataModifierTable.Clear();
            profileDataModifierTable.Extrapolate = tableModifier.Extrapolate ? -1 : 0;

            if (tableData.VariableTypes.Count == 1)
            {
                profileDataModifierTable.SetVariable(1, tableData.VariableTypes[0]);
                foreach (Dictionary<int, double> row in tableData.Rows)
                {
                    profileDataModifierTable.AddPoint(row[0], row[1]);
                }

                return true;
            }

            int count_X = tableData.AxisValues[0].Count;
            int count_Y = tableData.AxisValues[1].Count;
            int count_Z = tableData.VariableTypes.Count == 3 ? tableData.AxisValues[2].Count : 0;
            profileDataModifierTable.SetSize(count_X, count_Y, count_Z);

            for (int axis = 0; axis < tableData.VariableTypes.Count; axis++)
            {
                profileDataModifierTable.SetVariable(axis + 1, tableData.VariableTypes[axis]);
                for (int index = 0; index < tableData.AxisValues[axis].Count; index++)
                {
                    profileDataModifierTable.SetAxisValue(axis + 1, index + 1, tableData.AxisValues[axis][index]);
                }
            }

            foreach (Dictionary<int, double> row in tableData.Rows)
            {
                int x = tableData.AxisIndexes[0][row[0]] + 1;
                int y = tableData.AxisIndexes[1][row[1]] + 1;
                int z = tableData.VariableTypes.Count == 3 ? tableData.AxisIndexes[2][row[2]] + 1 : 1;
                profileDataModifierTable.SetDataValue(x, y, z, row[tableData.VariableTypes.Count]);
            }

            return true;
        }

        private sealed class TableData
        {
            public List<tpdProfileDataVariableType> VariableTypes { get; } = new List<tpdProfileDataVariableType>();
            public List<List<double>> AxisValues { get; } = new List<List<double>>();
            public List<Dictionary<double, int>> AxisIndexes { get; } = new List<Dictionary<double, int>>();
            public List<Dictionary<int, double>> Rows { get; } = new List<Dictionary<int, double>>();
        }

        /// <summary>
        /// A table axis header as a TAS profile variable: either the TAS name itself - what
        /// <c>Convert.ToSAM(ProfileDataModifier)</c> writes on a round trip - or SAM's own
        /// <see cref="CurveModifierVariableType"/> name, which is how a SAM_Systems graph states a variable
        /// without naming anything of TAS's (PR5B, SAM#111).
        /// </summary>
        private static bool TryGetVariableType(string header, out tpdProfileDataVariableType variableType)
        {
            if (Enum.TryParse(header, true, out variableType) && Enum.IsDefined(typeof(tpdProfileDataVariableType), variableType))
            {
                return true;
            }

            if (Enum.TryParse(header, false, out CurveModifierVariableType curveModifierVariableType) && Enum.IsDefined(typeof(CurveModifierVariableType), curveModifierVariableType))
            {
                try
                {
                    variableType = curveModifierVariableType.ToTPD();
                    return true;
                }
                catch (NotImplementedException)
                {
                    //A SAM variable TAS has no counterpart for: not a table TAS can be given.
                }
            }

            variableType = default;
            return false;
        }

        private static bool TryGetTableData(TableModifier tableModifier, out TableData tableData)
        {
            tableData = null;

            List<string> headers = tableModifier?.Headers?.ToList();
            if (headers == null || headers.Count < 2 || headers.Count > 4 || tableModifier.RowCount <= 0)
            {
                return false;
            }

            TableData result = new TableData();
            HashSet<tpdProfileDataVariableType> variableTypes = new HashSet<tpdProfileDataVariableType>();
            int axisCount = headers.Count - 1;
            for (int axis = 0; axis < axisCount; axis++)
            {
                if (string.IsNullOrWhiteSpace(headers[axis])
                    || !TryGetVariableType(headers[axis], out tpdProfileDataVariableType variableType)
                    || variableType == tpdProfileDataVariableType.tpdProfileDataVariableLAST
                    || !variableTypes.Add(variableType))
                {
                    return false;
                }

                result.VariableTypes.Add(variableType);
                result.AxisValues.Add(new List<double>());
                result.AxisIndexes.Add(new Dictionary<double, int>());
            }

            for (int rowIndex = 0; rowIndex < tableModifier.RowCount; rowIndex++)
            {
                Dictionary<int, double> row = tableModifier.GetDictionary(rowIndex);
                if (row == null)
                {
                    return false;
                }

                for (int column = 0; column <= axisCount; column++)
                {
                    if (!row.TryGetValue(column, out double value) || double.IsNaN(value) || double.IsInfinity(value))
                    {
                        return false;
                    }

                    if (column < axisCount && !result.AxisIndexes[column].ContainsKey(value))
                    {
                        result.AxisIndexes[column][value] = result.AxisValues[column].Count;
                        result.AxisValues[column].Add(value);
                    }
                }

                result.Rows.Add(row);
            }

            int expectedRowCount = 1;
            foreach (List<double> axisValues in result.AxisValues)
            {
                if (axisValues.Count == 0)
                {
                    return false;
                }

                expectedRowCount *= axisValues.Count;
            }

            if (expectedRowCount != result.Rows.Count)
            {
                return false;
            }

            HashSet<Tuple<int, int, int>> coordinates = new HashSet<Tuple<int, int, int>>();
            foreach (Dictionary<int, double> row in result.Rows)
            {
                int x = result.AxisIndexes[0][row[0]];
                int y = axisCount > 1 ? result.AxisIndexes[1][row[1]] : 0;
                int z = axisCount > 2 ? result.AxisIndexes[2][row[2]] : 0;
                if (!coordinates.Add(Tuple.Create(x, y, z)))
                {
                    return false;
                }
            }

            tableData = result;
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
