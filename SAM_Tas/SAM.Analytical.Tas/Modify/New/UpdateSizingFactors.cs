using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void UpdateSizingFactors(this Building building, ArchitecturalModel architecturalModel)
        {
            if (building == null || architecturalModel == null)
                return;

            List<Space> spaces = architecturalModel.GetSpaces();
            if (spaces == null || spaces.Count == 0)
                return;

            double heatingSizingFactor = double.NaN;
            if (!architecturalModel.TryGetValue(ArchitecturalModelParameter.HeatingSizingFactor, out heatingSizingFactor))
                heatingSizingFactor = double.NaN;

            double coolingSizingFactor = double.NaN;
            if (!architecturalModel.TryGetValue(ArchitecturalModelParameter.CoolingSizingFactor, out coolingSizingFactor))
                coolingSizingFactor = double.NaN;

            UpdateSizingFactors(building, spaces, heatingSizingFactor, coolingSizingFactor);
        }
    }
}