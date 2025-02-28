using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDXCoilUnit ToSAM(this DXCoilUnit dXCoilUnit)
        {
            if (dXCoilUnit == null)
            {
                return null;
            }

            dynamic @dynamic = dXCoilUnit as dynamic;

            double designFlowRate = System.Convert.ToDouble((dXCoilUnit.DesignFlowRate as dynamic).Value);

            double overallEfficiency = dXCoilUnit.OverallEfficiency.Value;

            SystemDXCoilUnit result = new SystemDXCoilUnit(dynamic.Name)
            {
                Description = dynamic.Description,
                HeatingDuty = dXCoilUnit.HeatingDuty?.ToSAM(),
                CoolingDuty = dXCoilUnit.CoolingDuty?.ToSAM(),
                BypassFactor = dXCoilUnit.BypassFactor?.ToSAM(),
                OverallEfficiency = dXCoilUnit.OverallEfficiency?.ToSAM(),
                HeatGainFactor = dXCoilUnit.HeatGainFactor,
                Pressure = dXCoilUnit.Pressure,
                DesignFlowRate = dXCoilUnit.DesignFlowRate?.ToSAM(),
                DesignFlowType = dXCoilUnit.DesignFlowType.ToSAM(),
                MinimumFlowRate = dXCoilUnit.MinimumFlowRate?.ToSAM(),
                MinimumFlowType = dXCoilUnit.MinimumFlowType.ToSAM(),
                ZonePosition = dXCoilUnit.ZonePosition.ToSAM(),
                ControlMethod = dXCoilUnit.ControlMethod.ToSAM(),
                PartLoad = dXCoilUnit.PartLoad?.ToSAM(),
                ScheduleName = @dynamic.GetSchedule()?.Name,
            };

            ElectricalGroup electricalGroup = @dynamic.GetElectricalGroup1();
            if (electricalGroup != null)
            {
                result.SetValue(SystemDXCoilUnitParameter.ElectricalCollection, new CollectionLink(CollectionType.Electrical, ((dynamic)electricalGroup).Name));
            }

            RefrigerantGroup refrigerantGroup = @dynamic.GetRefrigerantGroup();
            if (refrigerantGroup != null)
            {
                result.SetValue(SystemDXCoilUnitParameter.RefrigerantCollection, new CollectionLink(CollectionType.Refrigerant, ((dynamic)refrigerantGroup).Name));
            }

            result.SetReference(((ZoneComponent)dXCoilUnit).Reference());

            return result;
        }
    }
}
