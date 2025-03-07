﻿using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemZone ToTPD(this DisplaySystemSpace displaySystemSpace, SystemPlantRoom systemPlantRoom, global::TPD.System system)
        {
            if(displaySystemSpace == null || system == null)
            {
                return null;
            }

            EnergyCentre energyCentre = system.GetPlantRoom()?.GetEnergyCentre();

            SystemZone result = system.AddSystemZone();

            dynamic @dynamic = result;

            result.TemperatureSetpoint.Update(displaySystemSpace.TemperatureSetpoint, energyCentre);
            result.RHSetpoint.Update(displaySystemSpace.RelativeHumiditySetpoint, energyCentre);
            result.PollutantSetpoint.Update(displaySystemSpace.PollutantSetpoint, energyCentre);
            result.DisplacementVent = displaySystemSpace.DisplacementVentilation ? 1 : 0;
            result.FlowRate.Update(displaySystemSpace.FlowRate, energyCentre);
            result.FreshAir.Update(displaySystemSpace.FreshAir, energyCentre);

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
                List<ZoneLoad> zoneLoads = Query.ZoneLoads(energyCentre.GetTSDData(1));
                if (zoneLoads != null)
                {
                    foreach(ZoneLoad zoneLoad in zoneLoads)
                    {
                        @dynamic.AddZoneLoad(zoneLoad);
                    }
                }

                if(systemPlantRoom != null)
                {
                    List<ISystemSpaceComponent> systemSpaceComponents = systemPlantRoom.GetSystemSpaceComponents<ISystemSpaceComponent>(displaySystemSpace);
                    if (systemSpaceComponents != null)
                    {
                        foreach (ISystemSpaceComponent systemSpaceComponent in systemSpaceComponents)
                        {
                            if (systemSpaceComponent is SystemDXCoilUnit)
                            {
                                DXCoilUnit dXCoilUnit = ((SystemDXCoilUnit)systemSpaceComponent).ToTPD(result);
                            }
                            else if (systemSpaceComponent is SystemFanCoilUnit)
                            {
                                FanCoilUnit fanCoilUnit = ((SystemFanCoilUnit)systemSpaceComponent).ToTPD(result);
                            }
                            else if (systemSpaceComponent is SystemChilledBeam)
                            {
                                ChilledBeam chilledBeam = ((SystemChilledBeam)systemSpaceComponent).ToTPD(result);
                            }
                            else if (systemSpaceComponent is SystemRadiator)
                            {
                                Radiator radiator = ((SystemRadiator)systemSpaceComponent).ToTPD(result);
                            }
                        }
                    }
                }
            }

            displaySystemSpace.SetLocation(result as global::TPD.SystemComponent);

            return result;
        }
    }
}
