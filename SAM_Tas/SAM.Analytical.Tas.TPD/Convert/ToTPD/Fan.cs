using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.Fan ToTPD(this DisplaySystemFan displaySystemFan, global::TPD.System system, ElectricalGroup electricalGroup, PlantSchedule plantSchedule)
        {
            if(displaySystemFan == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddFan();
            result.name = displaySystemFan.Name;
            result.Description = displaySystemFan.Description;
            result.DesignFlowRate.Value = displaySystemFan.DesignFlowRate;
            result.OverallEfficiency.Value = displaySystemFan.OverallEfficiency;
            result.Pressure = displaySystemFan.Pressure;
            result.HeatGainFactor = displaySystemFan.HeatGainFactor;
            result.PartLoad.Value = 0;
            result.PartLoad.ClearModifiers();
            result.SetDirection(tpdDirection.tpdLeftRight);
            result.DesignFlowType = tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

            Point2D point2D = displaySystemFan.SystemGeometry?.Location?.ToTPD();
            if (point2D != null)
            {
                result.SetPosition(point2D.X, point2D.Y);
            }

            if (electricalGroup != null)
            {
                result.SetElectricalGroup1(electricalGroup);
            }

            if (plantSchedule != null)
            {
                result.SetSchedule(plantSchedule);
            }

            ProfileDataModifierTable profileDataModifierTable = result.PartLoad.AddModifierTable();
            profileDataModifierTable.Name = "Fan Part Load Curve";
            profileDataModifierTable.SetVariable(1, tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            profileDataModifierTable.Multiplier = tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            profileDataModifierTable.Clear();
            profileDataModifierTable.AddPoint(0, 0);
            profileDataModifierTable.AddPoint(10, 3);
            profileDataModifierTable.AddPoint(20, 7);
            profileDataModifierTable.AddPoint(30, 13);
            profileDataModifierTable.AddPoint(40, 21);
            profileDataModifierTable.AddPoint(50, 30);
            profileDataModifierTable.AddPoint(60, 41);
            profileDataModifierTable.AddPoint(70, 54);
            profileDataModifierTable.AddPoint(80, 68);
            profileDataModifierTable.AddPoint(90, 83);
            profileDataModifierTable.AddPoint(100, 100);

            return result as global::TPD.Fan;
        }
    }
}
