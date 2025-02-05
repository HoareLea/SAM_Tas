using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static LoadComponent ToTPD(this DisplaySystemLoadComponent displaySystemLoadComponent, global::TPD.System system)
        {
            if (displaySystemLoadComponent == null || system == null)
            {
                return null;
            }

            LoadComponent result = system.AddLoadComponent();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemLoadComponent.Name;
            @dynamic.Description = displaySystemLoadComponent.Description;

            CollectionLink collectionLink = displaySystemLoadComponent.GetValue<CollectionLink>(SystemLoadComponentParameter.Collection);
            if (collectionLink != null)
            {
                switch (collectionLink.CollectionType)
                {
                    case CollectionType.Heating:
                        HeatingGroup heatingGroup = system.GetPlantRoom()?.HeatingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (heatingGroup != null)
                        {
                            @dynamic.SetHeatingGroup(heatingGroup);
                        }
                        break;

                    case CollectionType.Cooling:
                        CoolingGroup coolingGroup = system.GetPlantRoom()?.CoolingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (coolingGroup != null)
                        {
                            @dynamic.SetCoolingGroup(coolingGroup);
                        }
                        break;

                    case CollectionType.DomesticHotWater:
                        DHWGroup dHWGroup = system.GetPlantRoom()?.DHWGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (dHWGroup != null)
                        {
                            @dynamic.SetDHWGroup(dHWGroup);
                        }
                        break;

                    case CollectionType.Refrigerant:
                        RefrigerantGroup refrigerantGroup = system.GetPlantRoom()?.RefrigerantGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (refrigerantGroup != null)
                        {
                            @dynamic.SetRefrigerantGroup(refrigerantGroup);
                        }
                        break;

                    case CollectionType.Fuel:
                        FuelGroup fuelGroup = system.GetPlantRoom()?.FuelGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (fuelGroup != null)
                        {
                            @dynamic.SetFuelGroup(fuelGroup);
                        }
                        break;

                    case CollectionType.Electrical:
                        ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (electricalGroup != null)
                        {
                            @dynamic.SetElectricalGroup1(electricalGroup);
                        }
                        break;
                }
            }

            result.Load?.Update(displaySystemLoadComponent.Load);

            displaySystemLoadComponent.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
