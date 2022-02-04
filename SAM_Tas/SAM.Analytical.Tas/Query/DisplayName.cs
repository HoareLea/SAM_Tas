using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string DisplayName(this VentilationSystem ventilationSystem)
        {
            if (ventilationSystem is null)
            {
                return null;
            }

            List<string> names = new List<string>();

            names.Add(ventilationSystem.FullName);

            if (ventilationSystem.TryGetValue(VentilationSystemParameter.SupplyUnitName, out string supplyUnitName))
            {
                names.Add(supplyUnitName);
            }

            if (ventilationSystem.TryGetValue(VentilationSystemParameter.ExhaustUnitName, out string exhaustUnitName))
            {
                names.Add(exhaustUnitName);
            }

            names.RemoveAll(x => string.IsNullOrEmpty(x));

            return string.Join("_", names);
        }
    }
}