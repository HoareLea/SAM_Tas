// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using System;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// SAM#123, COM-free: what the manufacturer-guidance grounding resolves from a unit's strategy before it
    /// writes anything to TAS - only the carriers the Stage 11 prototype proved are accepted, and the exchanger
    /// state cells are the stated rules on a grid that puts every threshold on a breakpoint. Fixture values only.
    /// </summary>
    [TestFixture]
    public class GuidanceCoolingRecipeTests
    {
        [Test]
        public void AProvenStrategy_ResolvesToTheRecipe()
        {
            Assert.That(TPD.Modify.TryGetGuidanceRecipe(GuidanceCooling(), out TPD.Modify.GuidanceRecipe recipe, out string refusal), Is.True, refusal);

            Assert.That(recipe.Elevated_Lps, Is.EqualTo(80.0));
            Assert.That(recipe.CoolingExtractFraction, Is.EqualTo(0.8576).Within(1e-9));
            Assert.That(recipe.CoilNetDrop_K, Is.EqualTo(8.245).Within(1e-9));
            Assert.That(recipe.MinimumSupply_C, Is.EqualTo(13.0));
            Assert.That(recipe.DesignSupply_Lps, Is.EqualTo(25.0));
            Assert.That(recipe.DesignExtract_Lps, Is.EqualTo(25.0));
            Assert.That(recipe.CoolingDuty_W, Is.EqualTo(TPD.Modify.GuidanceCoolingDuty_W));
            Assert.That(recipe.ExtractFraction, Is.EqualTo(0.8));
        }

        [Test]
        public void TheStateCells_AreTheStatedRules()
        {
            TPD.Modify.TryGetGuidanceRecipe(GuidanceCooling(), out TPD.Modify.GuidanceRecipe recipe, out _);

            //Background: the unit's own bypass - intake >= 12, extract > intake and extract >= 19, independent of the
            //cooling-stat (a warm extract with the stat satisfied still bypasses) - else the background fraction.
            Assert.That(recipe.BackgroundEfficiency(14.0, 20.0), Is.EqualTo(0.0));
            Assert.That(recipe.BackgroundEfficiency(12.0, 20.0), Is.EqualTo(0.0));
            Assert.That(recipe.BackgroundEfficiency(11.99, 20.0), Is.EqualTo(0.8));
            Assert.That(recipe.BackgroundEfficiency(14.0, 22.01), Is.EqualTo(0.0));
            Assert.That(recipe.BackgroundEfficiency(14.0, 18.99), Is.EqualTo(0.8));
            Assert.That(recipe.BackgroundEfficiency(14.0, 19.0), Is.EqualTo(0.0));
            Assert.That(recipe.BackgroundEfficiency(20.0, 20.0), Is.EqualTo(0.8));
            Assert.That(recipe.BackgroundEfficiency(-2.0, 17.0), Is.EqualTo(0.8));

            //Cooling: the same bypass decision, otherwise heat/coolth recovery at the elevated-airflow fraction.
            Assert.That(recipe.CoolingEfficiency(30.0, 25.0), Is.EqualTo(0.8576).Within(1e-9));
            Assert.That(recipe.CoolingEfficiency(18.0, 25.0), Is.EqualTo(0.0));
            Assert.That(recipe.CoolingEfficiency(10.0, 25.0), Is.EqualTo(0.8576).Within(1e-9));
        }

        [Test]
        public void TheSupplyLaw_IsTheNetDropFloored_WithTheKinkOnTheGrid()
        {
            TPD.Modify.TryGetGuidanceRecipe(GuidanceCooling(), out TPD.Modify.GuidanceRecipe recipe, out _);

            Assert.That(recipe.SupplyLaw_C(30.0), Is.EqualTo(30.0 - 8.245).Within(1e-9));
            Assert.That(recipe.SupplyLaw_C(13.0 + 8.245), Is.EqualTo(13.0).Within(1e-9));
            Assert.That(recipe.SupplyLaw_C(13.0 + 8.245 + 0.1), Is.EqualTo(13.1).Within(1e-9));
            Assert.That(recipe.SupplyLaw_C(15.0), Is.EqualTo(13.0));

            //A hot recovered extract (intake 10 C, extract 80 C) puts about 70 C on the coil: the law still follows it.
            double entering_C = (0.8576 * 80.0) + (0.1424 * 10.0);
            Assert.That(entering_C, Is.GreaterThan(60.0));
            Assert.That(recipe.SupplyLawEntering_C.Last(), Is.GreaterThan(entering_C));
            Assert.That(recipe.SupplyLaw_C(entering_C), Is.EqualTo(entering_C - 8.245).Within(1e-9));
            Assert.That(recipe.SupplyLawEntering_C, Is.EqualTo(new[] { -50.0, 13.0 + 8.245, 150.0 }));
        }

        [Test]
        public void NoStatedMinimum_IsAStraightLineTable()
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = GuidanceCooling(strategy => strategy.CoolingSupplyTemperatureRule = CoolingRule(double.NaN));

            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out TPD.Modify.GuidanceRecipe recipe, out string refusal), Is.True, refusal);
            Assert.That(recipe.SupplyLawEntering_C, Is.EqualTo(new[] { -50.0, 150.0 }));
            Assert.That(recipe.SupplyLaw_C(15.0), Is.EqualTo(15.0 - 8.245).Within(1e-9));
        }

        [Test]
        public void EveryThreshold_SitsOnABreakpoint()
        {
            TPD.Modify.TryGetGuidanceRecipe(GuidanceCooling(), out TPD.Modify.GuidanceRecipe recipe, out _);

            foreach (double value in new[] { 11.99, 12.0, 12.1, 22.0, 30.0, 37.4, 40.3, 45.0 })
            {
                Assert.That(recipe.Intakes_C.Any(x => Math.Abs(x - value) < 1e-9), Is.True, "intake " + value);
            }

            foreach (double value in new[] { 18.99, 19.0, 22.0, 30.0, 37.8, 45.0, 45.1, 50.0, 80.0, 100.0 })
            {
                Assert.That(recipe.Extracts_C.Any(x => Math.Abs(x - value) < 1e-9), Is.True, "extract " + value);
            }

            Assert.That(recipe.Intakes_C, Is.Ordered.Ascending);
            Assert.That(recipe.Extracts_C, Is.Ordered.Ascending);
            Assert.That(recipe.Intakes_C.Distinct().Count(), Is.EqualTo(recipe.Intakes_C.Length));
            Assert.That(recipe.Extracts_C.Distinct().Count(), Is.EqualTo(recipe.Extracts_C.Length));
        }

        [Test]
        public void AnExtractSwitchedStrategy_IsRefused()
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = GuidanceCooling(strategy => strategy.CoolingActivationSignal = CoolingActivationSignal.ExtractTemperature);

            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out _, out string refusal), Is.False);
            Assert.That(refusal, Does.Contain("room cooling-stat"));
        }

        [Test]
        public void ATableCoolingRule_IsRefused()
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = GuidanceCooling(strategy => strategy.CoolingSupplyTemperatureRule = SupplyTemperatureRule.PerformanceTable());

            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out _, out string refusal), Is.False);
            Assert.That(refusal, Does.Contain("exchanger then coil"));
        }

        [Test]
        public void TheSupersededIntakeOffsetRule_IsRefused()
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = GuidanceCooling(strategy => strategy.CoolingSupplyTemperatureRule = SupplyTemperatureRule.IntakeOffset(new[] { 70.0, 80.0, 90.0 }, new[] { 15.0, 14.0, 13.0 }));

            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out _, out string refusal), Is.False);
            Assert.That(refusal, Does.Contain("exchanger then coil"));
        }

        [Test]
        public void AnElevatedAirflowOutsideTheStatedFigures_IsRefused()
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = GuidanceCooling(strategy =>
            {
                strategy.CoolingSupplyTemperatureRule = SupplyTemperatureRule.ExchangerThenCoil(new[] { 90.0, 100.0 }, new[] { 0.85, 0.84 }, new[] { 8.5, 8.2 }, new[] { 0.6, 0.8 }, 13.0);
            });

            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out _, out string refusal), Is.False);
            Assert.That(refusal, Does.Contain("no exchanger and coil figures"));
        }

        [Test]
        public void TheDuty_IsNumerical_AndNeedsNoPublishedCapacity()
        {
            MechanicalVentilationGuidanceCooling guidanceCooling = GuidanceCooling(null, withCapacity: false);

            Assert.That(TPD.Modify.TryGetGuidanceRecipe(guidanceCooling, out TPD.Modify.GuidanceRecipe recipe, out string refusal), Is.True, refusal);
            Assert.That(recipe.CoolingDuty_W, Is.EqualTo(TPD.Modify.GuidanceCoolingDuty_W));

            //Far above what the stated drop asks of the coil at the elevated airflow (rho cp V dT ~ 0.8 kW).
            Assert.That(recipe.CoolingDuty_W, Is.GreaterThan(10.0 * 1.2 * 1.006 * 0.080 * recipe.CoilNetDrop_K * 1000.0));
        }

        [Test]
        public void AHotExtractBeyond45C_KeepsTheExactState()
        {
            TPD.Modify.TryGetGuidanceRecipe(GuidanceCooling(), out TPD.Modify.GuidanceRecipe recipe, out _);

            //For any intake below 45 C, every extract cell beyond 45 C is on the same side of the bypass diagonal, so a
            //hot (displacement-vent) extract interpolates exactly between them.
            //(From 45.1 C: the 45/45 cell is the diagonal itself, and its 0.1 K smear is the same as everywhere else.)
            foreach (double intake in recipe.Intakes_C.Where(x => x < 45.0))
            {
                double[] states = recipe.Extracts_C.Where(x => x >= 45.1).Select(x => recipe.CoolingEfficiency(intake, x)).Distinct().ToArray();
                Assert.That(states.Length, Is.EqualTo(1), "intake " + intake);
            }
        }

        [Test]
        public void ARuleWithoutAMinimum_SummarisesWithoutAFloor()
        {
            TPD.GuidanceCoolingResult result = new TPD.GuidanceCoolingResult(Guid.NewGuid(), "U", 30.0, 30.0, 80.0, 0.8576, 8.245, double.NaN, 12.0, 19.0, TPD.Modify.GuidanceCoolingDuty_W, 22.0,
                new System.Collections.Generic.List<double> { 30.0 }, new System.Collections.Generic.List<double> { 23.0 }, new System.Collections.Generic.List<double> { 24.0 },
                new System.Collections.Generic.List<double> { 25.0 }, new System.Collections.Generic.List<double> { 16.755 }, new System.Collections.Generic.List<double> { 80.0 },
                new System.Collections.Generic.List<double> { 80.0 }, new System.Collections.Generic.List<double> { 800.0 }, new System.Collections.Generic.List<double> { 0.0 });

            string summary = result.Summary();
            Assert.That(summary, Does.Not.Contain("NaN"));
            Assert.That(summary, Does.Contain("no minimum stated"));
            Assert.That(summary, Does.Not.Contain("at the limit"));
            Assert.That(result.SupplyTarget_C(0), Is.EqualTo(25.0 - 8.245).Within(1e-9));
        }

        [Test]
        public void TheBypassDiagonal_IsOnTheGridUpTo45C()
        {
            TPD.Modify.TryGetGuidanceRecipe(GuidanceCooling(), out TPD.Modify.GuidanceRecipe recipe, out _);

            //Every step on both axes is at most 0.1 K from the bypass thresholds up to 45 C.
            foreach (double[] axis in new[] { recipe.Intakes_C.Where(x => x >= 12.0).ToArray(), recipe.Extracts_C.Where(x => x >= 17.9 && x <= 45.0).ToArray() })
            {
                Assert.That(axis.Last(), Is.EqualTo(45.0).Within(1e-9));
                for (int i = 1; i < axis.Length; i++)
                {
                    Assert.That(axis[i] - axis[i - 1], Is.LessThanOrEqualTo(0.1 + 1e-9));
                }
            }
        }

        [Test]
        public void SelectedRoomStat_KeepsThe22COffPointAndPointOneKelvinFullDemand()
        {
            Assert.That(TPD.Modify.TryGetGuidanceRecipe(GuidanceCooling(), out TPD.Modify.GuidanceRecipe recipe, out _), Is.True);
            Assert.That(recipe.ActivationTemperature_C, Is.EqualTo(22.0));
            Assert.That(TPD.Modify.GuidanceCoolingStatBand_K, Is.EqualTo(0.1));

            //GroundGuidanceCooling writes TAS normal controllers at activation + half the band.
            double setpoint = recipe.ActivationTemperature_C + TPD.Modify.GuidanceCoolingStatBand_K / 2.0;
            Assert.That(setpoint - TPD.Modify.GuidanceCoolingStatBand_K / 2.0, Is.EqualTo(22.0).Within(1e-9));
            Assert.That(setpoint + TPD.Modify.GuidanceCoolingStatBand_K / 2.0, Is.EqualTo(22.1).Within(1e-9));
        }

        private static SupplyTemperatureRule CoolingRule(double minimum_C)
        {
            return SupplyTemperatureRule.ExchangerThenCoil(new[] { 60.0, 80.0, 100.0, 120.0 }, new[] { 0.8796, 0.8576, 0.8356, 0.8136 }, new[] { 9.265, 8.745, 8.225, 7.705 }, new[] { 0.3, 0.5, 0.8, 1.1 }, minimum_C);
        }

        internal static MechanicalVentilationGuidanceCooling GuidanceCooling(Action<VentilationUnitOperatingStrategy> edit = null, bool withCapacity = true)
        {
            VentilationUnitOperatingStrategy strategy = new VentilationUnitOperatingStrategy
            {
                Source = "Test Fixture, manufacturer modelling guidance, v.1 - not a real product",
                CoolingActivationTemperature_C = 22.0,
                CoolingActivationSignal = CoolingActivationSignal.RoomTemperature,
                MinimumCoolingActivationTemperature_C = 22.0,
                MaximumCoolingActivationTemperature_C = 25.0,
                BypassMinimumIntakeTemperature_C = 12.0,
                BypassMinimumExtractTemperature_C = 19.0,
                ElevatedAirFlow_Lps = 80.0,
                MinimumElevatedAirFlow_Lps = 60.0,
                MaximumElevatedAirFlow_Lps = 120.0,
                SummerBypassSupplyTemperatureRule = SupplyTemperatureRule.OutdoorAir(),
                HeatCoolthRecoverySupplyTemperatureRule = SupplyTemperatureRule.LinearBlend(0.8),
                CoolingSupplyTemperatureRule = CoolingRule(13.0),
            };

            edit?.Invoke(strategy);

            VentilationUnitPerformanceTable table = new VentilationUnitPerformanceTable(
                new[]
                {
                    new VentilationUnitPerformanceAxis(VentilationUnitPerformanceAxis.Name_ExternalDryBulbTemperature, "degC", new double[] { 29, 34 }),
                    new VentilationUnitPerformanceAxis(VentilationUnitPerformanceAxis.Name_EnteringDryBulbTemperature, "degC", new double[] { 23, 26 }),
                    new VentilationUnitPerformanceAxis(VentilationUnitPerformanceAxis.Name_AirFlowRate, "l/s", new double[] { 50, 120 }),
                },
                withCapacity
                    ? new[]
                    {
                        new VentilationUnitPerformanceOutput(VentilationUnitPerformanceOutput.Name_SupplyAirTemperature, "degC", new double[] { 15, 16, 17, 18, 16, 17, 18, 19 }),
                        new VentilationUnitPerformanceOutput(VentilationUnitPerformanceOutput.Name_CombinedCoolingCapacity, "kW", new double[] { 1.0, 1.5, 1.2, 1.7, 1.4, 2.0, 1.3, 1.9 }),
                    }
                    : new[]
                    {
                        new VentilationUnitPerformanceOutput(VentilationUnitPerformanceOutput.Name_SupplyAirTemperature, "degC", new double[] { 15, 16, 17, 18, 16, 17, 18, 19 }),
                    });

            MechanicalVentilationGuidanceSettings settings = new MechanicalVentilationGuidanceSettings
            {
                OperatingStrategy = strategy,
                SupplyAirTemperatureTable = table,
                SourceIdentifier = "Test Fixture",
            };

            Guid guid_Stat = Guid.NewGuid();

            return new MechanicalVentilationGuidanceCooling(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                guid_Stat,
                new[]
                {
                    new MechanicalVentilationGuidanceRoom(guid_Stat, Guid.NewGuid(), 13.0, 0.0, 41.6, 0.0),
                    new MechanicalVentilationGuidanceRoom(Guid.NewGuid(), Guid.NewGuid(), 12.0, 0.0, 38.4, 0.0),
                    new MechanicalVentilationGuidanceRoom(Guid.NewGuid(), Guid.NewGuid(), 0.0, 15.0, 0.0, 48.0),
                    new MechanicalVentilationGuidanceRoom(Guid.NewGuid(), Guid.NewGuid(), 0.0, 10.0, 0.0, 32.0),
                },
                settings);
        }
    }
}
