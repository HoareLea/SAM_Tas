using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemZone ToTPD(this DisplaySystemSpace displaySystemSpace, global::TPD.System system)
        {
            if(displaySystemSpace == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddSystemZone();

            CollectionLink collectionLink;

            collectionLink = displaySystemSpace.GetValue<CollectionLink>(SystemSpaceParameter.EquipmentElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    result.SetElectricalGroup1(electricalGroup);
                }
            }

            collectionLink = displaySystemSpace.GetValue<CollectionLink>(SystemSpaceParameter.LightingElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    result.SetElectricalGroup2(electricalGroup);
                }
            }

            collectionLink = displaySystemSpace.GetValue<CollectionLink>(SystemSpaceParameter.DomesticHotWaterCollection);
            if (collectionLink != null)
            {
                DHWGroup dHWGroup = system.GetPlantRoom()?.DHWGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (dHWGroup != null)
                {
                    result.SetDHWGroup(dHWGroup);
                }
            }

            displaySystemSpace.SetLocation(result as SystemComponent);

            return result as SystemZone;
        }
    }
}
