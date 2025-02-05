using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static CollectionLink CollectionLink(this ISystemComponent systemComponent)
        {
            if (systemComponent == null)
            {
                return null;
            }

            dynamic @dynamic = (dynamic)systemComponent;

            HeatingGroup heatingGroup = @dynamic.GetHeatingGroup();
            CoolingGroup coolingGroup = @dynamic.GetCoolingGroup();
            DHWGroup dHWGroup = @dynamic.GetDHWGroup();
            RefrigerantGroup refrigerantGroup = @dynamic.GetRefrigerantGroup();
            SteamGroup steamGroup = @dynamic.GetSteamGroup();
            FuelGroup fuelGroup = @dynamic.GetFuelGroup();
            ElectricalGroup electricalGroup1 = @dynamic.GetElectricalGroup1();
            ElectricalGroup electricalGroup2 = @dynamic.GetElectricalGroup2();

            CollectionLink result = null;

            if(systemComponent is global::TPD.CoolingCoil)
            {
                if (coolingGroup == null)
                {
                    return null;
                }

                result = new CollectionLink(CollectionType.Cooling, ((dynamic)coolingGroup).Name);
            }
            else if(systemComponent is global::TPD.HeatingCoil)
            {
                if (heatingGroup != null)
                {
                    result = new CollectionLink(CollectionType.Heating, ((dynamic)heatingGroup).Name);
                }
                else if (fuelGroup != null)
                {
                    result = new CollectionLink(CollectionType.Fuel, ((dynamic)fuelGroup).Name);
                }
                else if (electricalGroup1 != null)
                {
                    result = new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup1).Name);
                }
                else if (electricalGroup2 != null)
                {
                    result = new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup2).Name);
                }
            }
            else if (systemComponent is LoadComponent)
            {
                if (heatingGroup != null)
                {
                    result = new CollectionLink(CollectionType.Heating, ((dynamic)heatingGroup).Name);
                }
                else if (coolingGroup != null)
                {
                    result = new CollectionLink(CollectionType.Cooling, ((dynamic)coolingGroup).Name);
                }
                else if (dHWGroup != null)
                {
                    result = new CollectionLink(CollectionType.DomesticHotWater, ((dynamic)dHWGroup).Name);
                }
                else if (refrigerantGroup != null)
                {
                    result = new CollectionLink(CollectionType.Refrigerant, ((dynamic)refrigerantGroup).Name);
                }
                else if (fuelGroup != null)
                {
                    result = new CollectionLink(CollectionType.Fuel, ((dynamic)fuelGroup).Name);
                }
                else if (electricalGroup1 != null)
                {
                    result = new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup1).Name);
                }
                else if (electricalGroup2 != null)
                {
                    result = new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup2).Name);
                }
            }
            else
            {
                if(electricalGroup1 == null)
                {
                    return null;
                }

                result = new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup1).Name);
            }

            return result;
        }
    }
}