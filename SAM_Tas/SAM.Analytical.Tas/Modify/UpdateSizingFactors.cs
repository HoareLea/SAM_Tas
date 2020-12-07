using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void UpdateSizingFactors(this Building building, AnalyticalModel analyticalModel)
        {
            if (building == null || analyticalModel == null)
                return;

            List<Space> spaces = analyticalModel.AdjacencyCluster?.GetSpaces();
            if (spaces == null || spaces.Count == 0)
                return;

            double heatingSizingFactor = double.NaN;
            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.HeatingSizingFactor, out heatingSizingFactor))
                heatingSizingFactor = double.NaN;

            double coolingSizingFactor = double.NaN;
            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.CoolingSizingFactor, out coolingSizingFactor))
                coolingSizingFactor = double.NaN;

            UpdateSizingFactors(building, spaces, heatingSizingFactor, coolingSizingFactor);
        }

        public static void UpdateSizingFactors(this Building building, IEnumerable<Space> spaces, double heatingSizingFactor = double.NaN, double coolingSizingFactor = double.NaN)
        {
            if (building == null || spaces == null)
                return;

            List<zone> zones = building.Zones();
            if (zones == null || zones.Count == 0)
                return;

            foreach(Space space in spaces)
            {
                if (space == null || string.IsNullOrWhiteSpace(space.Name))
                    continue;

                double heatingSizingFactor_Zone = double.NaN;
                if (!space.TryGetValue(SpaceParameter.HeatingSizingFactor, out heatingSizingFactor_Zone))
                    heatingSizingFactor_Zone = double.NaN;

                if (double.IsNaN(heatingSizingFactor_Zone) && !double.IsNaN(heatingSizingFactor))
                    heatingSizingFactor_Zone = heatingSizingFactor;

                double coolingSizingFactor_Zone = double.NaN;
                if (!space.TryGetValue(SpaceParameter.CoolingSizingFactor, out coolingSizingFactor_Zone))
                    coolingSizingFactor_Zone = double.NaN;

                if (double.IsNaN(coolingSizingFactor_Zone) && !double.IsNaN(coolingSizingFactor))
                    coolingSizingFactor_Zone = coolingSizingFactor;

                if (double.IsNaN(coolingSizingFactor_Zone) && double.IsNaN(coolingSizingFactor_Zone))
                    continue;

                zone zone = zones.Zone(space.Name);
                if (zone == null)
                    continue;
                
                if(!double.IsNaN(coolingSizingFactor_Zone))
                {
                    zone.sizeCooling = (int)SizingType.tbdNoSizing;
                    zone.maxCoolingLoad = System.Convert.ToSingle(zone.maxCoolingLoad * coolingSizingFactor_Zone);
                }

                if (!double.IsNaN(heatingSizingFactor_Zone))
                {
                    zone.sizeHeating = (int)SizingType.tbdNoSizing;
                    zone.maxHeatingLoad = System.Convert.ToSingle(zone.maxHeatingLoad * heatingSizingFactor_Zone);
                }
            }
        }
    }
}