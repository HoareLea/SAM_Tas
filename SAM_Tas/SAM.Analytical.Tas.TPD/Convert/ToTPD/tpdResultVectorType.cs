using SAM.Core.Tas.TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.tpdResultVectorType ToTPD(this ResultDataType resultDataType)
        {
            switch (resultDataType)
            {
                case ResultDataType.Load:
                    return global::TPD.tpdResultVectorType.tpdLoad;

                case ResultDataType.ElectricLoad:
                    return global::TPD.tpdResultVectorType.tpdElectricLoad;

                case ResultDataType.ThermalLoad:
                    return global::TPD.tpdResultVectorType.tpdThermalLoad;

                case ResultDataType.Demand:
                    return global::TPD.tpdResultVectorType.tpdDemand;

                case ResultDataType.ElectricDemand:
                    return global::TPD.tpdResultVectorType.tpdElectricDemand;

                case ResultDataType.ThermalDemand:
                    return global::TPD.tpdResultVectorType.tpdThermalDemand;

                case ResultDataType.Consumption:
                    return global::TPD.tpdResultVectorType.tpdConsumption;

                case ResultDataType.ElectricConsumption:
                    return global::TPD.tpdResultVectorType.tpdElectricConsumption;

                case ResultDataType.ThermalConsumption:
                    return global::TPD.tpdResultVectorType.tpdThermalConsumption;

                case ResultDataType.Generated:
                    return global::TPD.tpdResultVectorType.tpdGenerated;

                case ResultDataType.Co2:
                    return global::TPD.tpdResultVectorType.tpdCo2;

                case ResultDataType.Cost:
                    return global::TPD.tpdResultVectorType.tpdCost;

                case ResultDataType.FuelType:
                    return global::TPD.tpdResultVectorType.tpdFuelType;

                case ResultDataType.ElectricFuelType:
                    return global::TPD.tpdResultVectorType.tpdElectricFuelType;

                case ResultDataType.ThermalFuelType:
                    return global::TPD.tpdResultVectorType.tpdThermalFuelType;

                case ResultDataType.PlantComponent:
                    return global::TPD.tpdResultVectorType.tpdPlantComp;

                case ResultDataType.ElectricPlantComponent:
                    return global::TPD.tpdResultVectorType.tpdElectricPlantComp;

                case ResultDataType.ThermalPlantComponent:
                    return global::TPD.tpdResultVectorType.tpdThermalPlantComp;

                case ResultDataType.UnmetHours:
                    return global::TPD.tpdResultVectorType.tpdUnmetHours;
            }

            throw new System.NotImplementedException();
        }
    }
}
