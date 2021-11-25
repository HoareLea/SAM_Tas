using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void UpdateSizingFactors(this Building building, BuildingModel buildingModel)
        {
            if (building == null || buildingModel == null)
                return;

            List<Space> spaces = buildingModel.GetSpaces();
            if (spaces == null || spaces.Count == 0)
                return;

            double heatingSizingFactor = double.NaN;
            if (!buildingModel.TryGetValue(BuildingModelParameter.HeatingSizingFactor, out heatingSizingFactor))
                heatingSizingFactor = double.NaN;

            double coolingSizingFactor = double.NaN;
            if (!buildingModel.TryGetValue(BuildingModelParameter.CoolingSizingFactor, out coolingSizingFactor))
                coolingSizingFactor = double.NaN;

            UpdateSizingFactors(building, spaces, heatingSizingFactor, coolingSizingFactor);
        }
    }
}