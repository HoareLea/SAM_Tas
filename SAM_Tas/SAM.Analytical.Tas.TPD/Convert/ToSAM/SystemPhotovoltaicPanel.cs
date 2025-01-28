using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPhotovoltaicPanel ToSAM(this PVPanel pVPanel)
        {
            if (pVPanel == null)
            {
                return null;
            }

            dynamic @dynamic = pVPanel;

            SystemPhotovoltaicPanel result = new SystemPhotovoltaicPanel(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.PanelEfficiency = ((ProfileData)@dynamic.PanelEfficiency).ToSAM();
            result.InverterSize = ((SizedVariable)@dynamic.InverterSize).ToSAM();
            result.Multiplicity = System.Convert.ToInt32(@dynamic.Multiplicity);
            result.InverterEfficiency = ((ProfileData)@dynamic.InverterEfficiency).ToSAM();
            result.UseZoneSurface = dynamic.UseZoneSurface;
            result.Area = dynamic.Area;
            result.Inclination = ((ProfileData)@dynamic.Inclination).ToSAM();
            result.Orientation = ((ProfileData)@dynamic.Orientation).ToSAM();
            result.Reflectance = @dynamic.Reflectance;
            result.MinIrradiance = @dynamic.MinIrradiance;
            result.NOCT = @dynamic.NOCT;
            result.PowerTemperatureCoefficient = @dynamic.PowerTempCoeff;
            result.UseSTC = dynamic.UseSTC;
            result.OutputAtSTC = dynamic.OutputAtSTC;
            result.DeratingFactor = dynamic.DeratingFactor;

            result.Description = dynamic.Description;

            List<FuelSource> fuelSources = Query.FuelSources(pVPanel as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName, fuelSources[0]?.Name);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemPhotovoltaicPanel displaySystemPhotovoltaicPanel = Systems.Create.DisplayObject<DisplaySystemPhotovoltaicPanel>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemPhotovoltaicPanel != null)
            {
                ITransform2D transform2D = ((IPlantComponent)pVPanel).Transform2D();
                if (transform2D != null)
                {
                    displaySystemPhotovoltaicPanel.Transform(transform2D);
                }

                result = displaySystemPhotovoltaicPanel;
            }

            return result;
        }
    }
}