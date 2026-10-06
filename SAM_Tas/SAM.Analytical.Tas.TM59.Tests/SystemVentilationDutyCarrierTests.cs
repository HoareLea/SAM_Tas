// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Step 7, the flow carriers. Every extract and transfer leg gets its own <c>SystemDamper</c> in a
    /// <b>working copy</b> of the PR1 graph, carrying that leg's design airflow absolutely.
    /// <para>
    /// <b>Why a damper has to be inserted at all.</b> Measured on licensed TAS: a <c>Duct</c> declares
    /// no GUID and no design-flow property, and a <c>Junction</c> declares no members. PR1 runs its
    /// extract legs straight into the unit's junction and its transfer legs room to room, so until one
    /// is put there, neither leg has anywhere to hold a duty. A supply leg needs none: its duty belongs
    /// on the room's own zone, which is where TAS itself puts it.
    /// </para>
    /// <para>All of this runs on the real PR1 graph, and none of it touches TAS.</para>
    /// </summary>
    [TestFixture]
    public class SystemVentilationDutyCarrierTests
    {
        private static Core.Systems.SystemEnergyCentre Prepare(
            AdjacencyCluster adjacencyCluster,
            out SystemVentilationConversionContext systemVentilationConversionContext,
            out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation)
        {
            mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            Assert.That(mechanicalVentilationMaterialisation.IsMaterialised, Is.True, string.Join(" | ", mechanicalVentilationMaterialisation.Refusals));

            systemVentilationConversionContext = TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster));

            Core.Systems.SystemEnergyCentre result = new Core.Systems.SystemEnergyCentre(mechanicalVentilationMaterialisation.SystemEnergyCentre);

            TPD.Modify.MaterialiseVentilationDutyCarriers(result, systemVentilationConversionContext);

            return result;
        }

        private static SystemPlantRoom PlantRoom(Core.Systems.SystemEnergyCentre systemEnergyCentre)
        {
            return systemEnergyCentre.GetSystemPlantRooms()[0];
        }

        private static SystemDamper Damper(Core.Systems.SystemEnergyCentre systemEnergyCentre, Guid guid)
        {
            foreach (SystemDamper systemDamper in PlantRoom(systemEnergyCentre).GetSystemComponents<SystemDamper>() ?? new List<SystemDamper>())
            {
                if (systemDamper.Guid == guid)
                {
                    return systemDamper;
                }
            }

            return null;
        }

        [Test]
        public void OneDamperPerExtractAndTransferLeg_AndNoneForSupply()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            Core.Systems.SystemEnergyCentre systemEnergyCentre = Prepare(adjacencyCluster, out SystemVentilationConversionContext systemVentilationConversionContext, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            List<SystemVentilationLegIntent> legs = systemVentilationConversionContext.LegIntents;

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.Refusals, Is.Empty, string.Join(" | ", systemVentilationConversionContext.Refusals));

                foreach (SystemVentilationLegIntent systemVentilationLegIntent in legs)
                {
                    if (systemVentilationLegIntent.ConnectionType == SystemVentilationConnectionType.Supply)
                    {
                        Assert.That(systemVentilationLegIntent.Guid_DutyCarrier, Is.EqualTo(Guid.Empty), "a supply leg's duty rides on the room's zone");
                        continue;
                    }

                    Assert.That(systemVentilationLegIntent.Guid_DutyCarrier, Is.Not.EqualTo(Guid.Empty), systemVentilationLegIntent.ToString());
                    Assert.That(Damper(systemEnergyCentre, systemVentilationLegIntent.Guid_DutyCarrier), Is.Not.Null, "the carrier must exist in the working copy");
                }
            });

            //Two extract and four transfer legs, so six carriers - and no two legs share one.
            List<Guid> carriers = legs.FindAll(x => x.RequiresDutyCarrier).ConvertAll(x => x.Guid_DutyCarrier);

            Assert.Multiple(() =>
            {
                Assert.That(carriers.Count, Is.EqualTo(6));
                Assert.That(carriers.Distinct().Count(), Is.EqualTo(6), "one duty cannot stand for two legs");
            });
        }

        [Test]
        public void EachCarrierHoldsItsOwnLegsDesignAirflowAbsolutely()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            Core.Systems.SystemEnergyCentre systemEnergyCentre = Prepare(adjacencyCluster, out SystemVentilationConversionContext systemVentilationConversionContext, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                if (!systemVentilationLegIntent.RequiresDutyCarrier)
                {
                    continue;
                }

                SystemDamper systemDamper = Damper(systemEnergyCentre, systemVentilationLegIntent.Guid_DutyCarrier);

                Assert.Multiple(() =>
                {
                    Assert.That(systemDamper.DesignFlowRate.Value, Is.EqualTo(systemVentilationLegIntent.DesignFlowRate_Lps).Within(1e-9));

                    //FlowRateType.Value, not a derivation rule: NearestZoneFlowRate would let TAS take a
                    //neighbouring zone's number instead of the design's.
                    Assert.That(systemDamper.DesignFlowType, Is.EqualTo(FlowRateType.Value));

                    //And SizingType.Value, so Modify.Update writes the TYPE as well as the value - a
                    //plain SizedFlowValue would leave the template's ACH sizing rule in place and the
                    //litres per second would be discarded.
                    Assert.That(systemDamper.DesignFlowRate, Is.InstanceOf<DesignConditionSizedFlowValue>());
                    Assert.That(((DesignConditionSizedFlowValue)systemDamper.DesignFlowRate).SizingType, Is.EqualTo(Systems.SizingType.Value));

                    //A minimum inherited from the template's own leg would silently floor this one.
                    Assert.That(systemDamper.MinimumFlowType, Is.EqualTo(FlowRateType.None));
                    Assert.That(systemDamper.MinimumFlowRate.Value, Is.EqualTo(0.0));
                });
            }
        }

        [Test]
        public void TheCarrierReplacesTheDirectLeg_SoNothingIsConnectedTwice()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            Core.Systems.SystemEnergyCentre systemEnergyCentre = Prepare(adjacencyCluster, out SystemVentilationConversionContext systemVentilationConversionContext, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            HashSet<Guid> guids_Connection = new HashSet<Guid>();

            foreach (ISystemConnection systemConnection in PlantRoom(systemEnergyCentre).GetSystemConnections() ?? new List<ISystemConnection>())
            {
                guids_Connection.Add(systemConnection.Guid);
            }

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                if (!systemVentilationLegIntent.RequiresDutyCarrier)
                {
                    Assert.That(guids_Connection.Contains(systemVentilationLegIntent.Guid_SystemConnection), Is.True, "a supply leg is left exactly as PR1 built it");
                    continue;
                }

                Assert.That(
                    guids_Connection.Contains(systemVentilationLegIntent.Guid_SystemConnection),
                    Is.False,
                    "the direct leg must be gone, or the conversion would build both it and the leg through the damper");
            }
        }

        [Test]
        public void PR1sOwnGraphIsNotTouched()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            string before = mechanicalVentilationMaterialisation.SystemEnergyCentre.ToJsonObject().ToJsonString();

            SystemVentilationConversionContext systemVentilationConversionContext = TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster));

            Core.Systems.SystemEnergyCentre systemEnergyCentre_Working = new Core.Systems.SystemEnergyCentre(mechanicalVentilationMaterialisation.SystemEnergyCentre);

            TPD.Modify.MaterialiseVentilationDutyCarriers(systemEnergyCentre_Working, systemVentilationConversionContext);

            string after = mechanicalVentilationMaterialisation.SystemEnergyCentre.ToJsonObject().ToJsonString();

            Assert.Multiple(() =>
            {
                Assert.That(after, Is.EqualTo(before), "the design graph is an input and must come back byte-identical");
                Assert.That(
                    systemEnergyCentre_Working.ToJsonObject().ToJsonString(),
                    Is.Not.EqualTo(before),
                    "and the working copy must actually have gained the carriers");
            });
        }

        [Test]
        public void TheCarrierIdentitiesAreDerived_SoTwoRunsAgreeExactly()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            Dictionary<Guid, string> zoneReferences = SystemVentilationFixture.ZoneReferences(adjacencyCluster);

            List<string> Run()
            {
                SystemVentilationConversionContext systemVentilationConversionContext = TPD.Create.SystemVentilationConversionContext(
                    mechanicalVentilationMaterialisation.SystemEnergyCentre,
                    mechanicalVentilationMaterialisation.Bindings,
                    zoneReferences);

                Core.Systems.SystemEnergyCentre systemEnergyCentre = new Core.Systems.SystemEnergyCentre(mechanicalVentilationMaterialisation.SystemEnergyCentre);

                TPD.Modify.MaterialiseVentilationDutyCarriers(systemEnergyCentre, systemVentilationConversionContext);

                return systemVentilationConversionContext.LegIntents.ConvertAll(x => x.ToString());
            }

            Assert.That(Run(), Is.EqualTo(Run()));
        }

        [Test]
        public void BranchingTransfer_GivesEachLegItsOwnCarrierAndItsOwnDuty()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            Core.Systems.SystemEnergyCentre systemEnergyCentre = Prepare(adjacencyCluster, out SystemVentilationConversionContext systemVentilationConversionContext, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Guid guid_Hall = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, SystemVentilationFixture.Space(adjacencyCluster, "Hall", true));

            List<SystemVentilationLegIntent> legs = systemVentilationConversionContext.LegIntents
                .FindAll(x => x.ConnectionType == SystemVentilationConnectionType.Transfer && x.Guid_SystemSpace_From == guid_Hall);

            Assert.That(legs.Count, Is.EqualTo(2), "the hall branches to two wet rooms");

            List<double> duties = legs.ConvertAll(x => Damper(systemEnergyCentre, x.Guid_DutyCarrier).DesignFlowRate.Value);
            duties.Sort();

            Assert.Multiple(() =>
            {
                Assert.That(legs[0].Guid_DutyCarrier, Is.Not.EqualTo(legs[1].Guid_DutyCarrier));
                Assert.That(duties[0], Is.EqualTo(5.0).Within(1e-9));
                Assert.That(duties[1], Is.EqualTo(7.5).Within(1e-9));
            });
        }

        [Test]
        public void DuplicateDisplayNamesDoNotChangeWhichLegOwnsWhichCarrier()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.DuplicateNames(out AirHandlingUnit airHandlingUnit);

            Core.Systems.SystemEnergyCentre systemEnergyCentre = Prepare(adjacencyCluster, out SystemVentilationConversionContext systemVentilationConversionContext, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            List<SystemVentilationLegIntent> legs = systemVentilationConversionContext.LegIntents.FindAll(x => x.RequiresDutyCarrier);

            //One extract at 13 l/s and one transfer at 4 l/s, in a model where every room is called
            //"Bedroom 2" - so the dampers themselves end up with colliding names too.
            Assert.That(legs.Count, Is.EqualTo(2));

            Dictionary<SystemVentilationConnectionType, double> duties = new Dictionary<SystemVentilationConnectionType, double>();

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in legs)
            {
                duties[systemVentilationLegIntent.ConnectionType] = Damper(systemEnergyCentre, systemVentilationLegIntent.Guid_DutyCarrier).DesignFlowRate.Value;
            }

            Assert.Multiple(() =>
            {
                Assert.That(duties[SystemVentilationConnectionType.Extract], Is.EqualTo(13.0).Within(1e-9));
                Assert.That(duties[SystemVentilationConnectionType.Transfer], Is.EqualTo(4.0).Within(1e-9));

                Assert.That(
                    legs.ConvertAll(x => Damper(systemEnergyCentre, x.Guid_DutyCarrier).Name).Distinct().Count(),
                    Is.LessThanOrEqualTo(2),
                    "the names may collide - what must not collide is the identities");

                Assert.That(legs[0].Guid_DutyCarrier, Is.Not.EqualTo(legs[1].Guid_DutyCarrier));
            });
        }
    }
}
