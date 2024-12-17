using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CoolingSystemCollection ToSAM(this CoolingGroup coolingGroup)
        {
            if (coolingGroup == null)
            {
                return null;
            }

            dynamic @dynamic = coolingGroup;

            CoolingSystemCollection result = new CoolingSystemCollection(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.MaximumReturnTemperature = coolingGroup.MaximumReturnTemp;
            result.VariableFlowCapacity = coolingGroup.VariableFlowCapacity == 1;
            result.PeakDemand = coolingGroup.PeakDemand;
            result.SizeFraction = coolingGroup.SizeFraction;
            if(coolingGroup.UseDistributionHeatGainProfile == -1)
            {
                result.Distribution = coolingGroup.DistributionHeatGainProfile.ToSAM();
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplayCoolingSystemCollection displayCoolingSystemCollection = Systems.Create.DisplayObject<DisplayCoolingSystemCollection>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displayCoolingSystemCollection != null)
            {
                ITransform2D transform2D = ((IPlantComponent)coolingGroup).Transform2D();
                if (transform2D != null)
                {
                    displayCoolingSystemCollection.Transform(transform2D);
                }

                result = displayCoolingSystemCollection;
            }

            return result;
        }
    }
}