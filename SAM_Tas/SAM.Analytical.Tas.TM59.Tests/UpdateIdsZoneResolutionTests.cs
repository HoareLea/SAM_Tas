// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Tas;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Regression for the <c>Modify.UpdateIds</c> zone-resolution ordering defect.</b>
    /// <para>
    /// <b>What was wrong.</b> UpdateIds cleared <c>SpaceParameter.ZoneGuid</c> from every space in a first
    /// loop, and only AFTERWARDS tried to resolve the TBD zone by reading that same parameter - so the GUID
    /// path could never fire and resolution always degraded to the exact-name fallback. The stale-stamp
    /// cleanup the clearing exists for is kept; the identity is now captured
    /// (<see cref="Tas.Query.SpaceZoneGuids"/>) BEFORE the clearing loop and the resolution
    /// (<see cref="Tas.Query.ResolvedZone"/>) reads the captured value.
    /// </para>
    /// <para>
    /// <b>Why it matters.</b> A model whose SAM space names differ from the TAS zone names (zones renamed in
    /// TAS, or a space duplicated with the same name) resolved by GUID before the clearing was introduced and
    /// resolved to NOTHING after - and with the zone went every zoneSurface, panel and aperture stamp, which
    /// is what left every aperture part reporting "no building element stamp" downstream.
    /// </para>
    /// <para>
    /// <b>No TAS COM here.</b> <c>TBD.zone</c> cannot be constructed without an installed TAS, so
    /// <c>ResolvedZone</c> is generic over the zone representation and these tests resolve strings. What they
    /// pin is the contract: the captured GUID is authoritative while it identifies a zone, the exact name is
    /// only a fallback, and no match is a refusal - never an arbitrary zone.
    /// </para>
    /// </summary>
    [TestFixture]
    public class UpdateIdsZoneResolutionTests
    {
        private const string ZoneGuid_A = "6f1b0f2e-0000-4000-8000-00000000000a";
        private const string ZoneGuid_B = "6f1b0f2e-0000-4000-8000-00000000000b";

        private static readonly IReadOnlyDictionary<string, string> ZonesByGuid = new Dictionary<string, string>
        {
            [ZoneGuid_A] = "Zone A (renamed in TAS)",
            [ZoneGuid_B] = "Zone B",
        };

        private static readonly IReadOnlyDictionary<string, string> ZonesByName = new Dictionary<string, string>
        {
            ["Zone A (renamed in TAS)"] = "Zone A (renamed in TAS)",
            ["Zone B"] = "Zone B",
        };

        /// <summary>
        /// <b>The test this fix exists for.</b> A space stamped with zone A's GUID whose name equals zone B's
        /// name must resolve to zone A: the GUID is the authoritative identity, the name only a fallback.
        /// Before the fix the stamp was cleared before it was read, and this space silently became zone B.
        /// </summary>
        [Test]
        public void StampedGuid_WinsOverName()
        {
            string resolved = Tas.Query.ResolvedZone(ZoneGuid_A, "Zone B", ZonesByGuid, ZonesByName);

            Assert.That(resolved, Is.EqualTo("Zone A (renamed in TAS)"));
        }

        /// <summary>
        /// A space that was never stamped (a legacy model) keeps resolving by its exact name.
        /// </summary>
        [Test]
        public void NoStamp_StillResolvesByName()
        {
            string resolved = Tas.Query.ResolvedZone(null, "Zone B", ZonesByGuid, ZonesByName);

            Assert.That(resolved, Is.EqualTo("Zone B"));
        }

        /// <summary>
        /// A stamp naming a zone the TBD no longer holds (stale after a rebuild) must not fail the space -
        /// the exact name still resolves it. The stale stamp itself is cleared and re-written with the
        /// current GUID by the caller.
        /// </summary>
        [Test]
        public void StaleStamp_FallsBackToName()
        {
            string resolved = Tas.Query.ResolvedZone("6f1b0f2e-0000-4000-8000-0000000000ff", "Zone B", ZonesByGuid, ZonesByName);

            Assert.That(resolved, Is.EqualTo("Zone B"));
        }

        /// <summary>
        /// Neither identity matching is a refusal, never an arbitrary zone: a stale stamp AND an unmatched
        /// name resolves to null, so the space keeps NO zone stamp rather than borrowing another zone's.
        /// </summary>
        [Test]
        public void NeitherMatches_RefusesRatherThanGuesses()
        {
            string resolved = Tas.Query.ResolvedZone("6f1b0f2e-0000-4000-8000-0000000000ff", "Not A Zone", ZonesByGuid, ZonesByName);

            Assert.That(resolved, Is.Null);
        }

        /// <summary>
        /// A whitespace stamp is not a stated identity and must not even be looked up - it falls straight
        /// through to the name.
        /// </summary>
        [Test]
        public void BlankStamp_IsNotAStatedIdentity()
        {
            string resolved = Tas.Query.ResolvedZone("   ", "Zone B", ZonesByGuid, ZonesByName);

            Assert.That(resolved, Is.EqualTo("Zone B"));
        }

        /// <summary>
        /// The identity a resolved space is re-stamped with - the zone's own current GUID - resolves the same
        /// zone on a repeat pass even when the name has changed in between. This is the idempotency half of
        /// the contract: a repeat UpdateIds into a TBD whose zone GUIDs survived is a no-op, not a reshuffle.
        /// </summary>
        [Test]
        public void RefreshedGuid_ResolvesTheSameZoneOnARepeatPass()
        {
            //First pass: resolved by name (legacy model), re-stamped with the zone's own GUID.
            string resolved = Tas.Query.ResolvedZone(null, "Zone B", ZonesByGuid, ZonesByName);
            Assert.That(resolved, Is.EqualTo("Zone B"));

            //Repeat pass: the name no longer matches anything (renamed meanwhile), the refreshed GUID does.
            string resolvedAgain = Tas.Query.ResolvedZone(ZoneGuid_B, "Zone B (renamed meanwhile)", ZonesByGuid, ZonesByName);
            Assert.That(resolvedAgain, Is.EqualTo("Zone B"));
        }

        /// <summary>
        /// <b>The ordering pin.</b> The stamp must be captured BEFORE it is cleared: capture-then-clear keeps
        /// the identity available for the resolution; clear-then-read (the old ordering) loses it. This test
        /// mirrors UpdateIds's two loops against a live space to prove the captured map is unaffected by the
        /// clearing that follows it.
        /// </summary>
        [Test]
        public void Capture_BeforeClearing_KeepsTheIdentityAvailable()
        {
            Space space = new Space("Zone A (renamed in TAS)");
            space.SetValue(SpaceParameter.ZoneGuid, ZoneGuid_A);

            //The capture, as UpdateIds now performs it before its clearing loop.
            Dictionary<System.Guid, string> captured = Tas.Query.SpaceZoneGuids([space]);
            Assert.That(captured.TryGetValue(space.Guid, out string zoneGuid) && zoneGuid == ZoneGuid_A, Is.True);

            //The clearing, as UpdateIds performs it.
            space.RemoveValue(SpaceParameter.ZoneGuid);
            Assert.That(space.TryGetValue(SpaceParameter.ZoneGuid, out string _), Is.False);

            //The captured identity is unaffected - and still resolves the zone even though the space's own
            //name (deliberately not a zone name here) cannot.
            Assert.That(captured[space.Guid], Is.EqualTo(ZoneGuid_A));
            string resolved = Tas.Query.ResolvedZone(captured[space.Guid], space.Name, ZonesByGuid, ZonesByName);
            Assert.That(resolved, Is.EqualTo("Zone A (renamed in TAS)"));

            //...whereas reading the stamp AFTER the clearing - the old ordering - finds nothing.
            Dictionary<System.Guid, string> capturedAfterClearing = Tas.Query.SpaceZoneGuids([space]);
            Assert.That(capturedAfterClearing, Is.Empty);
        }

        /// <summary>
        /// Unstamped and blank-stamped spaces contribute nothing to the captured map - the fallback path
        /// carries them instead.
        /// </summary>
        [Test]
        public void Capture_SkipsSpacesStatingNoIdentity()
        {
            Space space_Unstamped = new Space("Zone B");
            Space space_Blank = new Space("Zone A (renamed in TAS)");
            space_Blank.SetValue(SpaceParameter.ZoneGuid, "   ");

            Dictionary<System.Guid, string> captured = Tas.Query.SpaceZoneGuids([space_Unstamped, space_Blank, null]);

            Assert.That(captured, Is.Empty);
        }

        /// <summary>
        /// Optional parameters are supplied by the C# compiler and do not preserve a CLR signature. Keep the
        /// original zone-aware Match methods alongside the translation-aware overloads so an already-compiled
        /// plugin can replace only SAM.Analytical.Tas.dll without receiving MissingMethodException.
        /// </summary>
        [Test]
        public void MatchBinaryCompatibility_KeepsTheOriginalAndTranslationAwareOverloads()
        {
            Type queryType = typeof(Tas.Query);
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            MethodInfo panelOriginal = queryType.GetMethod("Match", flags, null,
                [typeof(TBD.IZoneSurface), typeof(List<Panel>), typeof(string), typeof(double)], null);
            MethodInfo panelTranslated = queryType.GetMethod("Match", flags, null,
                [typeof(TBD.IZoneSurface), typeof(List<Panel>), typeof(string), typeof(double), typeof(Vector3D)], null);

            MethodInfo apertureOriginal = queryType.GetMethod("Match", flags, null,
                [typeof(TBD.IZoneSurface), typeof(List<Aperture>), typeof(string), typeof(AperturePart).MakeByRefType(), typeof(double)], null);
            MethodInfo apertureTranslated = queryType.GetMethod("Match", flags, null,
                [typeof(TBD.IZoneSurface), typeof(List<Aperture>), typeof(string), typeof(AperturePart).MakeByRefType(), typeof(double), typeof(Vector3D)], null);

            Assert.Multiple(() =>
            {
                Assert.That(panelOriginal, Is.Not.Null);
                Assert.That(panelOriginal?.ReturnType, Is.EqualTo(typeof(Panel)));
                Assert.That(panelTranslated, Is.Not.Null);
                Assert.That(panelTranslated?.ReturnType, Is.EqualTo(typeof(Panel)));
                Assert.That(apertureOriginal, Is.Not.Null);
                Assert.That(apertureOriginal?.ReturnType, Is.EqualTo(typeof(Aperture)));
                Assert.That(apertureTranslated, Is.Not.Null);
                Assert.That(apertureTranslated?.ReturnType, Is.EqualTo(typeof(Aperture)));
            });
        }

        /// <summary>
        /// <c>AdjacencyCluster.Shade</c> classifies every panel unrelated to a space as a shade. The non-shade
        /// centroid subset in <c>UpdateIds</c> therefore already excludes unexported orphan panels.
        /// </summary>
        [Test]
        public void TranslationPanelSubset_ExcludesPanelsUnrelatedToSpacesAsShades()
        {
            Space space = new Space("Exported zone");
            Panel panel_Exported = PanelAt(0);
            Panel panel_Unassigned = PanelAt(1000);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddObject(panel_Exported);
            adjacencyCluster.AddObject(panel_Unassigned);
            adjacencyCluster.AddRelation(space, panel_Exported);

            List<Panel> panels = adjacencyCluster.GetPanels().FindAll(x => !adjacencyCluster.Shade(x));

            Assert.Multiple(() =>
            {
                Assert.That(adjacencyCluster.Shade(panel_Exported), Is.False);
                Assert.That(adjacencyCluster.Shade(panel_Unassigned), Is.True);
                Assert.That(panels, Has.Count.EqualTo(1));
                Assert.That(panels[0].Guid, Is.EqualTo(panel_Exported.Guid));
                Assert.That(panels.BoundingBox3D().GetCentroid().X, Is.EqualTo(5).Within(Core.Tolerance.Distance));
                Assert.That(adjacencyCluster.GetPanels().BoundingBox3D().GetCentroid().X, Is.EqualTo(505).Within(Core.Tolerance.Distance));
            });
        }

        /// <summary>
        /// A panel related to a SAM-only space is not a shade, but it still has no TBD counterpart. The
        /// translation subset must therefore be driven by resolved/shared spaces, not every non-shade panel.
        /// </summary>
        [Test]
        public void TranslationPanelSubset_ExcludesNonShadePanelsFromUnresolvedSpaces()
        {
            Space space_Shared = new Space("Shared zone");
            Space space_SAMOnly = new Space("Added after export");
            Panel panel_Shared = PanelAt(0);
            Panel panel_SAMOnly = PanelAt(1000);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(space_Shared);
            adjacencyCluster.AddObject(space_SAMOnly);
            adjacencyCluster.AddObject(panel_Shared);
            adjacencyCluster.AddObject(panel_SAMOnly);
            adjacencyCluster.AddRelation(space_Shared, panel_Shared);
            adjacencyCluster.AddRelation(space_SAMOnly, panel_SAMOnly);

            MethodInfo methodInfo = typeof(Tas.Modify).GetMethod("UpdateIdsTranslationPanels", BindingFlags.NonPublic | BindingFlags.Static);
            List<Panel> panels = methodInfo?.Invoke(null,
                [adjacencyCluster, new List<Space> { space_Shared }]) as List<Panel>;

            Assert.Multiple(() =>
            {
                Assert.That(methodInfo, Is.Not.Null);
                Assert.That(adjacencyCluster.Shade(panel_Shared), Is.False);
                Assert.That(adjacencyCluster.Shade(panel_SAMOnly), Is.False);
                Assert.That(panels, Has.Count.EqualTo(1));
                Assert.That(panels?[0].Guid, Is.EqualTo(panel_Shared.Guid));
                Assert.That(panels?.BoundingBox3D().GetCentroid().X, Is.EqualTo(5).Within(Core.Tolerance.Distance));
                Assert.That(adjacencyCluster.GetPanels().BoundingBox3D().GetCentroid().X, Is.EqualTo(505).Within(Core.Tolerance.Distance));
            });
        }

        private static Panel PanelAt(double x)
        {
            Polygon3D polygon3D = new Polygon3D(new List<Point3D>
            {
                new Point3D(x, 0, 0),
                new Point3D(x + 10, 0, 0),
                new Point3D(x + 10, 0, 10),
                new Point3D(x, 0, 10),
            });

            return Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, new Face3D(polygon3D));
        }
    }
}
