// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using System;
using System.Collections.Generic;

using TasModify = SAM.Analytical.Tas.Modify;
using AnalyticalCreate = SAM.Analytical.Create;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>One generated plant zone per air handling unit, however many times the model is converted.</b>
    /// <para>
    /// <c>Modify.UpdateIZAMs</c> writes each unit a small TAS zone named after it. It used to build that zone
    /// unconditionally - a fresh 3 x 3 x 2 box, renamed to the unit - with nothing looking for the one it had
    /// written before. A Part O optimisation round warm starts from a copy of the canonical TBD, which has
    /// already been through that method once, so the round appended a second zone per unit and left the first
    /// behind: same name, and stripped of its internal condition by the remove-by-name step that runs before
    /// the rebuild. Three MVHR units came back as six zones, three of them without internal conditions.
    /// </para>
    /// <para>
    /// The fix is <c>Modify.ResolvePlantZoneReuse</c> and <see cref="PlantZoneIdentity"/>: the zone states
    /// which unit it belongs to, and a later run finds it and updates it. These tests pin the idempotency
    /// that gives, by replaying the create-or-reuse decision the way <c>UpdateIZAMs</c> makes it - see
    /// <see cref="Round"/> - over rounds of the shape a Part O optimisation actually performs.
    /// </para>
    /// <para>
    /// No TBD/COM type appears anywhere here, so this runs with no TAS licence, install or COM server. That is
    /// the same reason <c>ResolveAirHandlingUnitMovements</c> exists as a separate pure step, and this is the
    /// second decision lifted out of the COM loop for it.
    /// </para>
    /// </summary>
    [TestFixture]
    public class PlantZoneIdempotencyTests
    {
        /// <summary>A TBD zone as this fixture needs it: the two strings the resolution reads and writes.</summary>
        private sealed class FakeZone
        {
            public string Name;
            public string Description;
        }

        /// <summary>
        /// One conversion round, replaying <c>Modify.UpdateIZAMs</c>'s plant-zone half exactly: resolve what
        /// can be reused, take that zone or append a new one, then rename and stamp it. Everything the
        /// production loop does between those steps - the internal condition, the intake and exhaust
        /// movements - is written onto whichever zone comes out and does not affect how many there are.
        /// </summary>
        private static void Round(List<FakeZone> zones, List<AirHandlingUnit> airHandlingUnits, IEnumerable<string> spaceNames)
        {
            List<PlantZoneCandidate> candidates = new List<PlantZoneCandidate>();
            for (int i = 0; i < zones.Count; i++)
            {
                candidates.Add(new PlantZoneCandidate(i, zones[i].Name, zones[i].Description));
            }

            Dictionary<Guid, PlantZoneCandidate> reuse = TasModify.ResolvePlantZoneReuse(airHandlingUnits, candidates, spaceNames);

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                FakeZone zone;

                if (reuse.TryGetValue(airHandlingUnit.Guid, out PlantZoneCandidate candidate) && candidate != null)
                {
                    zone = zones[candidate.Index];
                }
                else
                {
                    zone = new FakeZone();
                    zones.Add(zone);
                }

                zone.Name = airHandlingUnit.Name;

                string description = PlantZoneIdentity.Compose(zone.Description, airHandlingUnit.Guid);
                if (!string.IsNullOrEmpty(description))
                {
                    zone.Description = description;
                }
            }
        }

        private static int Count(List<FakeZone> zones, AirHandlingUnit airHandlingUnit)
        {
            int result = 0;
            foreach (FakeZone zone in zones)
            {
                if (PlantZoneIdentity.Parse(zone.Description) == airHandlingUnit.Guid)
                {
                    result++;
                }
            }

            return result;
        }

        private static AirHandlingUnit Unit(string name)
        {
            return AnalyticalCreate.AirHandlingUnit(name);
        }

        // ---- The defect ------------------------------------------------------------------------------

        [Test]
        public void FirstConversion_WritesExactlyOnePlantZone()
        {
            AirHandlingUnit airHandlingUnit = Unit("MVHR-01");
            List<FakeZone> zones = new List<FakeZone>();

            Round(zones, new List<AirHandlingUnit> { airHandlingUnit }, new List<string>());

            Assert.That(zones.Count, Is.EqualTo(1));
            Assert.That(zones[0].Name, Is.EqualTo("MVHR-01"));
            Assert.That(PlantZoneIdentity.Parse(zones[0].Description), Is.EqualTo(airHandlingUnit.Guid),
                "The zone must state which unit it belongs to, or the next round cannot find it.");
        }

        [Test]
        public void ASecondIdenticalConversion_StillLeavesExactlyOnePlantZone()
        {
            AirHandlingUnit airHandlingUnit = Unit("MVHR-01");
            List<AirHandlingUnit> airHandlingUnits = new List<AirHandlingUnit> { airHandlingUnit };
            List<FakeZone> zones = new List<FakeZone>();

            Round(zones, airHandlingUnits, new List<string>());
            Round(zones, airHandlingUnits, new List<string>());

            Assert.That(zones.Count, Is.EqualTo(1),
                "This is the defect: the second conversion used to append a second zone rather than update the first.");
        }

        [Test]
        public void BaselineThenThreeOptimisationRounds_LeaveOneZonePerUnit()
        {
            //The shape a Part O optimisation actually runs: a baseline conversion, then Opt01, Opt02 and
            //OptMax, each warm starting from a copy of the canonical TBD the baseline produced - so each one
            //opens a file that already carries the baseline's plant zones.
            List<AirHandlingUnit> airHandlingUnits = new List<AirHandlingUnit>
            {
                Unit("MVHR-01"),
                Unit("MVHR-02"),
                Unit("MVHR-03"),
            };

            List<FakeZone> canonical = new List<FakeZone>();
            Round(canonical, airHandlingUnits, new List<string>());

            Assert.That(canonical.Count, Is.EqualTo(3), "The baseline conversion writes one zone per unit.");

            foreach (string round in new string[] { "Opt01", "Opt02", "OptMax" })
            {
                //The warm start is a file copy of the canonical, which every round starts from afresh.
                List<FakeZone> zones = new List<FakeZone>();
                foreach (FakeZone zone in canonical)
                {
                    zones.Add(new FakeZone { Name = zone.Name, Description = zone.Description });
                }

                Round(zones, airHandlingUnits, new List<string>());

                Assert.That(zones.Count, Is.EqualTo(3),
                    string.Format("{0} must reuse the three plant zones its warm start handed it, not append three more.", round));

                foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
                {
                    Assert.That(Count(zones, airHandlingUnit), Is.EqualTo(1),
                        string.Format("{0}: {1} must own exactly one plant zone.", round, airHandlingUnit.Name));
                }
            }
        }

        [Test]
        public void RepeatedRoundsOnOneAccumulatingFile_StillLeaveOneZonePerUnit()
        {
            //The harsher case: rounds applied one after another to the SAME file rather than each to a fresh
            //copy of the canonical. Idempotency has to hold here too, or a chain of reconversions grows.
            List<AirHandlingUnit> airHandlingUnits = new List<AirHandlingUnit>
            {
                Unit("MVHR-01"),
                Unit("MVHR-02"),
            };

            List<FakeZone> zones = new List<FakeZone>();

            for (int i = 0; i < 6; i++)
            {
                Round(zones, airHandlingUnits, new List<string>());
            }

            Assert.That(zones.Count, Is.EqualTo(2));
            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                Assert.That(Count(zones, airHandlingUnit), Is.EqualTo(1));
            }
        }

        [Test]
        public void SeveralDwellingsEachWithTheirOwnUnit_KeepOneZoneEach()
        {
            List<AirHandlingUnit> airHandlingUnits = new List<AirHandlingUnit>
            {
                Unit("Flat 1 MVHR"),
                Unit("Flat 2 MVHR"),
                Unit("Flat 3 MVHR"),
                Unit("Flat 4 MVHR"),
            };

            List<FakeZone> zones = new List<FakeZone>();
            Round(zones, airHandlingUnits, new List<string>());
            Round(zones, airHandlingUnits, new List<string>());

            Assert.That(zones.Count, Is.EqualTo(4));
            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                Assert.That(Count(zones, airHandlingUnit), Is.EqualTo(1), airHandlingUnit.Name);
            }
        }

        // ---- Identity is the guid, not the name -------------------------------------------------------

        [Test]
        public void TwoUnitsSharingOneName_KeepSeparateZones()
        {
            //Presentation names are not unique and are not the identity. Two units called the same thing must
            //still own one zone each - which name matching alone could not deliver.
            AirHandlingUnit airHandlingUnit_1 = Unit("MVHR-01");
            AirHandlingUnit airHandlingUnit_2 = Unit("MVHR-01");

            Assert.That(airHandlingUnit_1.Guid, Is.Not.EqualTo(airHandlingUnit_2.Guid));

            List<AirHandlingUnit> airHandlingUnits = new List<AirHandlingUnit> { airHandlingUnit_1, airHandlingUnit_2 };
            List<FakeZone> zones = new List<FakeZone>();

            Round(zones, airHandlingUnits, new List<string>());
            Round(zones, airHandlingUnits, new List<string>());
            Round(zones, airHandlingUnits, new List<string>());

            Assert.That(zones.Count, Is.EqualTo(2));
            Assert.That(Count(zones, airHandlingUnit_1), Is.EqualTo(1));
            Assert.That(Count(zones, airHandlingUnit_2), Is.EqualTo(1));
        }

        [Test]
        public void RenamingTheUnit_MovesItsExistingZoneRatherThanAddingAnother()
        {
            AirHandlingUnit airHandlingUnit = Unit("MVHR-01");
            List<FakeZone> zones = new List<FakeZone>();

            Round(zones, new List<AirHandlingUnit> { airHandlingUnit }, new List<string>());

            //The same unit - same guid - now called something else.
            AirHandlingUnit airHandlingUnit_Renamed = new AirHandlingUnit(
                airHandlingUnit.Guid,
                "Flat 1 MVHR",
                airHandlingUnit.SummerSupplyTemperature,
                airHandlingUnit.WinterSupplyTemperature);

            Round(zones, new List<AirHandlingUnit> { airHandlingUnit_Renamed }, new List<string>());

            Assert.That(zones.Count, Is.EqualTo(1), "A rename must not orphan the old zone and build a new one.");
            Assert.That(zones[0].Name, Is.EqualTo("Flat 1 MVHR"), "The existing zone is renamed with its unit.");
        }

        // ---- What must not be touched -----------------------------------------------------------------

        [Test]
        public void ZonesBelongingToNobody_ArePreserved()
        {
            //Rooms and a modeller's own zones. None of them is claimed, renamed or removed.
            List<FakeZone> zones = new List<FakeZone>
            {
                new FakeZone { Name = "Studio 1_0", Description = "[Id]=1001" },
                new FakeZone { Name = "Bathroom_2", Description = "[Id]=1002" },
                new FakeZone { Name = "Plant Room", Description = "A note the TAS user wrote" },
            };

            AirHandlingUnit airHandlingUnit = Unit("MVHR-01");
            List<string> spaceNames = new List<string> { "Studio 1_0", "Bathroom_2" };

            Round(zones, new List<AirHandlingUnit> { airHandlingUnit }, spaceNames);
            Round(zones, new List<AirHandlingUnit> { airHandlingUnit }, spaceNames);

            Assert.That(zones.Count, Is.EqualTo(4), "Three existing zones plus one plant zone, and no more on the second round.");
            Assert.That(zones[0].Name, Is.EqualTo("Studio 1_0"));
            Assert.That(zones[0].Description, Is.EqualTo("[Id]=1001"));
            Assert.That(zones[1].Name, Is.EqualTo("Bathroom_2"));
            Assert.That(zones[2].Name, Is.EqualTo("Plant Room"));
            Assert.That(zones[2].Description, Is.EqualTo("A note the TAS user wrote"));
        }

        [Test]
        public void ARoomNamedAfterTheUnit_IsNotSeizedAsItsPlantZone()
        {
            //A space of the model that happens to carry the unit's name is a room. The generated plant zone
            //has no space behind it, so adoption must skip this and build its own.
            List<FakeZone> zones = new List<FakeZone>
            {
                new FakeZone { Name = "MVHR-01", Description = "[Id]=1001" },
            };

            AirHandlingUnit airHandlingUnit = Unit("MVHR-01");
            List<string> spaceNames = new List<string> { "MVHR-01" };

            Round(zones, new List<AirHandlingUnit> { airHandlingUnit }, spaceNames);

            Assert.That(zones.Count, Is.EqualTo(2));
            Assert.That(zones[0].Description, Is.EqualTo("[Id]=1001"), "The room is untouched.");
            Assert.That(PlantZoneIdentity.Parse(zones[1].Description), Is.EqualTo(airHandlingUnit.Guid));
        }

        [Test]
        public void APlantZoneWrittenBeforeTheIdentityExisted_IsAdoptedNotDuplicated()
        {
            //Every TBD in existence when this fix landed carries plant zones with no identity on them. They
            //are adopted by name once, and answer by guid from then on.
            List<FakeZone> zones = new List<FakeZone>
            {
                new FakeZone { Name = "MVHR-01", Description = null },
            };

            AirHandlingUnit airHandlingUnit = Unit("MVHR-01");

            Round(zones, new List<AirHandlingUnit> { airHandlingUnit }, new List<string>());

            Assert.That(zones.Count, Is.EqualTo(1), "The legacy zone is the unit's zone - not something to duplicate.");
            Assert.That(PlantZoneIdentity.Parse(zones[0].Description), Is.EqualTo(airHandlingUnit.Guid));

            Round(zones, new List<AirHandlingUnit> { airHandlingUnit }, new List<string>());

            Assert.That(zones.Count, Is.EqualTo(1));
        }

        [Test]
        public void AZoneClaimedByAnotherUnit_IsNeverAdoptedByName()
        {
            AirHandlingUnit airHandlingUnit_Owner = Unit("MVHR-01");
            AirHandlingUnit airHandlingUnit_Other = Unit("MVHR-01");

            List<FakeZone> zones = new List<FakeZone>
            {
                new FakeZone { Name = "MVHR-01", Description = PlantZoneIdentity.Compose(null, airHandlingUnit_Owner.Guid) },
            };

            Round(zones, new List<AirHandlingUnit> { airHandlingUnit_Other }, new List<string>());

            Assert.That(zones.Count, Is.EqualTo(2), "The name matches, but the zone already states a different owner.");
            Assert.That(Count(zones, airHandlingUnit_Owner), Is.EqualTo(1));
            Assert.That(Count(zones, airHandlingUnit_Other), Is.EqualTo(1));
        }

        // ---- The description is shared ----------------------------------------------------------------

        [Test]
        public void ForeignDescriptionSegments_SurviveTheStamp()
        {
            string description = PlantZoneIdentity.Compose("[Id]=1001; A note the TAS user wrote", Guid.NewGuid());

            Assert.That(description, Does.Contain("[Id]=1001"));
            Assert.That(description, Does.Contain("A note the TAS user wrote"));
            Assert.That(description, Does.Contain(PlantZoneIdentity.Marker));
        }

        [Test]
        public void RestampingTheSameZone_ReplacesTheIdentityRatherThanAppendingASecond()
        {
            Guid guid_1 = Guid.NewGuid();
            Guid guid_2 = Guid.NewGuid();

            string description = PlantZoneIdentity.Compose(PlantZoneIdentity.Compose(null, guid_1), guid_2);

            Assert.That(PlantZoneIdentity.Parse(description), Is.EqualTo(guid_2));
            Assert.That(description.Split(new string[] { PlantZoneIdentity.Marker }, StringSplitOptions.None).Length, Is.EqualTo(2),
                "Exactly one identity segment - a second would make the zone's owner ambiguous.");
        }

        [Test]
        public void ADescriptionStatingNoIdentity_ParsesAsEmptyRatherThanThrowing()
        {
            Assert.That(PlantZoneIdentity.Parse(null), Is.EqualTo(Guid.Empty));
            Assert.That(PlantZoneIdentity.Parse(string.Empty), Is.EqualTo(Guid.Empty));
            Assert.That(PlantZoneIdentity.Parse("[Id]=1001; [LevelName]=Level 01"), Is.EqualTo(Guid.Empty));
            Assert.That(PlantZoneIdentity.Parse(PlantZoneIdentity.Marker + "not-a-guid"), Is.EqualTo(Guid.Empty),
                "A malformed identity is no identity - never a throw, and never a wrong match.");
        }

        // ---- The airflow semantics are untouched ------------------------------------------------------

        [Test]
        public void ReusingAPlantZone_DoesNotChangeTheAirflowAndIZAMPlan()
        {
            //The plant-zone decision and the airflow decision are independent steps over the same model.
            //Whatever zone a round ends up writing to, the internal conditions and inter-zone air movements
            //it writes - and the ones it removes first, which is what keeps a unit's supply and extract
            //balanced across a re-export - are resolved from the SAM model alone and are identical every
            //round. Part F requirements, DesignAirFlow, the selected equipment and OperatingAirFlow are read
            //by neither step and are pinned in SAM's own suite.
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            AirHandlingUnit airHandlingUnit = Unit("MVHR-01");
            adjacencyCluster.AddObject(airHandlingUnit);

            AirHandlingUnitAirMovement airHandlingUnitAirMovement = new AirHandlingUnitAirMovement(airHandlingUnit.Name);
            adjacencyCluster.AddObject(airHandlingUnitAirMovement);
            adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitAirMovement);

            Core.ObjectReference reference = new Core.ObjectReference(airHandlingUnit);
            SpaceAirMovement outward = new SpaceAirMovement("MVHR-01 exhaust", 0.156, reference.ToString(), null);
            adjacencyCluster.AddObject(outward);
            adjacencyCluster.AddRelation(outward, airHandlingUnit);

            List<AirHandlingUnit> airHandlingUnits = new List<AirHandlingUnit> { airHandlingUnit };

            TasModify.ResolveAirHandlingUnitMovements(
                adjacencyCluster, airHandlingUnits, new HashSet<Guid>(),
                out Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement> ahuMovements_1,
                out Dictionary<AirHandlingUnit, List<SpaceAirMovement>> ahuOutwardMovements_1,
                out HashSet<string> icNames_1,
                out HashSet<string> izamNames_1);

            //A round in which the plant zone is reused rather than created.
            List<FakeZone> zones = new List<FakeZone>();
            Round(zones, airHandlingUnits, new List<string>());
            Round(zones, airHandlingUnits, new List<string>());
            Assert.That(zones.Count, Is.EqualTo(1));

            TasModify.ResolveAirHandlingUnitMovements(
                adjacencyCluster, airHandlingUnits, new HashSet<Guid>(),
                out Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement> ahuMovements_2,
                out Dictionary<AirHandlingUnit, List<SpaceAirMovement>> ahuOutwardMovements_2,
                out HashSet<string> icNames_2,
                out HashSet<string> izamNames_2);

            Assert.That(ahuMovements_2[airHandlingUnit].Name, Is.EqualTo(ahuMovements_1[airHandlingUnit].Name));
            Assert.That(ahuOutwardMovements_2[airHandlingUnit].Count, Is.EqualTo(ahuOutwardMovements_1[airHandlingUnit].Count));
            Assert.That(ahuOutwardMovements_2[airHandlingUnit][0].AirFlow, Is.EqualTo(ahuOutwardMovements_1[airHandlingUnit][0].AirFlow),
                "The unit's exhaust flow is what balances its intake; it is not the plant zone's business.");
            Assert.That(icNames_2, Is.EquivalentTo(icNames_1));
            Assert.That(izamNames_2, Is.EquivalentTo(izamNames_1),
                "The same names are removed and rewritten every round, so a reused zone ends up with exactly the movements a fresh one would.");
        }
    }
}
