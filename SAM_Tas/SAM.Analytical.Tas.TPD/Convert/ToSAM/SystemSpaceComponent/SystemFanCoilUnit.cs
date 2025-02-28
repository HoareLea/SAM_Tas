using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFanCoilUnit ToSAM(this FanCoilUnit fanCoilUnit)
        {
            if (fanCoilUnit == null)
            {
                return null;
            }

            dynamic @dynamic = fanCoilUnit;

            double designFlowRate = System.Convert.ToDouble((fanCoilUnit.DesignFlowRate as dynamic).Value);

            double heatingEfficiency = fanCoilUnit.HeatingEfficiency.Value;
            double overallEfficiency = fanCoilUnit.OverallEfficiency.Value;

            SystemFanCoilUnit result = new SystemFanCoilUnit(dynamic.Name)
            {
                Description = dynamic.Description,
                HeatingDuty = fanCoilUnit.HeatingDuty?.ToSAM(),
                CoolingDuty = fanCoilUnit.CoolingDuty?.ToSAM(),
                BypassFactor = fanCoilUnit.BypassFactor?.ToSAM(),
                HeatingEfficiency = fanCoilUnit.HeatingEfficiency?.ToSAM(),
                OverallEfficiency = fanCoilUnit.OverallEfficiency?.ToSAM(),
                HeatGainFactor = fanCoilUnit.HeatGainFactor,
                Pressure = fanCoilUnit.Pressure,
                DesignFlowRate = fanCoilUnit.DesignFlowRate?.ToSAM(),
                DesignFlowType = fanCoilUnit.DesignFlowType.ToSAM(),
                MinimumFlowRate = fanCoilUnit.MinimumFlowRate?.ToSAM(),
                MinimumFlowType = fanCoilUnit.MinimumFlowType.ToSAM(),
                ZonePosition = fanCoilUnit.ZonePosition.ToSAM(),
                ControlMethod = fanCoilUnit.ControlMethod.ToSAM(),
                PartLoad = fanCoilUnit.PartLoad?.ToSAM(),
                ScheduleName = @dynamic.GetSchedule()?.Name,
            };

            HeatingGroup heatingGroup = @dynamic.GetHeatingGroup();
            if (heatingGroup != null)
            {
                result.SetValue(SystemFanCoilUnitParameter.HeatingCollection, new CollectionLink(CollectionType.Heating, ((dynamic)heatingGroup).Name));
            }

            CoolingGroup coolingGroup = @dynamic.GetCoolingGroup();
            if (coolingGroup != null)
            {
                result.SetValue(SystemFanCoilUnitParameter.CoolingCollection, new CollectionLink(CollectionType.Cooling, ((dynamic)coolingGroup).Name));
            }

            ElectricalGroup electricalGroup = @dynamic.GetElectricalGroup1();
            if (electricalGroup != null)
            {
                result.SetValue(SystemFanCoilUnitParameter.ElectricalCollection, new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup).Name));
            }

            result.SetReference(((ZoneComponent)fanCoilUnit).Reference());

            return result;
        }
    }
}
