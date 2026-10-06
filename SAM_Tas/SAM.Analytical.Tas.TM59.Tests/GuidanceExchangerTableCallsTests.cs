// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Performance regression (2026-09-29, real 3-dwelling project). The manufacturer-guidance exchanger efficiency
    /// was one (intake, extract, own airflow) table of ~188,000 cells per unit, and every cell is one cross-process TAS
    /// call (~0.2-0.35 ms measured), so the call COUNT is the cost. It is now a (intake, extract) state table on
    /// breakpoint-pruned axes (Equal) times a two-point own-airflow table (Multiply) - the same interpolated function,
    /// ~70,000 cells. These pin the count through the production writer and read-back over a production recipe,
    /// every cell still written or verified exactly, and prove the new representation equal to the old over the
    /// whole input domain. COM-free: the tables are stand-ins the production code reaches through <c>dynamic</c>,
    /// exactly as it reaches TAS's.
    /// </summary>
    [TestFixture]
    public class GuidanceExchangerTableCallsTests
    {
        [Test]
        public void TheStateTableIsAFractionOfTheOldCells_AndTheReadBackReadsEachAxisOnceAndEveryCell()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();
            int cells_Old = recipe.Intakes_C.Length * recipe.Extracts_C.Length * 2;
            int cells = recipe.StateIntakes_C.Length * recipe.StateExtracts_C.Length;

            //The real project's recipe (12 / 19 / 22 C): 335 x 281 x 2 = 188,270 -> 265 x 266 = 70,490.
            Assert.That(recipe.Intakes_C.Length * recipe.Extracts_C.Length * 2, Is.EqualTo(188270));
            Assert.That(cells, Is.EqualTo(70490));
            Assert.That(cells, Is.LessThan(0.4 * cells_Old));

            CountingTable table = new CountingTable();
            Write(table, recipe);

            Assert.That(table.SetDataCalls, Is.LessThan(cells), "cells already holding TAS's initial 0.0 (bypass) are not written again");

            table.ResetCounts();
            Assert.That(ReadBack(table, recipe, out string disagreement), Is.True, disagreement);
            Assert.That(table.GetAxisCalls, Is.EqualTo(recipe.StateIntakes_C.Length + recipe.StateExtracts_C.Length));
            Assert.That(table.GetDataCalls, Is.EqualTo(cells));

            //Every cell holds the state - including the ones not written.
            for (int i = 0; i < recipe.StateIntakes_C.Length; i++)
            {
                for (int j = 0; j < recipe.StateExtracts_C.Length; j++)
                {
                    Assert.That(table.Data[i, j, 0], Is.EqualTo(recipe.State(recipe.StateIntakes_C[i], recipe.StateExtracts_C[j])));
                }
            }
        }

        /// <summary>
        /// The defect the licensed AFTER run found (2026-09-29): the writer asked (1,1,1) whether the table started at
        /// zero, TAS's fresh table holds 1.0 there, so no call was saved. The cells TAS carries over from the old extent
        /// are written whatever their value, and only those zeros.
        /// </summary>
        [Test]
        public void TheCellsAFreshTasTableCarriesOver_AreAlwaysWritten_AndOnlyThoseZerosAreWritten()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            CountingTable table = new CountingTable();
            Write(table, recipe);

            int nonZero = 0;
            foreach (double intake in recipe.StateIntakes_C)
            {
                foreach (double extract in recipe.StateExtracts_C)
                {
                    nonZero += recipe.State(intake, extract) != 0.0 ? 1 : 0;
                }
            }

            int zerosInOldExtent = 0;
            for (int i = 0; i < 2; i++)
            {
                zerosInOldExtent += recipe.State(recipe.StateIntakes_C[i], recipe.StateExtracts_C[0]) == 0.0 ? 1 : 0;
            }

            Assert.That(table.SetDataCalls, Is.EqualTo(nonZero + zerosInOldExtent));
            Assert.That(ReadBack(table, recipe, out string disagreement), Is.True, disagreement);
        }

        [Test]
        public void AnOldExtentCellTheRuleMakesZero_IsWrittenAsZero()
        {
            //A recipe whose first two state cells are bypass: the fresh table's carried-over 1.0 there must be overwritten.
            TPD.Modify.GuidanceRecipe recipe = Recipe();
            CountingTable table = new CountingTable { OldValue = 1.0 };
            Write(table, recipe);

            CountingTable stale = new CountingTable();
            Write(stale, recipe);
            int i = Array.FindIndex(recipe.StateIntakes_C, x => recipe.State(x, recipe.StateExtracts_C.Last()) == 0.0);
            Assume.That(i, Is.GreaterThanOrEqualTo(0));
            stale.Data[i, stale.Data.GetLength(1) - 1, 0] = 1.0;

            Assert.That(ReadBack(stale, recipe, out string staleDisagreement), Is.False);
            Assert.That(staleDisagreement, Does.Contain("is not the stated rule"));
        }

        [Test]
        public void ATableThatDoesNotStartAtZero_IsWrittenInFull()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            CountingTable table = new CountingTable { InitialValue = double.NaN };
            Write(table, recipe);

            Assert.That(table.SetDataCalls, Is.EqualTo((long)recipe.StateIntakes_C.Length * recipe.StateExtracts_C.Length));
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

        [Test]
        public void AStateTableOverTheWrongVariablesOrCombination_IsRefused()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            CountingTable variable = new CountingTable();
            Write(variable, recipe);
            variable.Variables[1] = global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableEDB;
            Assert.That(ReadBack(variable, recipe, out string disagreement_Variable), Is.False);
            Assert.That(disagreement_Variable, Does.Contain("intake and extract dry bulb"));

            CountingTable multiplier = new CountingTable();
            Write(multiplier, recipe);
            multiplier.Multiplier = global::TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierMultiply;
            Assert.That(ReadBack(multiplier, recipe, out string disagreement_Multiplier), Is.False);
            Assert.That(disagreement_Multiplier, Does.Contain("combination"));

            CountingTable extrapolate = new CountingTable();
            Write(extrapolate, recipe);
            extrapolate.Extrapolate = true;
            Assert.That(ReadBack(extrapolate, recipe, out _), Is.False);
        }

        [Test]
        public void TheAirflowTable_HoldsBothFractionsOverItsOwnAirflow_AndIsReadBackExactly()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            CountingTable airflow = new CountingTable();
            WriteAirflow(airflow, recipe);

            Assert.That(airflow.Variables[0], Is.EqualTo(global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableEFlow));
            Assert.That(airflow.Multiplier, Is.EqualTo(global::TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierMultiply));
            Assert.That(airflow.Extrapolate, Is.False);
            Assert.That(airflow.Axes[0], Is.EqualTo(new[] { recipe.DesignSupply_Lps, recipe.Elevated_Lps }));
            Assert.That(airflow.Data[0, 0, 0], Is.EqualTo(recipe.ExtractFraction));
            Assert.That(airflow.Data[1, 0, 0], Is.EqualTo(recipe.CoolingExtractFraction));
            Assert.That(airflow.SetDataCalls, Is.EqualTo(2));
            Assert.That(ReadBackAirflow(airflow, recipe, out string disagreement), Is.True, disagreement);

            CountingTable wrongFraction = new CountingTable();
            WriteAirflow(wrongFraction, recipe);
            wrongFraction.Data[1, 0, 0] = recipe.ExtractFraction;
            Assert.That(ReadBackAirflow(wrongFraction, recipe, out string disagreement_Fraction), Is.False);
            Assert.That(disagreement_Fraction, Does.Contain("recovery fractions"));

            CountingTable wrongAxis = new CountingTable();
            WriteAirflow(wrongAxis, recipe);
            wrongAxis.Axes[0][1] = recipe.DesignSupply_Lps;
            Assert.That(ReadBackAirflow(wrongAxis, recipe, out string disagreement_Axis), Is.False);
            Assert.That(disagreement_Axis, Does.Contain("design / elevated"));

            CountingTable equal = new CountingTable();
            WriteAirflow(equal, recipe);
            equal.Multiplier = global::TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            Assert.That(ReadBackAirflow(equal, recipe, out _), Is.False, "an equality airflow table would replace the state, not scale it");
        }

        /// <summary>
        /// The breakpoints every switch needs stay on the pruned axes: both inclusive minimums with the 0.01 K step
        /// below them, the activation step, and the whole 0.1 K diagonal lattice where extract = intake can switch.
        /// </summary>
        [Test]
        public void ThePrunedAxes_KeepEveryBreakpointAStateChangesAt()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            foreach (double value in new[] { -20.0, 11.99, 12.0, 45.0 })
            {
                Assert.That(recipe.StateIntakes_C, Has.Some.EqualTo(value).Within(1e-9), "intake " + value);
            }

            foreach (double value in new[] { 5.0, 18.99, 19.0, 22.0, 22.01, 22.1, 45.0, 45.1, 100.0 })
            {
                Assert.That(recipe.StateExtracts_C, Has.Some.EqualTo(value).Within(1e-9), "extract " + value);
            }

            //From the extract minimum to 45 C the diagonal crosses every 0.1 K line, so none of them is removed.
            Assert.That(recipe.StateIntakes_C.Count(x => x >= 19.0 - 1e-9 && x <= 45.0 + 1e-9), Is.EqualTo(261));
            Assert.That(recipe.StateExtracts_C.Count(x => x >= 19.0 - 1e-9 && x <= 45.0 + 1e-9), Is.EqualTo(262), "the 261 lattice lines and 22.01");

            Assert.That(recipe.StateIntakes_C, Is.Ordered.Ascending);
            Assert.That(recipe.StateExtracts_C, Is.Ordered.Ascending);
            Assert.That(recipe.StateIntakes_C.First(), Is.EqualTo(recipe.Intakes_C.First()));
            Assert.That(recipe.StateIntakes_C.Last(), Is.EqualTo(recipe.Intakes_C.Last()));
            Assert.That(recipe.StateExtracts_C.First(), Is.EqualTo(recipe.Extracts_C.First()));
            Assert.That(recipe.StateExtracts_C.Last(), Is.EqualTo(recipe.Extracts_C.Last()));
            Assert.That(recipe.StateIntakes_C.All(x => recipe.Intakes_C.Contains(x)), Is.True);
            Assert.That(recipe.StateExtracts_C.All(x => recipe.Extracts_C.Contains(x)), Is.True);
        }

        [Test]
        public void TheStateTimesTheAirflowFraction_IsTheGuidanceAtBothAirflows()
        {
            TPD.Modify.GuidanceRecipe recipe = Recipe();

            foreach (double intake in new[] { -20.0, 0.0, 11.99, 12.0, 15.0, 19.0, 25.0, 45.0 })
            {
                foreach (double extract in new[] { 5.0, 18.99, 19.0, 22.0, 25.0, 45.0, 100.0 })
                {
                    Assert.That(recipe.State(intake, extract) * recipe.ExtractFraction, Is.EqualTo(recipe.BackgroundEfficiency(intake, extract)));
                    Assert.That(recipe.State(intake, extract) * recipe.CoolingExtractFraction, Is.EqualTo(recipe.CoolingEfficiency(intake, extract)));
                }
            }
        }

        /// <summary>
        /// The proof the new representation is the old one. The OLD table - (intake, extract, own airflow) on the full
        /// lattice with the background and cooling efficiency at the two airflows - and the NEW pair exactly as the
        /// production writers leave them are each interpolated per axis, linearly and clamped at the ends
        /// (<c>Extrapolate</c> off), and compared over the whole input domain: every full-lattice breakpoint, 0.005 K
        /// either side of each, the midpoints, a dense grid well beyond both ends, and random points - at airflows
        /// below design, at design, between, at elevated and above. Several recipes, including thresholds off the
        /// 0.1 K lattice and an intake minimum above the extract minimum.
        /// </summary>
        [TestCase(12.0, 19.0, 22.0)]
        [TestCase(15.0, 15.0, 24.0)]
        [TestCase(19.0, 12.0, 22.0)]
        [TestCase(12.35, 19.07, 23.5)]
        public void TheNewRepresentation_EqualsTheOldOverTheWholeDomain(double intakeMinimum_C, double extractMinimum_C, double activation_C)
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = GuidanceCoolingRecipeTests.GuidanceCooling(strategy =>
            {
                strategy.BypassMinimumIntakeTemperature_C = intakeMinimum_C;
                strategy.BypassMinimumExtractTemperature_C = extractMinimum_C;
                strategy.CoolingActivationTemperature_C = activation_C;
                strategy.MinimumCoolingActivationTemperature_C = System.Math.Min(activation_C, strategy.MinimumCoolingActivationTemperature_C);
                strategy.MaximumCoolingActivationTemperature_C = System.Math.Max(activation_C, strategy.MaximumCoolingActivationTemperature_C);
            });
            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out TPD.Modify.GuidanceRecipe recipe, out string refusal), Is.True, refusal);

            double[] flows = { recipe.DesignSupply_Lps, recipe.Elevated_Lps };
            double[,,] old = new double[recipe.Intakes_C.Length, recipe.Extracts_C.Length, 2];
            for (int i = 0; i < recipe.Intakes_C.Length; i++)
            {
                for (int j = 0; j < recipe.Extracts_C.Length; j++)
                {
                    old[i, j, 0] = recipe.BackgroundEfficiency(recipe.Intakes_C[i], recipe.Extracts_C[j]);
                    old[i, j, 1] = recipe.CoolingEfficiency(recipe.Intakes_C[i], recipe.Extracts_C[j]);
                }
            }

            CountingTable state = new CountingTable();
            Write(state, recipe);
            Assert.That(ReadBack(state, recipe, out string disagreement), Is.True, disagreement);
            CountingTable airflow = new CountingTable();
            WriteAirflow(airflow, recipe);

            List<double> intakes = Domain(recipe.Intakes_C);
            List<double> extracts = Domain(recipe.Extracts_C);
            Random random = new Random(20260929);
            List<double[]> points = new List<double[]>();
            foreach (double intake in intakes)
            {
                foreach (double extract in extracts)
                {
                    points.Add(new[] { intake, extract });
                }
            }

            for (int n = 0; n < 200000; n++)
            {
                points.Add(new[] { -40.0 + (160.0 * random.NextDouble()), -40.0 + (160.0 * random.NextDouble()) });
            }

            double[] flows_Query = { 0.0, recipe.DesignSupply_Lps, 0.5 * (recipe.DesignSupply_Lps + recipe.Elevated_Lps), recipe.Elevated_Lps, 2.0 * recipe.Elevated_Lps };

            int compared = 0;
            double worst = 0.0;
            foreach (double[] point in points)
            {
                double state_New = Interpolate2(state.Axes[0], state.Axes[1], (i, j) => state.Data[i, j, 0], point[0], point[1]);
                foreach (double flow in flows_Query)
                {
                    double value_Old = Interpolate3(recipe.Intakes_C, recipe.Extracts_C, flows, old, point[0], point[1], flow);
                    double value_New = state_New * Interpolate1(airflow.Axes[0], k => airflow.Data[k, 0, 0], flow);

                    //Where the new state is exactly 0 or 1 - everywhere but inside the switching cells - the product is
                    //exact; inside them the two orders of multiplication may differ in the last bit only.
                    double tolerance = (state_New == 0.0 || state_New == 1.0) ? 0.0 : 1e-15;
                    double difference = System.Math.Abs(value_Old - value_New);
                    worst = System.Math.Max(worst, difference);
                    if (difference > tolerance)
                    {
                        Assert.Fail(string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0}, {1}, {2} l/s): old {3:R}, new {4:R}", point[0], point[1], flow, value_Old, value_New));
                    }

                    compared++;
                }
            }

            Assert.That(compared, Is.GreaterThan(1000000));
            TestContext.WriteLine("compared {0} points, worst |old - new| {1:R}", compared, worst);
        }

        /// <summary>Every lattice breakpoint, 0.005 K either side, each midpoint, and a coarse sweep well beyond both ends.</summary>
        private static List<double> Domain(double[] axis)
        {
            SortedSet<double> result = new SortedSet<double>();
            for (int i = 0; i < axis.Length; i++)
            {
                result.Add(axis[i]);
                result.Add(axis[i] - 0.005);
                result.Add(axis[i] + 0.005);
                if (i + 1 < axis.Length)
                {
                    result.Add(0.5 * (axis[i] + axis[i + 1]));
                }
            }

            for (double x = -40.0; x <= 130.0; x += 0.37)
            {
                result.Add(x);
            }

            return result.ToList();
        }

        /// <summary>Linear between breakpoints, the end value beyond them (no extrapolation), <c>v0 + (v1 - v0) t</c>.</summary>
        private static void Locate(double[] axis, double x, out int index, out double t)
        {
            if (x <= axis[0])
            {
                index = 0;
                t = 0.0;
                return;
            }

            if (x >= axis[axis.Length - 1])
            {
                index = axis.Length - 2;
                t = 1.0;
                return;
            }

            index = Array.BinarySearch(axis, x);
            if (index >= 0)
            {
                index = System.Math.Min(index, axis.Length - 2);
                t = (x - axis[index]) / (axis[index + 1] - axis[index]);
                return;
            }

            index = ~index - 1;
            t = (x - axis[index]) / (axis[index + 1] - axis[index]);
        }

        private static double Lerp(double v0, double v1, double t) => v0 + ((v1 - v0) * t);

        private static double Interpolate1(double[] axis, Func<int, double> value, double x)
        {
            Locate(axis, x, out int i, out double t);
            return Lerp(value(i), value(i + 1), t);
        }

        private static double Interpolate2(double[] axis1, double[] axis2, Func<int, int, double> value, double x, double y)
        {
            Locate(axis1, x, out int i, out double s);
            Locate(axis2, y, out int j, out double t);
            return Lerp(Lerp(value(i, j), value(i, j + 1), t), Lerp(value(i + 1, j), value(i + 1, j + 1), t), s);
        }

        private static double Interpolate3(double[] axis1, double[] axis2, double[] axis3, double[,,] data, double x, double y, double z)
        {
            Locate(axis3, z, out int k, out double u);
            return Lerp(Interpolate2(axis1, axis2, (i, j) => data[i, j, k], x, y), Interpolate2(axis1, axis2, (i, j) => data[i, j, k + 1], x, y), u);
        }

        private static TPD.Modify.GuidanceRecipe Recipe()
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = GuidanceCoolingRecipeTests.GuidanceCooling();

            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out TPD.Modify.GuidanceRecipe recipe, out string refusal), Is.True, refusal);

            //The production grid, not a toy one: the count is what matters.
            Assert.That(recipe.StateIntakes_C.Length * recipe.StateExtracts_C.Length, Is.GreaterThan(50000));

            return recipe;
        }

        private static void Write(CountingTable table, TPD.Modify.GuidanceRecipe recipe)
        {
            Invoke("WriteExchangerStateTable", table, recipe);
        }

        private static void WriteAirflow(CountingTable table, TPD.Modify.GuidanceRecipe recipe)
        {
            Invoke("WriteExchangerAirflowTable", table, recipe);
        }

        private static void Invoke(string name, CountingTable table, TPD.Modify.GuidanceRecipe recipe)
        {
            MethodInfo methodInfo = typeof(TPD.Modify).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(methodInfo, Is.Not.Null, name);

            methodInfo.Invoke(null, new object[] { table, recipe });
        }

        private static bool ReadBack(CountingTable table, TPD.Modify.GuidanceRecipe recipe, out string disagreement)
        {
            MethodInfo methodInfo = typeof(TPD.Modify).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(x => x.Name == "ReadBackExchangerStateTable" && x.GetParameters()[0].ParameterType == typeof(object));

            return Invoke(methodInfo, table, recipe, out disagreement);
        }

        private static bool ReadBackAirflow(CountingTable table, TPD.Modify.GuidanceRecipe recipe, out string disagreement)
        {
            MethodInfo methodInfo = typeof(TPD.Modify).GetMethod("ReadBackExchangerAirflowTable", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(methodInfo, Is.Not.Null);

            return Invoke(methodInfo, table, recipe, out disagreement);
        }

        private static bool Invoke(MethodInfo methodInfo, CountingTable table, TPD.Modify.GuidanceRecipe recipe, out string disagreement)
        {
            object[] arguments = { table, recipe, null };
            bool result = (bool)methodInfo.Invoke(null, arguments);
            disagreement = (string)arguments[2];
            return result;
        }
    }

    /// <summary>
    /// A TAS <c>ProfileDataModifierTable</c> stand-in that counts every call. Public: it is reached through <c>dynamic</c>.
    /// <para>
    /// By default it is the fresh table licensed TAS gives (measured 2026-09-29 over the full 335 x 281 x 2 production
    /// grid): <c>GetAxisSize</c> answers 2 x 0 x 0 before it is sized, and once sized it holds 1.0 at (1,1,1) and (2,1,1)
    /// - the old extent, each axis at least 1, survives <c>SetSize</c> - and <see cref="InitialValue"/> in every new cell.
    /// An axis sized 0 is absent (licensed: <c>SetSize(n1, n2, 0)</c> answers <c>GetAxisSize(3)</c> = 0) and its
    /// cells are addressed at index 1.
    /// </para>
    /// </summary>
    public sealed class CountingTable
    {
        public CountingTable()
        {
            Axes = new[] { new double[2], new double[0], new double[0] };
        }

        /// <summary>What a cell inside the table's old extent still holds once it is sized.</summary>
        public double OldValue { get; set; } = 1.0;

        public double InitialValue { get; set; }

        public bool CornersOnlyZero { get; set; }

        public double[][] Axes { get; private set; }

        public double[,,] Data { get; private set; }

        public global::TPD.tpdProfileDataVariableType[] Variables { get; } = new global::TPD.tpdProfileDataVariableType[3];

        public long SetDataCalls { get; private set; }

        public long GetDataCalls { get; private set; }

        public long GetAxisCalls { get; private set; }

        public string Name { get; set; }

        public bool Extrapolate { get; set; }

        public global::TPD.tpdProfileDataModifierMultiplier Multiplier { get; set; }

        public void ResetCounts()
        {
            SetDataCalls = 0;
            GetDataCalls = 0;
            GetAxisCalls = 0;
        }

        public void SetVariable(int axis, global::TPD.tpdProfileDataVariableType variable) => Variables[axis - 1] = variable;

        public global::TPD.tpdProfileDataVariableType GetVariable(int axis) => Variables[axis - 1];

        public void SetSize(int n1, int n2, int n3)
        {
            int old1 = System.Math.Max(1, Axes[0].Length), old2 = System.Math.Max(1, Axes[1].Length), old3 = System.Math.Max(1, Axes[2].Length);
            int m1 = System.Math.Max(1, n1), m2 = System.Math.Max(1, n2), m3 = System.Math.Max(1, n3);

            Axes = new[] { new double[n1], new double[n2], new double[n3] };
            Data = new double[m1, m2, m3];

            for (int i = 0; i < m1; i++)
            {
                for (int j = 0; j < m2; j++)
                {
                    for (int k = 0; k < m3; k++)
                    {
                        bool corner = (i == 0 && j == 0 && k == 0) || (i == m1 - 1 && j == m2 - 1 && k == m3 - 1);
                        bool old = i < old1 && j < old2 && k < old3;
                        Data[i, j, k] = CornersOnlyZero ? (corner ? 0.0 : 0.5) : old ? OldValue : InitialValue;
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
