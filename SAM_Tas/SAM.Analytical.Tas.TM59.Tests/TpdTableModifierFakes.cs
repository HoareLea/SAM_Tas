// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    internal sealed class FakeTpdProfileData : global::TPD.ProfileData
    {
        private int modifierCount;

        public double Value { get; set; }

        public FakeTpdTableModifier Table { get; private set; }

        public global::TPD.ProfileDataModifierTable AddModifierTable()
        {
            Table = new FakeTpdTableModifier(this);
            modifierCount++;
            return Table;
        }

        public void ClearModifiers()
        {
            modifierCount = 0;
            Table = null;
        }

        public int GetModifierCount()
        {
            return modifierCount;
        }

        public global::TPD.ProfileDataModifier GetModifier(int index)
        {
            return null;
        }

        public global::TPD.tpdProfileDataModifierType GetModifierType(int index)
        {
            return index >= 1 && index <= modifierCount
                ? global::TPD.tpdProfileDataModifierType.tpdProfileDataModifierTable
                : default;
        }

        public void RemoveModifier(int index)
        {
            if (index >= 1 && index <= modifierCount)
            {
                modifierCount--;
            }
        }

        public global::TPD.ProfileDataModifierCurve AddModifierCurve() => throw new NotSupportedException();
        public global::TPD.ProfileDataModifierHourly AddModifierHourly() => throw new NotSupportedException();
        public global::TPD.ProfileDataModifierLua AddModifierLua() => throw new NotSupportedException();
        public global::TPD.ProfileDataModifierSchedule AddModifierSchedule() => throw new NotSupportedException();
        public global::TPD.ProfileDataModifierYearly AddModifierYearly() => throw new NotSupportedException();
    }

    internal sealed class FakeTpdTableModifier : global::TPD.ProfileDataModifierTable
    {
        private readonly global::TPD.ProfileData profile;
        private readonly int[] sizes = new int[3];
        private readonly global::TPD.tpdProfileDataVariableType[] variables = new global::TPD.tpdProfileDataVariableType[3];
        private readonly List<double>[] axes = { new List<double>(), new List<double>(), new List<double>() };
        private readonly Dictionary<Tuple<int, int, int>, double> data = new Dictionary<Tuple<int, int, int>, double>();

        public FakeTpdTableModifier(global::TPD.ProfileData profile = null)
        {
            this.profile = profile;
        }

        public int Extrapolate { get; set; }
        public global::TPD.tpdProfileDataModifierMultiplier Multiplier { get; set; }
        public string Name { get; set; }
        public int PopulatedCount => data.Count;

        public void AddPoint(double x, double y)
        {
            if (sizes[0] == 0)
            {
                sizes[0] = 1;
                axes[0].Add(x);
            }
            else
            {
                sizes[0]++;
                axes[0].Add(x);
            }

            data[Tuple.Create(sizes[0], 1, 1)] = y;
        }

        public void Clear()
        {
            for (int axis = 0; axis < 3; axis++)
            {
                sizes[axis] = 0;
                axes[axis].Clear();
            }

            data.Clear();
        }

        public int GetAxisSize(int axis)
        {
            return axis < 1 || axis > 3 ? 0 : sizes[axis - 1];
        }

        public double GetAxisValue(int axis, int index)
        {
            return axes[axis - 1][index - 1];
        }

        public double GetDataValue(int x, int y, int z)
        {
            return data.TryGetValue(Tuple.Create(x, y, z), out double value) ? value : 0;
        }

        public global::TPD.ProfileData GetProfile()
        {
            return profile;
        }

        public global::TPD.tpdProfileDataVariableType GetVariable(int axis)
        {
            return variables[axis - 1];
        }

        public void SetAxisValue(int axis, int index, double val)
        {
            List<double> values = axes[axis - 1];
            while (values.Count < sizes[axis - 1])
            {
                values.Add(0);
            }

            values[index - 1] = val;
        }

        public void SetDataValue(int x, int y, int z, double val)
        {
            data[Tuple.Create(x, y, z)] = val;
        }

        public void SetSize(int x, int y, int z)
        {
            sizes[0] = x;
            sizes[1] = y;
            sizes[2] = z;
            for (int axis = 0; axis < 3; axis++)
            {
                axes[axis].Clear();
                for (int index = 0; index < sizes[axis]; index++)
                {
                    axes[axis].Add(0);
                }
            }

            data.Clear();
        }

        public void SetVariable(int axis, global::TPD.tpdProfileDataVariableType type)
        {
            variables[axis - 1] = type;
        }
    }
}
