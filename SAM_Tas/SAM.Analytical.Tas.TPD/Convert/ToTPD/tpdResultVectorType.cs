using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.tpdResultVectorType ToTPD(this SystemEnergyCentreDataType systemEnergyCentreDataType)
        {
            switch (systemEnergyCentreDataType)
            {
                case SystemEnergyCentreDataType.Load:
                    return global::TPD.tpdResultVectorType.tpdLoad;

                case SystemEnergyCentreDataType.ElectricLoad:
                    return global::TPD.tpdResultVectorType.tpdElectricLoad;

                case SystemEnergyCentreDataType.ThermalLoad:
                    return global::TPD.tpdResultVectorType.tpdThermalLoad;

                case SystemEnergyCentreDataType.Demand:
                    return global::TPD.tpdResultVectorType.tpdDemand;

                case SystemEnergyCentreDataType.ElectricDemand:
                    return global::TPD.tpdResultVectorType.tpdElectricDemand;

                case SystemEnergyCentreDataType.ThermalDemand:
                    return global::TPD.tpdResultVectorType.tpdThermalDemand;

                case SystemEnergyCentreDataType.Consumption:
                    return global::TPD.tpdResultVectorType.tpdConsumption;

                case SystemEnergyCentreDataType.ElectricConsumption:
                    return global::TPD.tpdResultVectorType.tpdElectricConsumption;

                case SystemEnergyCentreDataType.ThermalConsumption:
                    return global::TPD.tpdResultVectorType.tpdThermalConsumption;

                case SystemEnergyCentreDataType.Generated:
                    return global::TPD.tpdResultVectorType.tpdGenerated;

                case SystemEnergyCentreDataType.Co2:
                    return global::TPD.tpdResultVectorType.tpdCo2;

                case SystemEnergyCentreDataType.Cost:
                    return global::TPD.tpdResultVectorType.tpdCost;

                case SystemEnergyCentreDataType.FuelType:
                    return global::TPD.tpdResultVectorType.tpdFuelType;

                case SystemEnergyCentreDataType.ElectricFuelType:
                    return global::TPD.tpdResultVectorType.tpdElectricFuelType;

                case SystemEnergyCentreDataType.ThermalFuelType:
                    return global::TPD.tpdResultVectorType.tpdThermalFuelType;

                case SystemEnergyCentreDataType.PlantComponent:
                    return global::TPD.tpdResultVectorType.tpdPlantComp;

                case SystemEnergyCentreDataType.ElectricPlantComponent:
                    return global::TPD.tpdResultVectorType.tpdElectricPlantComp;

                case SystemEnergyCentreDataType.ThermalPlantComponent:
                    return global::TPD.tpdResultVectorType.tpdThermalPlantComp;

                case SystemEnergyCentreDataType.UnmetHours:
                    return global::TPD.tpdResultVectorType.tpdUnmetHours;
            }

            throw new System.NotImplementedException();
        }
    }
}
