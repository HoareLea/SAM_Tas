using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.fluid> fluids(this global::TPD.EnergyCentre energyCentre)
        {
            if (energyCentre is null)
            {
                return null;
            }

            List<global::TPD.fluid> result = new List<global::TPD.fluid>();
            for (int i = 1; i <= energyCentre.GetFluidCount(); i++)
            {
                global::TPD.fluid fluid = energyCentre.GetFluid(i);
                if(fluid == null)
                {
                    continue;
                }

                result.Add(fluid);
            }

            return result;
        }
    }
}