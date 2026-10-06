// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using System.Collections.Generic;

//The interoperability library's own Query and Modify, named explicitly. Inside this namespace a bare
//`Query` binds to SAM.Analytical.Tas.TM59.Query, which is a different class - and the error it produces
//says the member does not exist rather than that the wrong class was found.
using TasModify = SAM.Analytical.Tas.Modify;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The unit boundary between a SAM air movement and a TBD inter-zone air movement.</b>
    /// <para>
    /// SAM states these airflows volumetrically all the way down - Approved Document F sizes a terminal in
    /// l/s and <c>SpaceAirMovement.AirFlow</c> carries m3/s. A TBD IZAM is not volumetric: the EDSL Building
    /// Simulator documentation states the Inter-Zone Air Movement flow rate as a time-varying <b>mass flow
    /// rate</b> and the Inter-Zone Air Movement table gives the unit as <b>kg/s</b>, which a licensed TBD
    /// confirms - the profile written into one reports its own <c>units</c> as <c>kg/s</c>.
    /// </para>
    /// <para>
    /// Neither type says which it is, so passing the SAM number straight through compiles, balances,
    /// simulates and is wrong by the density of air - about 21% low on every flow in the dwelling, with no
    /// error anywhere. These tests pin the conversion and stop it being undone.
    /// </para>
    /// </summary>
    [TestFixture]
    public class IZAMMassFlowTests
    {
        /// <summary>45.5 l/s, the supply duty of a bedroom of the licensed acceptance dwelling.</summary>
        private const double airFlow_Bedroom_M3PerSecond = 0.0455;

        private const double tolerance = 1e-12;

        // =================================================================================================
        // 1. The conversion
        // =================================================================================================

        /// <summary>
        /// A volumetric flow becomes a mass flow, and the number changes. The assertion that matters is the
        /// inequality: a "conversion" that returned its argument would satisfy every round-trip test below
        /// and would be exactly the defect.
        /// </summary>
        [Test]
        public void AVolumetricFlow_IsWrittenAsAMassFlow()
        {
            double massFlow = TasQuery.IZAMMassFlow_KgPerSecond(airFlow_Bedroom_M3PerSecond);

            Assert.That(massFlow, Is.EqualTo(airFlow_Bedroom_M3PerSecond * TasQuery.IZAMAirDensity_KgPerM3).Within(tolerance));

            Assert.That(massFlow, Is.Not.EqualTo(airFlow_Bedroom_M3PerSecond),
                "A TBD inter-zone air movement is a mass flow rate in kg/s. Writing the SAM m3/s value into it understates the dwelling's ventilation by the density of air and reports no error at all.");

            //And it is larger, not smaller: air is denser than 1 kg/m3.
            Assert.That(massFlow, Is.GreaterThan(airFlow_Bedroom_M3PerSecond));
        }

        /// <summary>
        /// The density is SAM's own, not a second one minted here. <c>Modify.AddAirMovementObjects</c>
        /// already writes <see cref="Core.FluidProperty.Air.Density"/> as an air handling unit's density
        /// profile, so the mass flow a TBD carries and the density the rest of SAM states about the same air
        /// agree by construction.
        /// </summary>
        [Test]
        public void TheDensity_IsSAMsOwnAuthorityAndNotALocalLiteral()
        {
            Assert.That(TasQuery.IZAMAirDensity_KgPerM3, Is.EqualTo(Core.FluidProperty.Air.Density));

            //Sanity, not policy: whatever the authority says, it has to be air at ordinary conditions.
            Assert.That(TasQuery.IZAMAirDensity_KgPerM3, Is.InRange(1.1, 1.3));
        }

        /// <summary>
        /// The readback direction, so that a TBD written by this library can be compared with the SAM
        /// movement and with the Approved Document F terminal duty without anybody restating the density at
        /// the comparison.
        /// </summary>
        [TestCase(0.0455)]
        [TestCase(0.0325)]
        [TestCase(0.008)]
        [TestCase(0.156)]
        public void TheConversion_RoundTripsBackToTheSAMVolumeAndTheDesignDuty(double airFlow_M3PerSecond)
        {
            double massFlow = TasQuery.IZAMMassFlow_KgPerSecond(airFlow_M3PerSecond);

            Assert.That(TasQuery.IZAMVolumeFlow_M3PerSecond(massFlow), Is.EqualTo(airFlow_M3PerSecond).Within(1e-12));

            //l/s, which is the unit Approved Document F states the terminal duty in.
            Assert.That(TasQuery.IZAMVolumeFlow_M3PerSecond(massFlow) * 1000.0, Is.EqualTo(airFlow_M3PerSecond * 1000.0).Within(1e-9));
        }

        /// <summary>
        /// A flow the model does not state stays unstated. Turning <c>NaN</c> into a number here would write
        /// a zero-flow inter-zone air movement into a file and call it a design.
        /// </summary>
        [Test]
        public void AnUnstatedFlow_IsNotTurnedIntoANumber()
        {
            Assert.That(TasQuery.IZAMMassFlow_KgPerSecond(double.NaN), Is.NaN);
            Assert.That(TasQuery.IZAMVolumeFlow_M3PerSecond(double.NaN), Is.NaN);
            Assert.That(TasQuery.IZAMMassFlow_KgPerSecond(0), Is.EqualTo(0));
        }

        // =================================================================================================
        // 2. What the write path actually puts in the file
        // =================================================================================================

        /// <summary>
        /// <b>The production write seam, run for real.</b> <c>Modify.UpdateIZAMProfile</c> is what every one
        /// of <c>Modify.UpdateIZAMs</c>'s inter-zone air movements goes through, and
        /// <see cref="FakeProfile"/> is a plain managed implementation of <c>TBD.profile</c> - so this is the
        /// shipped code writing into the shipped field, with no licence and no COM server involved.
        /// </summary>
        [Test]
        public void TheWriteSeam_PutsTheMassFlowIntoTheProfileFactor()
        {
            FakeProfile profile_TBD = new();

            Assert.That(TasModify.UpdateIZAMProfile(profile_TBD, ContinuousProfile(), airFlow_Bedroom_M3PerSecond), Is.True);

            //factor x value is the flow TAS reads. The profile says WHEN, the factor says HOW MUCH.
            Assert.That(profile_TBD.factor * profile_TBD.value, Is.EqualTo(airFlow_Bedroom_M3PerSecond * TasQuery.IZAMAirDensity_KgPerM3).Within(1e-7));

            Assert.That(profile_TBD.factor, Is.Not.EqualTo((float)airFlow_Bedroom_M3PerSecond),
                "The volumetric SAM value reached the TBD profile unconverted, which is the regression this seam exists to prevent.");
        }

        /// <summary>
        /// <b>Every shape is converted, so a balanced network stays balanced.</b> One density is applied to
        /// the whole graph - outside into the unit, unit into a room, room to room, room back to the unit and
        /// the unit's exhaust - so each node's sum is scaled by the same factor and conservation survives the
        /// change of units exactly. Converting some shapes and not others would be worse than converting
        /// none: TAS refuses an unbalanced zone outright.
        /// </summary>
        [Test]
        public void EveryShapeOfMovement_IsConvertedAtTheSameDensityAndTheNodeStillBalances()
        {
            //The air handling unit node of the licensed acceptance dwelling: 156 l/s in from outside and
            //156 l/s of room extract arriving, 156 l/s of supply delivered and 156 l/s exhausted.
            double[] inward = [0.156, 0.044, 0.044, 0.044, 0.008, 0.008, 0.008];
            double[] outward = [0.0455, 0.0455, 0.0325, 0.0325, 0.156];

            double total_In = 0;
            double total_Out = 0;

            foreach (double airFlow in inward)
            {
                FakeProfile profile_TBD = new();
                TasModify.UpdateIZAMProfile(profile_TBD, ContinuousProfile(), airFlow);
                total_In += profile_TBD.factor * profile_TBD.value;
            }

            foreach (double airFlow in outward)
            {
                FakeProfile profile_TBD = new();
                TasModify.UpdateIZAMProfile(profile_TBD, ContinuousProfile(), airFlow);
                total_Out += profile_TBD.factor * profile_TBD.value;
            }

            Assert.That(total_In, Is.EqualTo(total_Out).Within(1e-6), "The unit's zone does not conserve mass flow, which TAS refuses to simulate.");

            //And it is genuinely mass, not the volume flow relabelled.
            Assert.That(total_In, Is.EqualTo(0.312 * TasQuery.IZAMAirDensity_KgPerM3).Within(1e-6));
        }

        // =================================================================================================
        // Fixture
        // =================================================================================================

        /// <summary>A profile that runs at full flow all the time, which is what a continuous system does.</summary>
        private static Profile ContinuousProfile()
        {
            return new Profile("Continuous", new List<double>() { 1.0 });
        }
    }
}
