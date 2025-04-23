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
            if (!analyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.HeatingSizingFactor, out heatingSizingFactor))
                heatingSizingFactor = double.NaN;

            double coolingSizingFactor = double.NaN;
            if (!analyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.CoolingSizingFactor, out coolingSizingFactor))
                coolingSizingFactor = double.NaN;

            UpdateSizingFactors(building, spaces, heatingSizingFactor, coolingSizingFactor);
        }

        public static void UpdateSizingFactors(this Building building, IEnumerable<Space> spaces, double heatingSizingFactor = double.NaN, double coolingSizingFactor = double.NaN)
        {
            if (building == null || spaces == null)
                return;

            Dictionary<string, zone> zones = building.ZoneDictionary();
            if (zones == null || zones.Count == 0)
                return;

            foreach(Space space in spaces)
            {
                string name = space?.Name;

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                zone zone;
                if (!zones.TryGetValue(name, out zone))
                    continue;

                if (zone == null)
                    continue;

                double heatingSizingFactor_Zone = double.NaN;
                if (!space.TryGetValue(Analytical.SpaceParameter.HeatingSizingFactor, out heatingSizingFactor_Zone))
                    heatingSizingFactor_Zone = double.NaN;

                if ((double.IsNaN(heatingSizingFactor_Zone) || heatingSizingFactor_Zone == 0) && !double.IsNaN(heatingSizingFactor) && heatingSizingFactor != 0)
                    heatingSizingFactor_Zone = heatingSizingFactor;

                if (heatingSizingFactor_Zone == 0)
                    heatingSizingFactor_Zone = double.NaN;

                double coolingSizingFactor_Zone = double.NaN;
                if (!space.TryGetValue(Analytical.SpaceParameter.CoolingSizingFactor, out coolingSizingFactor_Zone))
                    coolingSizingFactor_Zone = double.NaN;

                if ((double.IsNaN(coolingSizingFactor_Zone) || coolingSizingFactor_Zone == 0) && !double.IsNaN(coolingSizingFactor) && coolingSizingFactor != 0)
                    coolingSizingFactor_Zone = coolingSizingFactor;

                if (coolingSizingFactor_Zone == 0)
                    coolingSizingFactor_Zone = double.NaN;

                if (double.IsNaN(coolingSizingFactor_Zone) && double.IsNaN(heatingSizingFactor_Zone))
                    continue;
                
                if(!double.IsNaN(coolingSizingFactor_Zone))
                {
                    zone.sizeCooling = (int)TBD.SizingType.tbdNoSizing;
                    zone.maxCoolingLoad = System.Convert.ToSingle(zone.maxCoolingLoad * coolingSizingFactor_Zone);
                }

                if (!double.IsNaN(heatingSizingFactor_Zone))
                {
                    zone.sizeHeating = (int)TBD.SizingType.tbdNoSizing;
                    zone.maxHeatingLoad = System.Convert.ToSingle(zone.maxHeatingLoad * heatingSizingFactor_Zone);
                }
            }
        }
    }
}