using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FluidType ToSAM(this fluid fluid)
        {
            if (fluid == null)
            {
                return null;
            }

            FluidType result = new FluidType(fluid.Name);
            result.Density = fluid.Density;
            result.Description = fluid.Description;
            result.FreezingPoint = fluid.FreezingPoint;
            result.SpecificHeatCapacity = fluid.SpecificHeatCapacity;

            return result;
        }
    }
}