// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Core;
using System.Collections.Generic;

using TasModify = SAM.Analytical.Tas.Modify;
using AnalyticalCreate = SAM.Analytical.Create;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The stale-exhaust regression found in the second review round.</b>
    /// <para>
    /// A TBD written by an earlier export can carry <c>"IZAM &lt;AHU&gt; TO OUTSIDE"</c> from a topology
    /// this run no longer builds - the unit's own exhaust, from before its extract was flattened to leave
    /// from the rooms instead (see <c>Query.DesignTerminalExtractFlattening</c>). <c>Modify.UpdateIZAMs</c>
    /// used to queue that name for removal only from INSIDE the loop that walks the unit's CURRENT outward
    /// movements, so a re-export of a model where that movement no longer exists never queued it, and the
    /// stale exhaust survived - unmatched outflow on the unit's zone, which TAS refuses to simulate.
    /// </para>
    /// <para>
    /// Pinned at <c>Modify.ResolveAirHandlingUnitMovements</c>, the pure SAM.Analytical resolution step
    /// <c>UpdateIZAMs</c> now delegates to before it touches the TBD file. No TBD/COM type appears anywhere
    /// in it, so this runs with no TAS licence, install or COM server.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ResolveAirHandlingUnitMovementsTests
    {
        [Test]
        public void AnAHUWithNoCurrentOutwardMovement_StillQueuesItsOutwardIZAMNameForRemoval()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            AirHandlingUnit airHandlingUnit = AnalyticalCreate.AirHandlingUnit("MVHR-01");
            adjacencyCluster.AddObject(airHandlingUnit);

            AirHandlingUnitAirMovement airHandlingUnitAirMovement = new AirHandlingUnitAirMovement(airHandlingUnit.Name);
            adjacencyCluster.AddObject(airHandlingUnitAirMovement);
            adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitAirMovement);

            //No outward SpaceAirMovement is related to the unit at all - the shape a re-export takes after
            //the unit's extract has been flattened to leave from the rooms instead, or after the topology
            //otherwise changed to give this AHU no exhaust of its own.
            TasModify.ResolveAirHandlingUnitMovements(
                adjacencyCluster,
                new List<AirHandlingUnit> { airHandlingUnit },
                new HashSet<System.Guid>(),
                out Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement> ahuMovements,
                out Dictionary<AirHandlingUnit, List<SpaceAirMovement>> ahuOutwardMovements,
                out HashSet<string> icNamesToReplace,
                out HashSet<string> izamNamesToReplace);

            Assert.That(ahuMovements.ContainsKey(airHandlingUnit), Is.True);
            Assert.That(ahuOutwardMovements.ContainsKey(airHandlingUnit), Is.False, "No current outward movement exists, so none should be (re)written.");

            Assert.That(izamNamesToReplace, Does.Contain(string.Format("IZAM {0} TO OUTSIDE", airHandlingUnit.Name)),
                "A stale outward IZAM from an earlier export must be queued for removal even when this run builds no replacement - a regression found in review.");
        }

        [Test]
        public void AnAHUWithACurrentOutwardMovement_QueuesItAndCarriesItForward()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            AirHandlingUnit airHandlingUnit = AnalyticalCreate.AirHandlingUnit("MVHR-01");
            adjacencyCluster.AddObject(airHandlingUnit);

            AirHandlingUnitAirMovement airHandlingUnitAirMovement = new AirHandlingUnitAirMovement(airHandlingUnit.Name);
            adjacencyCluster.AddObject(airHandlingUnitAirMovement);
            adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitAirMovement);

            ObjectReference reference_AHU = new ObjectReference(airHandlingUnit);
            SpaceAirMovement outward = new SpaceAirMovement(string.Format("{0} exhaust", airHandlingUnit.Name), 0.156, reference_AHU.ToString(), null);
            adjacencyCluster.AddObject(outward);
            adjacencyCluster.AddRelation(outward, airHandlingUnit);

            TasModify.ResolveAirHandlingUnitMovements(
                adjacencyCluster,
                new List<AirHandlingUnit> { airHandlingUnit },
                new HashSet<System.Guid>(),
                out Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement> ahuMovements,
                out Dictionary<AirHandlingUnit, List<SpaceAirMovement>> ahuOutwardMovements,
                out HashSet<string> icNamesToReplace,
                out HashSet<string> izamNamesToReplace);

            Assert.That(ahuOutwardMovements.ContainsKey(airHandlingUnit), Is.True);
            Assert.That(ahuOutwardMovements[airHandlingUnit], Does.Contain(outward));

            Assert.That(izamNamesToReplace, Does.Contain(string.Format("IZAM {0} TO OUTSIDE", airHandlingUnit.Name)));
        }

        [Test]
        public void AnAHUWithNoAirHandlingUnitAirMovement_IsNotProcessedAtAll()
        {
            //A modeller-authored unit this export does not touch at all: it carries no
            //AirHandlingUnitAirMovement, so nothing of this run's realization is on it, and its own
            //hand-built IZAMs - if any - must not be swept up by this run's removal set merely because they
            //reference an AHU.
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            AirHandlingUnit airHandlingUnit = AnalyticalCreate.AirHandlingUnit("Modeller AHU");
            adjacencyCluster.AddObject(airHandlingUnit);

            TasModify.ResolveAirHandlingUnitMovements(
                adjacencyCluster,
                new List<AirHandlingUnit> { airHandlingUnit },
                new HashSet<System.Guid>(),
                out Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement> ahuMovements,
                out Dictionary<AirHandlingUnit, List<SpaceAirMovement>> ahuOutwardMovements,
                out HashSet<string> icNamesToReplace,
                out HashSet<string> izamNamesToReplace);

            Assert.That(ahuMovements, Is.Empty);
            Assert.That(ahuOutwardMovements, Is.Empty);
            Assert.That(icNamesToReplace, Is.Empty);
            Assert.That(izamNamesToReplace, Is.Empty);
        }

        /// <summary>
        /// The exhaust-suppression case <c>Query.DesignTerminalExtractFlattening</c> feeds in via
        /// <c>guids_AirHandlingUnit_NoExhaust</c>: a current outward movement exists, but writing it would
        /// take the same extract air out of the building twice because the room's own extract was
        /// flattened straight to outside instead. The name is still queued, so a stale exhaust from BEFORE
        /// the flattening does not survive a re-export.
        /// </summary>
        [Test]
        public void AFlattenedAHU_QueuesItsOutwardNameButDoesNotCarryTheMovementForward()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            AirHandlingUnit airHandlingUnit = AnalyticalCreate.AirHandlingUnit("MVHR-01");
            adjacencyCluster.AddObject(airHandlingUnit);

            AirHandlingUnitAirMovement airHandlingUnitAirMovement = new AirHandlingUnitAirMovement(airHandlingUnit.Name);
            adjacencyCluster.AddObject(airHandlingUnitAirMovement);
            adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitAirMovement);

            ObjectReference reference_AHU = new ObjectReference(airHandlingUnit);
            SpaceAirMovement outward = new SpaceAirMovement(string.Format("{0} exhaust", airHandlingUnit.Name), 0.156, reference_AHU.ToString(), null);
            adjacencyCluster.AddObject(outward);
            adjacencyCluster.AddRelation(outward, airHandlingUnit);

            TasModify.ResolveAirHandlingUnitMovements(
                adjacencyCluster,
                new List<AirHandlingUnit> { airHandlingUnit },
                new HashSet<System.Guid> { airHandlingUnit.Guid },
                out Dictionary<AirHandlingUnit, AirHandlingUnitAirMovement> ahuMovements,
                out Dictionary<AirHandlingUnit, List<SpaceAirMovement>> ahuOutwardMovements,
                out HashSet<string> icNamesToReplace,
                out HashSet<string> izamNamesToReplace);

            Assert.That(ahuOutwardMovements.ContainsKey(airHandlingUnit), Is.False, "Suppressed for this AHU, so nothing should be (re)written.");
            Assert.That(izamNamesToReplace, Does.Contain(string.Format("IZAM {0} TO OUTSIDE", airHandlingUnit.Name)));
        }
    }
}
