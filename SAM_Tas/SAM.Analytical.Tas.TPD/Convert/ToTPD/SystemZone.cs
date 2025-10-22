using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemZone ToTPD(this DisplaySystemSpace displaySystemSpace, SystemPlantRoom systemPlantRoom, global::TPD.System system, SystemZone systemZone = null, bool addSystemSpaceComponents = true)
        {
            if(displaySystemSpace == null || system == null)
            {
                return null;
            }

            SystemZone result = systemZone;
            if(systemZone == null)
            {
                result = system.AddSystemZone();
            }

            EnergyCentre energyCentre = system.GetPlantRoom()?.GetEnergyCentre();

            dynamic @dynamic = result;

            result.TemperatureSetpoint.Update(displaySystemSpace.TemperatureSetpoint, energyCentre);
            result.RHSetpoint.Update(displaySystemSpace.RelativeHumiditySetpoint, energyCentre);
            result.PollutantSetpoint.Update(displaySystemSpace.PollutantSetpoint, energyCentre);
            result.FlowRate.Update(displaySystemSpace.FlowRate, energyCentre);
            result.FreshAir.Update(displaySystemSpace.FreshAir, energyCentre);

            result.MinimumFlowFraction = displaySystemSpace.MinimumDesignFlowFraction;

            if (displaySystemSpace.DisplacementVentilation)
            {
                result.DisplacementVent = displaySystemSpace.DisplacementVentilation.ToTPD();
                result.Flags = result.Flags | (int)tpdSystemZoneFlags.tpdSystemZoneFlagDisplacementVent;
            }

            if (displaySystemSpace.ModelInterzoneFlow)
            {
                result.Flags = result.Flags | (int)tpdSystemZoneFlags.tpdSystemZoneFlagModelInterzoneFlow;
            }

            if (displaySystemSpace.ModelVentilationFlow)
            {
                result.Flags = result.Flags | (int)tpdSystemZoneFlags.tpdSystemZoneFlagModelVentFlow;
            }

            CollectionLink collectionLink;

            collectionLink = displaySystemSpace.GetValue<CollectionLink>(SystemSpaceParameter.EquipmentElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    @dynamic.SetElectricalGroup1(electricalGroup);
                }
            }

            collectionLink = displaySystemSpace.GetValue<CollectionLink>(SystemSpaceParameter.LightingElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    @dynamic.SetElectricalGroup2(electricalGroup);
                }
            }

            collectionLink = displaySystemSpace.GetValue<CollectionLink>(SystemSpaceParameter.DomesticHotWaterCollection);
            if (collectionLink != null)
            {
                DHWGroup dHWGroup = system.GetPlantRoom()?.DHWGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (dHWGroup != null)
                {
                    @dynamic.SetDHWGroup(dHWGroup);
                }
            }


            if (energyCentre != null)
            {
                List<ZoneLoad> zoneLoads = Query.ZoneLoads(energyCentre.GetTSDData(1), new DisplaySystemSpace[] { displaySystemSpace });
                if (zoneLoads != null)
                {
                    foreach(ZoneLoad zoneLoad in zoneLoads)
                    {
                        @dynamic.AddZoneLoad(zoneLoad);
                    }
                }
            }

            if (addSystemSpaceComponents)
            {
                List<IZoneComponent> zoneComponents = Modify.AddSystemZoneComponents(result, displaySystemSpace, systemPlantRoom);
            }

            if (systemZone == null)
            {
                displaySystemSpace.SetLocation(result as global::TPD.SystemComponent);
            }

            return result;
        }
    }
}
