using SAM.Analytical.Systems;
using SAM.Core.Tas.TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ResultDataType ToSAM(this global::TPD.tpdResultVectorType tpdResultVectorType)
        {
            switch (tpdResultVectorType)
            {
                case global::TPD.tpdResultVectorType.tpdLoad:
                    return ResultDataType.Load;

                case global::TPD.tpdResultVectorType.tpdElectricLoad:
                    return ResultDataType.ElectricLoad;

                case global::TPD.tpdResultVectorType.tpdThermalLoad:
                    return ResultDataType.ThermalLoad;

                case global::TPD.tpdResultVectorType.tpdDemand:
                    return ResultDataType.Demand;

                case global::TPD.tpdResultVectorType.tpdElectricDemand:
                    return ResultDataType.ElectricDemand ;

                case global::TPD.tpdResultVectorType.tpdThermalDemand:
                    return ResultDataType.ThermalDemand;

                case global::TPD.tpdResultVectorType.tpdConsumption:
                    return ResultDataType.Consumption;

                case global::TPD.tpdResultVectorType.tpdElectricConsumption:
                    return ResultDataType.ElectricConsumption;

                case global::TPD.tpdResultVectorType.tpdThermalConsumption:
                    return ResultDataType.ThermalConsumption;

                case global::TPD.tpdResultVectorType.tpdGenerated:
                    return ResultDataType.Generated;

                case global::TPD.tpdResultVectorType.tpdCo2:
                    return ResultDataType.Co2;

                case global::TPD.tpdResultVectorType.tpdCost:
                    return ResultDataType.Cost;

                case global::TPD.tpdResultVectorType.tpdFuelType:
                    return ResultDataType.FuelType;

                case global::TPD.tpdResultVectorType.tpdElectricFuelType:
                    return ResultDataType.ElectricFuelType;

                case global::TPD.tpdResultVectorType.tpdThermalFuelType:
                    return ResultDataType.ThermalFuelType;

                case global::TPD.tpdResultVectorType.tpdPlantComp:
                    return ResultDataType.PlantComponent;

                case global::TPD.tpdResultVectorType.tpdElectricPlantComp:
                    return ResultDataType.ElectricPlantComponent;

                case global::TPD.tpdResultVectorType.tpdThermalPlantComp:
                    return ResultDataType.ThermalPlantComponent;

                case global::TPD.tpdResultVectorType.tpdUnmetHours:
                    return ResultDataType.UnmetHours;
            }

            return ResultDataType.Undefined;
        }
    }
}
