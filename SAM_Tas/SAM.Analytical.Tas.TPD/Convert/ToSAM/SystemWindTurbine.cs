using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWindTurbine ToSAM(this WindTurbine windTurbine)
        {
            if (windTurbine == null)
            {
                return null;
            }

            dynamic @dynamic = windTurbine;

            SystemWindTurbine result = new SystemWindTurbine(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.HubHeight = @dynamic.HubHeight;
            result.Area = @dynamic.Area;
            result.MinSpeed = @dynamic.MinSpeed;
            result.CutOffSpeed = @dynamic.CutOffSpeed;
            result.Multiplicity = @dynamic.Multiplicity;
            result.Efficiency = ((ProfileData)@dynamic.Efficiency).ToSAM();

            result.Description = dynamic.Description;

            List<FuelSource> fuelSources = Query.FuelSources(windTurbine as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName, fuelSources[0]?.Name);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemWindTurbine displaySystemWindTurbine = Systems.Create.DisplayObject<DisplaySystemWindTurbine>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemWindTurbine != null)
            {
                ITransform2D transform2D = ((IPlantComponent)windTurbine).Transform2D();
                if (transform2D != null)
                {
                    displaySystemWindTurbine.Transform(transform2D);
                }

                result = displaySystemWindTurbine;
            }

            return result;
        }
    }
}