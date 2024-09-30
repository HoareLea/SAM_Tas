using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEnergyCentreDataType ToSAM(this global::TPD.tpdResultVectorType tpdResultVectorType)
        {
            switch (tpdResultVectorType)
            {
                case global::TPD.tpdResultVectorType.tpdLoad:
                    return SystemEnergyCentreDataType.Load;

                case global::TPD.tpdResultVectorType.tpdElectricLoad:
                    return SystemEnergyCentreDataType.ElectricLoad;

                case global::TPD.tpdResultVectorType.tpdThermalLoad:
                    return SystemEnergyCentreDataType.ThermalLoad;

                case global::TPD.tpdResultVectorType.tpdDemand:
                    return SystemEnergyCentreDataType.Demand;

                case global::TPD.tpdResultVectorType.tpdElectricDemand:
                    return SystemEnergyCentreDataType.ElectricDemand ;

                case global::TPD.tpdResultVectorType.tpdThermalDemand:
                    return SystemEnergyCentreDataType.ThermalDemand;

                case global::TPD.tpdResultVectorType.tpdConsumption:
                    return SystemEnergyCentreDataType.Consumption;

                case global::TPD.tpdResultVectorType.tpdElectricConsumption:
                    return SystemEnergyCentreDataType.ElectricConsumption;

                case global::TPD.tpdResultVectorType.tpdThermalConsumption:
                    return SystemEnergyCentreDataType.ThermalConsumption;

                case global::TPD.tpdResultVectorType.tpdGenerated:
                    return SystemEnergyCentreDataType.Generated;

                case global::TPD.tpdResultVectorType.tpdCo2:
                    return SystemEnergyCentreDataType.Co2;

                case global::TPD.tpdResultVectorType.tpdCost:
                    return SystemEnergyCentreDataType.Cost;

                case global::TPD.tpdResultVectorType.tpdFuelType:
                    return SystemEnergyCentreDataType.FuelType;

                case global::TPD.tpdResultVectorType.tpdElectricFuelType:
                    return SystemEnergyCentreDataType.ElectricFuelType;

                case global::TPD.tpdResultVectorType.tpdThermalFuelType:
                    return SystemEnergyCentreDataType.ThermalFuelType;

                case global::TPD.tpdResultVectorType.tpdPlantComp:
                    return SystemEnergyCentreDataType.PlantComponent;

                case global::TPD.tpdResultVectorType.tpdElectricPlantComp:
                    return SystemEnergyCentreDataType.ElectricPlantComponent;

                case global::TPD.tpdResultVectorType.tpdThermalPlantComp:
                    return SystemEnergyCentreDataType.ThermalPlantComponent;

                case global::TPD.tpdResultVectorType.tpdUnmetHours:
                    return SystemEnergyCentreDataType.UnmetHours;
            }

            return SystemEnergyCentreDataType.Undefined;
        }
    }
}
