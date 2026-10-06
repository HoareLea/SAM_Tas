// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.IO;
using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Tas.TM59;
using SAM.Core;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Regression coverage for the TM59 apartment room-use classification bug: canonical
    /// apartment condition names carry bedroom-count metadata ("1/2/3 Bed Apt.") ahead of the
    /// actual room function ("Kitchen", "Living Room", "Living Room/Kitchen"), and the legacy
    /// fuzzy matcher used to see "Bed" and misclassify the whole condition as Sleeping before
    /// reaching the function suffix.
    /// </summary>
    [TestFixture]
    public class RoomUseTests
    {
        private static TM59Manager tM59Manager;

        [OneTimeSetUp]
        public static void OneTimeSetUp()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "SAM_InternalConditionTextMap_TM59.JSON");
            string json = File.ReadAllText(path);
            TextMap textMap = Core.Create.IJSAMObject<TextMap>(json);

            tM59Manager = new TM59Manager(textMap);
        }

        private static Space SpaceWithInternalCondition(string internalConditionName, string spaceName = "Space")
        {
            Space space = new Space(spaceName);
            space.InternalCondition = new InternalCondition(internalConditionName);
            return space;
        }

        [TestCase("1 Bed Apt. Kitchen")]
        [TestCase("2 Bed Apt. Kitchen")]
        [TestCase("3 Bed Apt. Kitchen")]
        [TestCase("1 BED APT. KITCHEN")]
        public void ApartmentKitchen_IsNotClassifiedAsBedroom(string internalConditionName)
        {
            // Previously "1 Bed Apt. Kitchen" produced Bedroom because "Bed" matched Sleeping
            // before the "Kitchen" function was ever considered.
            Space space = SpaceWithInternalCondition(internalConditionName);

            RoomUse roomUse = tM59Manager.RoomUse(space);

            Assert.That(roomUse, Is.EqualTo(RoomUse.LivingRoomOrKitchen));
        }

        [TestCase("1 Bed Apt. Living Room")]
        [TestCase("2 Bed Apt. Living Room")]
        [TestCase("3 Bed Apt. Living Room")]
        public void ApartmentLivingRoom_IsNotClassifiedAsBedroom(string internalConditionName)
        {
            Space space = SpaceWithInternalCondition(internalConditionName);

            RoomUse roomUse = tM59Manager.RoomUse(space);

            Assert.That(roomUse, Is.EqualTo(RoomUse.LivingRoomOrKitchen));
        }

        [TestCase("1 Bed Apt. Living Room/Kitchen")]
        [TestCase("2 Bed Apt. Living Room/Kitchen")]
        [TestCase("3 Bed Apt. Living Room/Kitchen")]
        public void ApartmentLivingRoomKitchen_RetainsCombinedUse(string internalConditionName)
        {
            Space space = SpaceWithInternalCondition(internalConditionName);

            RoomUse roomUse = tM59Manager.RoomUse(space);

            Assert.That(roomUse, Is.EqualTo(RoomUse.LivingRoomOrKitchen));
        }

        [TestCase("Single Bedroom")]
        [TestCase("Double Bedroom")]
        public void RealBedroom_RemainsBedroom(string internalConditionName)
        {
            Space space = SpaceWithInternalCondition(internalConditionName);

            RoomUse roomUse = tM59Manager.RoomUse(space);

            Assert.That(roomUse, Is.EqualTo(RoomUse.Bedroom));
        }

        [Test]
        public void Studio_RetainsExistingCombinedBehaviour()
        {
            // Studio genuinely functions as sleeping + living + cooking combined (its TextMap
            // entry maps "studio" into all three roles) - unlike the apartment-metadata cases,
            // this is not a false match, so the fix must not change it.
            Space space = SpaceWithInternalCondition("Studio");

            RoomUse roomUse = tM59Manager.RoomUse(space);

            Assert.That(roomUse, Is.EqualTo(RoomUse.Bedroom));
        }

        [TestCase("TM59_Bathroom/internal corridors")]
        [TestCase("TM59_Communal Corridor (including pipework gains)")]
        [TestCase("TM59_Stairs")]
        [TestCase("TM59_Cupboard/riser/lift/void")]
        [TestCase("TM59_Cupboard with HIU")]
        [TestCase("TM59_Riser Communal pipework")]
        public void NonHabitableCondition_RetainsOther(string internalConditionName)
        {
            Space space = SpaceWithInternalCondition(internalConditionName);

            RoomUse roomUse = tM59Manager.RoomUse(space);

            Assert.That(roomUse, Is.EqualTo(RoomUse.Other));
        }

        [Test]
        public void MissingInternalCondition_FallsBackToSpaceName()
        {
            // No InternalCondition at all - the Space-name fallback must still work.
            Space space = new Space("Master Bedroom");

            RoomUse roomUse = tM59Manager.RoomUse(space);

            Assert.That(roomUse, Is.EqualTo(RoomUse.Bedroom));
        }

        [Test]
        public void GenericInternalCondition_FallsBackToSpaceName()
        {
            // Unrecognised/generic InternalCondition name - should fall back to the Space name,
            // exactly as before this fix (precedence order is unchanged, only the apartment
            // bed-count metadata case is corrected).
            Space space = SpaceWithInternalCondition("Zone 04", "Kitchen");

            RoomUse roomUse = tM59Manager.RoomUse(space);

            Assert.That(roomUse, Is.EqualTo(RoomUse.LivingRoomOrKitchen));
        }

        [Test]
        public void NullSpace_ReturnsUndefined()
        {
            Assert.That(tM59Manager.RoomUse((Space)null), Is.EqualTo(RoomUse.Undefined));
        }
    }
}
