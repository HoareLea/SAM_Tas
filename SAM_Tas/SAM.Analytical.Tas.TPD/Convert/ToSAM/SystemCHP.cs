using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCHP ToSAM(this CHP cHP)
        {
            if (cHP == null)
            {
                return null;
            }

            dynamic @dynamic = cHP;

            SystemCHP result = new SystemCHP(@dynamic.Name)
            {
                Description = dynamic.Description,
                Setpoint = ((ProfileData)dynamic.Setpoint)?.ToSAM(),
                Efficiency = ((ProfileData)dynamic.Efficiency)?.ToSAM(),
                HeatPowerRatio = ((ProfileData)dynamic.HeatPowerRatio)?.ToSAM(),
                Duty = ((SizedVariable)dynamic.Duty)?.ToSAM(),
                DesignTemperatureDifference = dynamic.DesignDeltaT,
                Capacity = dynamic.Capacity,
                DesignPressureDrop = dynamic.DesignPressureDrop,
                LossesInSizing = dynamic.LossesInSizing
            };

            tpdCHPFlags tpdCHPFlags = (tpdCHPFlags)cHP.Flags;
            result.IsDomesticHotWater = tpdCHPFlags.HasFlag(tpdCHPFlags.tpdCHPIsDHW);


            Modify.SetReference(result, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(cHP as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                if(fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName, fuelSources[1]?.Name);
                }
            }

            result.ScheduleName = ((dynamic)cHP )?.GetSchedule()?.Name;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemCHP displaySystemCHP = Systems.Create.DisplayObject<DisplaySystemCHP>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemCHP != null)
            {
                ITransform2D transform2D = ((IPlantComponent)cHP).Transform2D();
                if (transform2D != null)
                {
                    displaySystemCHP.Transform(transform2D);
                }

                result = displaySystemCHP;
            }

            return result;
        }
    }
}