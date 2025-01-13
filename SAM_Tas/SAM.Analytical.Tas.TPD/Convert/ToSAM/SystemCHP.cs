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

            SystemCHP result = new SystemCHP(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.Setpoint = ((ProfileData)dynamic.Setpoint)?.ToSAM();
            result.Efficiency = ((ProfileData)dynamic.Efficiency)?.ToSAM();
            result.HeatPowerRatio = ((ProfileData)dynamic.HeatPowerRatio)?.ToSAM();
            result.Duty = ((SizedVariable)dynamic.Duty)?.ToSAM();
            result.DesignTemperatureDifference = dynamic.DesignDeltaT;
            result.Capacity = dynamic.Capacity;
            result.DesignPressureDrop = dynamic.DesignPressureDrop;
            result.LossesInSizing = dynamic.LossesInSizing;

            List<FuelSource> fuelSources = Query.FuelSources(cHP as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0].Name);
                if(fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName, fuelSources[1].Name);
                }
            }

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