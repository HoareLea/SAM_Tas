using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static AnalyticalSystemComponentType AnalyticalSystemComponentType(this ISystemComponent systemComponent)
        { 
            if(systemComponent == null)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.Undefined;
            }

            if(systemComponent is global::TPD.CoolingCoil)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemCoolingCoil;
            }

            if (systemComponent is global::TPD.HeatingCoil)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemHeatingCoil;
            }

            if (systemComponent is Junction)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemAirJunction;
            }

            if (systemComponent is global::TPD.Fan)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemFan;
            }

            if (systemComponent is DesiccantWheel)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemDesiccantWheel;
            }

            if (systemComponent is Exchanger)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemExchanger;
            }

            if (systemComponent is SystemZone)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemSpace;
            }

            if (systemComponent is Damper)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemDamper;
            }

            return Analytical.Systems.AnalyticalSystemComponentType.Undefined;
        }

        public static AnalyticalSystemComponentType AnalyticalSystemComponentType(this IPlantComponent plantComponent)
        {
            if (plantComponent is Pump)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemPump;
            }

            if (plantComponent is PlantJunction)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemLiquidJunction;
            }

            if (plantComponent is BoilerPlant)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemBoiler;
            }

            if (plantComponent is Chiller)
            {
                if(((Chiller)plantComponent).IsDirectAbsChiller == -1)
                {
                    return Analytical.Systems.AnalyticalSystemComponentType.SystemAirSourceDirectAbsorptionChiller;
                }

                return Analytical.Systems.AnalyticalSystemComponentType.SystemAirSourceChiller;
            }

            if (plantComponent is AbsorptionChiller)
            {
                if (((AbsorptionChiller)plantComponent).IsWaterSource == -1)
                {
                    return Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceAbsorptionChiller;
                }

                return Analytical.Systems.AnalyticalSystemComponentType.SystemAbsorptionChiller;
            }

            if (plantComponent is IceStorageChiller)
            {
                if (((IceStorageChiller)plantComponent).IsWaterSource == -1)
                {
                    return Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceIceStorageChiller;
                }

                return Analytical.Systems.AnalyticalSystemComponentType.SystemIceStorageChiller;
            }

            if (plantComponent is WaterSourceChiller)
            {
                if (((WaterSourceChiller)plantComponent).IsDirectAbsChiller == -1)
                {
                    return Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceDirectAbsorptionChiller;
                }

                return Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceChiller;
            }

            if (plantComponent is MultiChiller)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemMultiChiller;
            }

            if (plantComponent is MultiBoiler)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemMultiBoiler;
            }

            if (plantComponent is AirSourceHeatPump)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemAirSourceHeatPump;
            }

            if (plantComponent is WaterToWaterHeatPump)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemWaterToWaterHeatPump;
            }

            if (plantComponent is HeatPump)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceHeatPump;
            }

            if (plantComponent is HeatPump)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceHeatPump;
            }

            if (plantComponent is Tank)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemTank;
            }

            if (plantComponent is PipeLossComponent)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemPipeLossComponent;
            }

            if (plantComponent is HeatExchanger)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemLiquidExchanger;
            }

            if (plantComponent is CoolingTower)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemCoolingTower;
            }

            if (plantComponent is DryCooler)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemDryCooler;
            }

            if (plantComponent is GroundSource)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemVerticalBorehole;
            }

            if (plantComponent is SlinkyCoil)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemSlinkyCoil;
            }

            if (plantComponent is CHP)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemCHP;
            }

            if (plantComponent is SurfaceWaterExchanger)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemSurfaceWaterExchanger;
            }

            if (plantComponent is HorizontalGHE)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemHorizontalExchanger;
            }

            if (plantComponent is SolarPanel)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemSolarPanel;
            }

            if (plantComponent is PVPanel)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemPhotovoltaicPanel;
            }

            if (plantComponent is Valve)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemValve;
            }

            if (plantComponent is WindTurbine)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemWindTurbine;
            }

            if (plantComponent is CoolingGroup)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.CoolingSystemCollection;
            }

            if (plantComponent is DHWGroup)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.DomesticHotWaterSystemCollection;
            }

            if (plantComponent is ElectricalGroup)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.ElectricalSystemCollection;
            }

            if (plantComponent is FuelGroup)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.FuelSystemCollection;
            }

            if (plantComponent is HeatingGroup)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.HeatingSystemCollection;
            }

            if (plantComponent is RefrigerantGroup)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.RefrigerantSystemCollection;
            }

            //TODO: Add Plant Component Types

            return Analytical.Systems.AnalyticalSystemComponentType.Undefined;
        }
    }
}