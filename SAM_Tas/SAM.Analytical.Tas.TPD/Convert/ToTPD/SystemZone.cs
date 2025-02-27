using SAM.Analytical.Systems;
using SAM.Core;
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

            SystemZone result = system.AddSystemZone();

            dynamic @dynamic = result;

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

            EnergyCentre energyCentre = system.GetPlantRoom()?.GetEnergyCentre();

            if (energyCentre != null && systemPlantRoom != null)
            {
                List<ISystemSpaceComponent> systemSpaceComponents = systemPlantRoom.GetSystemSpaceComponents<ISystemSpaceComponent>(displaySystemSpace);
                if(systemSpaceComponents != null)
                {
                    foreach(ISystemSpaceComponent systemSpaceComponent in systemSpaceComponents)
                    {
                        if(systemSpaceComponent is SystemDXCoilUnit)
                        {
                            SystemDXCoilUnit systemDXCoilUnit = (SystemDXCoilUnit)systemSpaceComponent;

                            DXCoilUnit dXCoilUnit = result.AddDXCoilUnit();
                            ((dynamic)dXCoilUnit).Name = systemDXCoilUnit.Name;
                            ((dynamic)dXCoilUnit).Description = systemDXCoilUnit.Description;
                            dXCoilUnit.HeatingDuty?.Update(systemDXCoilUnit.HeatingDuty, energyCentre);
                            dXCoilUnit.CoolingDuty?.Update(systemDXCoilUnit.CoolingDuty, energyCentre);
                            dXCoilUnit.BypassFactor?.Update(systemDXCoilUnit.BypassFactor, energyCentre);
                            dXCoilUnit.OverallEfficiency?.Update(systemDXCoilUnit.OverallEfficiency, energyCentre);
                            dXCoilUnit.HeatGainFactor = systemDXCoilUnit.HeatGainFactor;
                            dXCoilUnit.Pressure = systemDXCoilUnit.Pressure;
                            dXCoilUnit.DesignFlowRate?.Update(systemDXCoilUnit.DesignFlowRate);
                            dXCoilUnit.DesignFlowType = systemDXCoilUnit.DesignFlowType.ToTPD();
                            dXCoilUnit.MinimumFlowRate?.Update(systemDXCoilUnit.MinimumFlowRate);
                            dXCoilUnit.MinimumFlowType = systemDXCoilUnit.MinimumFlowType.ToTPD();
                            dXCoilUnit.ZonePosition = systemDXCoilUnit.ZonePosition.ToTPD();
                            dXCoilUnit.ControlMethod = systemDXCoilUnit.ControlMethod.ToTPD();
                            dXCoilUnit.PartLoad?.Update(systemDXCoilUnit.PartLoad, energyCentre);
                        }
                        else if(systemSpaceComponent is SystemFanCoilUnit)
                        {
                            SystemFanCoilUnit systemFanCoilUnit = (SystemFanCoilUnit)systemSpaceComponent;

                            FanCoilUnit fanCoilUnit = result.AddFanCoilUnit();
                            ((dynamic)fanCoilUnit).Name = systemFanCoilUnit.Name;
                            ((dynamic)fanCoilUnit).Description = systemFanCoilUnit.Description;
                            fanCoilUnit.HeatingDuty?.Update(systemFanCoilUnit.HeatingDuty, energyCentre);
                            fanCoilUnit.CoolingDuty?.Update(systemFanCoilUnit.CoolingDuty, energyCentre);
                            fanCoilUnit.BypassFactor?.Update(systemFanCoilUnit.BypassFactor, energyCentre);
                            fanCoilUnit.OverallEfficiency?.Update(systemFanCoilUnit.OverallEfficiency, energyCentre);
                            fanCoilUnit.HeatGainFactor = systemFanCoilUnit.HeatGainFactor;
                            fanCoilUnit.Pressure = systemFanCoilUnit.Pressure;
                            fanCoilUnit.DesignFlowRate?.Update(systemFanCoilUnit.DesignFlowRate);
                            fanCoilUnit.DesignFlowType = systemFanCoilUnit.DesignFlowType.ToTPD();
                            fanCoilUnit.MinimumFlowRate?.Update(systemFanCoilUnit.MinimumFlowRate);
                            fanCoilUnit.MinimumFlowType = systemFanCoilUnit.MinimumFlowType.ToTPD();
                            fanCoilUnit.ZonePosition = systemFanCoilUnit.ZonePosition.ToTPD();
                            fanCoilUnit.ControlMethod = systemFanCoilUnit.ControlMethod.ToTPD();
                            fanCoilUnit.PartLoad?.Update(systemFanCoilUnit.PartLoad, energyCentre);
                        }
                        else if (systemSpaceComponent is SystemChilledBeam)
                        {
                            SystemChilledBeam systemChilledBeam = (SystemChilledBeam)systemSpaceComponent;

                            ChilledBeam chilledBeam = result.AddChilledBeam();
                            ((dynamic)chilledBeam).Name = systemChilledBeam.Name;
                            ((dynamic)chilledBeam).Description = systemChilledBeam.Description;
                            chilledBeam.HeatingDuty?.Update(systemChilledBeam.HeatingDuty, energyCentre);
                            chilledBeam.CoolingDuty?.Update(systemChilledBeam.CoolingDuty, energyCentre);
                            chilledBeam.BypassFactor?.Update(systemChilledBeam.BypassFactor, energyCentre);
                            chilledBeam.HeatingEfficiency?.Update(systemChilledBeam.HeatingEfficiency, energyCentre);
                            chilledBeam.DesignFlowRate?.Update(systemChilledBeam.DesignFlowRate);
                            chilledBeam.DesignFlowType = systemChilledBeam.DesignFlowType.ToTPD();
                            chilledBeam.ZonePosition = systemChilledBeam.ZonePosition.ToTPD();
                        }
                        else if (systemSpaceComponent is SystemRadiator)
                        {
                            SystemRadiator systemRadiator = (SystemRadiator)systemSpaceComponent;

                            Radiator radiator = result.AddRadiator();
                            ((dynamic)radiator).Name = systemRadiator.Name;
                            ((dynamic)radiator).Description = systemRadiator.Description;

                            radiator.Duty?.Update(systemRadiator.Duty, energyCentre);
                            radiator.Efficiency?.Update(systemRadiator.Efficiency, energyCentre);
                        }
                    }
                }
            }

            displaySystemSpace.SetLocation(result as global::TPD.SystemComponent);

            return result;
        }
    }
}
