using TPD;
using System.Linq;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSpace ToSAM(this SystemZone systemZone)
        {
            if (systemZone == null)
            {
                return null;
            }

            string name = null;
            double area = double.NaN;
            double volume = double.NaN;

            ZoneLoad zoneLoad = systemZone.ZoneLoads()?.FirstOrDefault();
            if (zoneLoad != null)
            {
                name = zoneLoad.Name;
                area = zoneLoad.FloorArea;
                volume = zoneLoad.Volume;
            }

            dynamic @dynamic = systemZone as dynamic;

            Core.ModifiableValue temperatureSetpoint = systemZone.TemperatureSetpoint?.ToSAM();
            Core.ModifiableValue relativeHumiditySetpoint = systemZone.RHSetpoint?.ToSAM();
            Core.ModifiableValue pollutantSetpoint = systemZone.PollutantSetpoint?.ToSAM();
            bool displacementVentilation = systemZone.DisplacementVent == 1;
            DesignConditionSizedFlowValue flowRate = systemZone.FlowRate.ToSAM();
            DesignConditionSizedFlowValue freshAir = systemZone.FreshAir.ToSAM();

            SystemSpace result = new SystemSpace(name, area, volume, temperatureSetpoint, relativeHumiditySetpoint, pollutantSetpoint, displacementVentilation, flowRate, freshAir);
            Modify.SetReference(result, dynamic.GUID);

            result.Description = dynamic.Description;

            ElectricalGroup electricalGroup1 = @dynamic.GetElectricalGroup1();
            if(electricalGroup1 != null)
            {
                result.SetValue(SystemSpaceParameter.EquipmentElectricalCollection, new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup1).Name));
            }

            ElectricalGroup electricalGroup2 = @dynamic.GetElectricalGroup2();
            if (electricalGroup2 != null)
            {
                result.SetValue(SystemSpaceParameter.LightingElectricalCollection, new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup2).Name));
            }

            DHWGroup dHWGroup = @dynamic.GetDHWGroup();
            if (dHWGroup != null)
            {
                result.SetValue(SystemSpaceParameter.DomesticHotWaterCollection, new CollectionLink(CollectionType.DomesticHotWater, ((dynamic)dHWGroup).Name));
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSpace displaySystemSpace = Systems.Create.DisplayObject<DisplaySystemSpace>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemSpace != null)
            {
                ITransform2D transform2D = ((ISystemComponent)systemZone).Transform2D();
                if (transform2D != null)
                {
                    displaySystemSpace.Transform(transform2D);
                }

                result = displaySystemSpace;
            }

            return result;
        }
    }
}
