// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The cluster records the design days of the run it came back from - not every run's.</b>
    /// <para>
    /// The workflow used to APPEND each run's design days to the cluster. A model re-run in a Part O 2B
    /// optimisation therefore carried one more pair per run, and a model first simulated on London weather
    /// kept London design days beside its CIBSE Z1 ones for every later round. See
    /// <see cref="SAM.Analytical.Tas.Modify.ReplaceDesignDays"/>. No TAS COM.
    /// </para>
    /// </summary>
    [TestFixture]
    public class DesignDayRecordReplacementTests
    {
        private static List<DesignDay> One(string name) => [new DesignDay(name, 2018, 7, 1)];

        [Test]
        public void RepeatedRuns_KeepExactlyTheLastRunsDesignDays()
        {
            AdjacencyCluster adjacencyCluster = new();

            //An earlier run on another weather - the London records the live model carried.
            adjacencyCluster.ReplaceDesignDays(One("London ANN CLG"), One("London ANN HTG"));

            for (int i = 0; i < 10; i++)
            {
                adjacencyCluster.ReplaceDesignDays(One("Z1 ANN CLG"), One("Z1 ANN HTG"));
            }

            List<DesignDay> designDays = adjacencyCluster.GetObjects<DesignDay>();

            Assert.That(designDays.Select(x => x.Name).OrderBy(x => x), Is.EqualTo(new[] { "Z1 ANN CLG", "Z1 ANN HTG" }));
        }

        [Test]
        public void Replacement_ClearsRecordsLeftByEarlierAccumulation()
        {
            AdjacencyCluster adjacencyCluster = new();

            //A model saved before the fix: many copies, two weathers.
            for (int i = 0; i < 8; i++)
            {
                adjacencyCluster.AddObject(new DesignDay(new DesignDay("London ANN CLG", 2018, 7, 1), LoadType.Cooling));
                adjacencyCluster.AddObject(new DesignDay(new DesignDay("Z1 ANN HTG", 2018, 1, 1), LoadType.Heating));
            }

            int removed = adjacencyCluster.ReplaceDesignDays(One("Z1 ANN CLG"), One("Z1 ANN HTG"));

            Assert.That(removed, Is.EqualTo(16));
            Assert.That(adjacencyCluster.GetObjects<DesignDay>(), Has.Count.EqualTo(2));
            Assert.That(adjacencyCluster.GetObjects<DesignDay>().Any(x => x.Name.StartsWith("London")), Is.False);
        }

        [Test]
        public void OtherObjects_AreLeftAlone()
        {
            AdjacencyCluster adjacencyCluster = new();

            Analytical.Zone zone = new("Flat 1");
            Space space = new("Bedroom 1", new Geometry.Spatial.Point3D(0, 0, 0));
            adjacencyCluster.AddObject(zone);
            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddRelation(zone, space);

            adjacencyCluster.ReplaceDesignDays(One("Z1 ANN CLG"), null);
            adjacencyCluster.ReplaceDesignDays(One("Z1 ANN CLG"), null);

            Assert.That(adjacencyCluster.GetObjects<DesignDay>(), Has.Count.EqualTo(1));
            Assert.That(adjacencyCluster.GetSpaces(), Has.Count.EqualTo(1));
            Assert.That(adjacencyCluster.GetSpaces(adjacencyCluster.GetObjects<Analytical.Zone>()[0]), Has.Count.EqualTo(1));
        }
    }
}
