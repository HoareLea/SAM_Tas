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

            displaySystemSpace.SetLocation(result as global::TPD.SystemComponent);

            return result;
        }
    }
}
