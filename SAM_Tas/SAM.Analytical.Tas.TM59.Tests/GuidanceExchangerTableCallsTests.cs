// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using System.Linq;
using System.Reflection;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Performance regression (2026-09-29, real 3-dwelling project: the "ventilation legs" step took 255 s of a 266 s
    /// conversion). The manufacturer-guidance exchanger state table is ~188,000 cells per unit on the production grid
    /// and every cell is one cross-process TAS call (~0.2-0.35 ms measured), so the call COUNT is the cost. These pin
    /// the count - through the production writer and read-back, over a production recipe - while every cell is
    /// still written or verified exactly. COM-free: the table is a stand-in the production code reaches through
    /// <c>dynamic</c>, exactly as it reaches TAS's.
    /// </summary>
    [TestFixture]
    public class GuidanceExchangerTableCallsTests
    {
        [Test]
        public void TheWriteSkipsCellsTasAlreadyHolds_AndTheReadBackReadsEachAxisOnce()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();
            int cells = recipe.Intakes_C.Length * recipe.Extracts_C.Length * 2;

            CountingTable table = new CountingTable();
            Write(table, recipe);

            Assert.That(table.SetDataCalls, Is.LessThan(cells), "cells already holding TAS's initial 0.0 (bypass) are not written again");

            table.ResetCounts();
            Assert.That(ReadBack(table, recipe, out string disagreement), Is.True, disagreement);

            //Axes once each (plus the airflow axis), then both values of every cell - previously axis 2 was re-read
            //for every cell and axis 1 for every row.
            Assert.That(table.GetAxisCalls, Is.LessThanOrEqualTo(recipe.Intakes_C.Length + recipe.Extracts_C.Length + 2));
            Assert.That(table.GetDataCalls, Is.EqualTo(cells));

            //Every cell holds the rule - including the ones not written.
            for (int i = 0; i < recipe.Intakes_C.Length; i++)
            {
                for (int j = 0; j < recipe.Extracts_C.Length; j++)
                {
                    Assert.That(table.Data[i, j, 0], Is.EqualTo(recipe.BackgroundEfficiency(recipe.Intakes_C[i], recipe.Extracts_C[j])));
                    Assert.That(table.Data[i, j, 1], Is.EqualTo(recipe.CoolingEfficiency(recipe.Intakes_C[i], recipe.Extracts_C[j])));
                }
            }
        }

        [Test]
        public void ATableThatDoesNotStartAtZero_IsWrittenInFull()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            CountingTable table = new CountingTable { InitialValue = double.NaN };
            Write(table, recipe);

            Assert.That(table.SetDataCalls, Is.EqualTo(recipe.Intakes_C.Length * recipe.Extracts_C.Length * 2L));
            Assert.That(ReadBack(table, recipe, out string disagreement), Is.True, disagreement);
        }

        [Test]
        public void AnUnwrittenCellThatIsNotZero_IsRefusedByTheReadBack()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            //Zero at the two corners the writer asks about, and not zero anywhere else it relied on.
            CountingTable table = new CountingTable { CornersOnlyZero = true };
            Write(table, recipe);

            Assert.That(ReadBack(table, recipe, out string disagreement), Is.False);
            Assert.That(disagreement, Does.Contain("is not the stated rule"));
        }

        [Test]
        public void AnAxisValueTasDidNotKeep_IsRefused()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            CountingTable table = new CountingTable();
            Write(table, recipe);
            table.Axes[1][5] += 0.5;

            Assert.That(ReadBack(table, recipe, out string disagreement), Is.False);
            Assert.That(disagreement, Does.Contain("extract axis value 6"));
        }

        private static TPD.Modify.GuidanceRecipe Recipe()
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = MixedGuidanceCoolingTests.GuidanceCoolingForTests();

            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out TPD.Modify.GuidanceRecipe recipe, out string refusal), Is.True, refusal);

            //The production grid, not a toy one: the count is what matters.
            Assert.That(recipe.Intakes_C.Length * recipe.Extracts_C.Length, Is.GreaterThan(50000));

            return recipe;
        }

        private static void Write(CountingTable table, TPD.Modify.GuidanceRecipe recipe)
        {
            MethodInfo methodInfo = typeof(TPD.Modify).GetMethod("WriteExchangerStateTable", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(methodInfo, Is.Not.Null);

            methodInfo.Invoke(null, new object[] { table, recipe });
        }

        private static bool ReadBack(CountingTable table, TPD.Modify.GuidanceRecipe recipe, out string disagreement)
        {
            MethodInfo methodInfo = typeof(TPD.Modify).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(x => x.Name == "ReadBackExchangerStateTable" && x.GetParameters()[0].ParameterType == typeof(object));

            object[] arguments = { table, recipe, null };
            bool result = (bool)methodInfo.Invoke(null, arguments);
            disagreement = (string)arguments[2];
            return result;
        }
    }

    /// <summary>A TAS <c>ProfileDataModifierTable</c> stand-in that counts every call. Public: it is reached through <c>dynamic</c>.</summary>
    public sealed class CountingTable
    {
        public double InitialValue { get; set; }

        public bool CornersOnlyZero { get; set; }

        public double[][] Axes { get; private set; } = new double[3][];

        public double[,,] Data { get; private set; }

        public long SetDataCalls { get; private set; }

        public long GetDataCalls { get; private set; }

        public long GetAxisCalls { get; private set; }

        public string Name { get; set; }

        public bool Extrapolate { get; set; }

        public object Multiplier { get; set; }

        public void ResetCounts()
        {
            SetDataCalls = 0;
            GetDataCalls = 0;
            GetAxisCalls = 0;
        }

        public void SetVariable(int axis, object variable)
        {
        }

        public void SetSize(int n1, int n2, int n3)
        {
            Axes = new[] { new double[n1], new double[n2], new double[n3] };
            Data = new double[n1, n2, n3];

            for (int i = 0; i < n1; i++)
            {
                for (int j = 0; j < n2; j++)
                {
                    for (int k = 0; k < n3; k++)
                    {
                        bool corner = (i == 0 && j == 0 && k == 0) || (i == n1 - 1 && j == n2 - 1 && k == n3 - 1);
                        Data[i, j, k] = CornersOnlyZero ? (corner ? 0.0 : 0.5) : InitialValue;
                    }
                }
            }
        }

        public int GetAxisSize(int axis) => Axes[axis - 1].Length;

        public void SetAxisValue(int axis, int index, double value) => Axes[axis - 1][index - 1] = value;

        public double GetAxisValue(int axis, int index)
        {
            GetAxisCalls++;
            return Axes[axis - 1][index - 1];
        }

        public void SetDataValue(int i, int j, int k, double value)
        {
            SetDataCalls++;
            Data[i - 1, j - 1, k - 1] = value;
        }

        public double GetDataValue(int i, int j, int k)
        {
            GetDataCalls++;
            return Data[i - 1, j - 1, k - 1];
        }
    }
}
