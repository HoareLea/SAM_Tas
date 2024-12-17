using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatingSystemCollection ToSAM(this HeatingGroup heatingGroup)
        {
            if (heatingGroup == null)
            {
                return null;
            }

            dynamic @dynamic = heatingGroup;

            HeatingSystemCollection result = new HeatingSystemCollection(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.MinimumReturnTemperature = heatingGroup.MinimumReturnTemp;
            result.VariableFlowCapacity = heatingGroup.VariableFlowCapacity == 1;
            result.PeakDemand = heatingGroup.PeakDemand;
            result.SizeFraction = heatingGroup.SizeFraction;
            if (heatingGroup.UseDistributionHeatLossProfile == -1)
            {
                result.Distribution = heatingGroup.DistributionHeatLossProfile.ToSAM();
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplayHeatingSystemCollection displayHeatingSystemCollection = Systems.Create.DisplayObject<DisplayHeatingSystemCollection>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displayHeatingSystemCollection != null)
            {
                ITransform2D transform2D = ((IPlantComponent)heatingGroup).Transform2D();
                if (transform2D != null)
                {
                    displayHeatingSystemCollection.Transform(transform2D);
                }

                result = displayHeatingSystemCollection;
            }

            return result;
        }
    }
}