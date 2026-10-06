// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Tas.TM59.Tests
{
    [TestFixture]
    public class TableModifierRoundTripTests
    {
        [TestCase(-1, true)]
        [TestCase(0, false)]
        public void Import_UsesTasNonZeroBooleanSemantics(int nativeExtrapolate, bool expected)
        {
            FakeTpdTableModifier table = OneDimensionalTable();
            table.Extrapolate = nativeExtrapolate;

            TableModifier result = SAM.Analytical.Tas.TPD.Convert.ToSAM(table);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Extrapolate, Is.EqualTo(expected));
        }

        [Test]
        public void Import_PreservesOrderedThreeAxisDefinitionAndValues()
        {
            FakeTpdTableModifier table = ThreeDimensionalTable();

            TableModifier result = SAM.Analytical.Tas.TPD.Convert.ToSAM(table);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Headers, Is.EqualTo(new[]
            {
                "tpdProfileDataVariableODB",
                "tpdProfileDataVariableEDB",
                "tpdProfileDataVariableEFlow",
                "value"
            }).AsCollection);
            Assert.That(result.RowCount, Is.EqualTo(8));
            Assert.That(result.GetDictionary(0).Values, Is.EqualTo(new[] { 34.0, 26.0, 120.0, 34026120.0 }).AsCollection);
            Assert.That(result.GetDictionary(7).Values, Is.EqualTo(new[] { 29.0, 23.0, 50.0, 29023050.0 }).AsCollection);
        }

        [Test]
        public void Import_RejectsDuplicateAxisVariables()
        {
            FakeTpdTableModifier table = ThreeDimensionalTable();
            table.SetVariable(2, global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableODB);

            Assert.That(SAM.Analytical.Tas.TPD.Convert.ToSAM(table), Is.Null);
        }

        [Test]
        public void JsonSaveReload_PreservesOrderedTableAndExtrapolation()
        {
            TableModifier expected = SAM.Analytical.Tas.TPD.Convert.ToSAM(ThreeDimensionalTable());
            expected.Extrapolate = true;
            string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid() + ".json");

            try
            {
                File.WriteAllText(path, expected.ToJsonObject().ToJsonString());
                TableModifier actual = new TableModifier(JsonNode.Parse(File.ReadAllText(path)).AsObject());

                Assert.That(actual.Headers, Is.EqualTo(expected.Headers).AsCollection);
                Assert.That(actual.RowCount, Is.EqualTo(expected.RowCount));
                Assert.That(actual.ArithmeticOperator, Is.EqualTo(ArithmeticOperator.Modulus));
                Assert.That(actual.Extrapolate, Is.True);
                for (int row = 0; row < expected.RowCount; row++)
                {
                    Assert.That(actual.GetDictionary(row), Is.EqualTo(expected.GetDictionary(row)));
                }
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void Export_ReconstructsThreeByFourByEightCoordinates()
        {
            double[] odb = { 29, 32, 34 };
            double[] edb = { 23, 24, 25, 26 };
            double[] flow = { 50, 60, 70, 80, 90, 100, 110, 120 };
            TableModifier modifier = Table(
                new[] { "tpdProfileDataVariableODB", "tpdProfileDataVariableEDB", "tpdProfileDataVariableEFlow", "value" },
                odb, edb, flow);
            modifier.Extrapolate = true;
            FakeTpdProfileData profile = new FakeTpdProfileData();

            bool result = SAM.Analytical.Tas.TPD.Modify.AddModifier(profile, modifier, null);

            Assert.That(result, Is.True);
            Assert.That(profile.Table.GetAxisSize(1), Is.EqualTo(3));
            Assert.That(profile.Table.GetAxisSize(2), Is.EqualTo(4));
            Assert.That(profile.Table.GetAxisSize(3), Is.EqualTo(8));
            Assert.That(profile.Table.GetVariable(1), Is.EqualTo(global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableODB));
            Assert.That(profile.Table.GetVariable(2), Is.EqualTo(global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableEDB));
            Assert.That(profile.Table.GetVariable(3), Is.EqualTo(global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableEFlow));
            Assert.That(profile.Table.Multiplier, Is.EqualTo(global::TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual));
            Assert.That(profile.Table.Extrapolate, Is.EqualTo(-1));
            Assert.That(profile.Table.PopulatedCount, Is.EqualTo(96));
            Assert.That(profile.Table.GetDataValue(3, 4, 8), Is.EqualTo(Value(34, 26, 120)));
        }

        [Test]
        public void Export_ReconstructsSmallTwoDimensionalTable()
        {
            TableModifier modifier = Table(
                new[] { "tpdProfileDataVariableODB", "tpdProfileDataVariableEDB", "value" },
                new[] { 31.0, 29.0 }, new[] { 25.0, 23.0, 24.0 });
            FakeTpdProfileData profile = new FakeTpdProfileData();

            Assert.That(SAM.Analytical.Tas.TPD.Modify.AddModifier(profile, modifier, null), Is.True);
            Assert.That(profile.Table.GetAxisSize(1), Is.EqualTo(2));
            Assert.That(profile.Table.GetAxisSize(2), Is.EqualTo(3));
            Assert.That(profile.Table.GetAxisSize(3), Is.Zero);
            Assert.That(profile.Table.PopulatedCount, Is.EqualTo(6));
            Assert.That(profile.Table.GetAxisValue(1, 1), Is.EqualTo(31));
            Assert.That(profile.Table.GetAxisValue(1, 2), Is.EqualTo(29));
            Assert.That(profile.Table.GetDataValue(2, 3, 1), Is.EqualTo(Value(29, 24)));
        }

        [Test]
        public void Export_RejectsDuplicateAxesBeforeCreatingNativeModifier()
        {
            TableModifier modifier = Table(
                new[] { "tpdProfileDataVariableODB", "tpdProfileDataVariableODB", "value" },
                new[] { 29.0, 32.0 }, new[] { 23.0, 24.0 });
            FakeTpdProfileData profile = new FakeTpdProfileData();

            Assert.That(SAM.Analytical.Tas.TPD.Modify.AddModifier(profile, modifier, null), Is.False);
            Assert.That(profile.GetModifierCount(), Is.Zero);
        }

        [Test]
        public void Export_RejectsIncompleteCoordinateGridBeforeCreatingNativeModifier()
        {
            TableModifier modifier = new TableModifier(ArithmeticOperator.Modulus,
                new[] { "tpdProfileDataVariableODB", "tpdProfileDataVariableEDB", "value" });
            modifier.AddValues(new Dictionary<int, double> { [0] = 29, [1] = 23, [2] = 1 });
            modifier.AddValues(new Dictionary<int, double> { [0] = 29, [1] = 24, [2] = 2 });
            modifier.AddValues(new Dictionary<int, double> { [0] = 32, [1] = 23, [2] = 3 });
            FakeTpdProfileData profile = new FakeTpdProfileData();

            Assert.That(SAM.Analytical.Tas.TPD.Modify.AddModifier(profile, modifier, null), Is.False);
            Assert.That(profile.GetModifierCount(), Is.Zero);
        }

        private static FakeTpdTableModifier OneDimensionalTable()
        {
            FakeTpdTableModifier result = new FakeTpdTableModifier
            {
                Multiplier = global::TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual
            };
            result.SetVariable(1, global::TPD.tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            result.AddPoint(0, 0);
            result.AddPoint(100, 1);
            return result;
        }

        private static FakeTpdTableModifier ThreeDimensionalTable()
        {
            double[] odb = { 34, 29 };
            double[] edb = { 26, 23 };
            double[] flow = { 120, 50 };
            FakeTpdTableModifier result = new FakeTpdTableModifier
            {
                Multiplier = global::TPD.tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual,
                Extrapolate = -1
            };
            result.SetSize(2, 2, 2);
            result.SetVariable(1, global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableODB);
            result.SetVariable(2, global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableEDB);
            result.SetVariable(3, global::TPD.tpdProfileDataVariableType.tpdProfileDataVariableEFlow);
            for (int index = 0; index < 2; index++)
            {
                result.SetAxisValue(1, index + 1, odb[index]);
                result.SetAxisValue(2, index + 1, edb[index]);
                result.SetAxisValue(3, index + 1, flow[index]);
            }
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                result.SetDataValue(x + 1, y + 1, z + 1, Value(odb[x], edb[y], flow[z]));
            }
            return result;
        }

        private static TableModifier Table(string[] headers, params double[][] axes)
        {
            TableModifier result = new TableModifier(ArithmeticOperator.Modulus, headers);
            AddRows(result, axes, new double[axes.Length], 0);
            return result;
        }

        private static void AddRows(TableModifier modifier, double[][] axes, double[] coordinate, int axis)
        {
            if (axis < axes.Length)
            {
                foreach (double value in axes[axis])
                {
                    coordinate[axis] = value;
                    AddRows(modifier, axes, coordinate, axis + 1);
                }
                return;
            }

            Dictionary<int, double> row = new Dictionary<int, double>();
            for (int index = 0; index < coordinate.Length; index++)
            {
                row[index] = coordinate[index];
            }
            row[coordinate.Length] = Value(coordinate);
            modifier.AddValues(row);
        }

        private static double Value(params double[] coordinate)
        {
            double result = 0;
            foreach (double value in coordinate)
            {
                result = (result * 1000) + value;
            }
            return result;
        }
    }
}
