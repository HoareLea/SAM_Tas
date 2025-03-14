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
            result.MinimumReturnTemperature = dynamic.MinimumReturnTemp;
            result.VariableFlowCapacity = dynamic.VariableFlowCapacity;
            result.PeakDemand = dynamic.PeakDemand;
            result.SizeFraction = dynamic.SizeFraction;
            result.DesignPressureDrop = dynamic.DesignPressureDrop;
            result.DesignTemperatureDifference = dynamic.DesignDeltaT;

            bool isEfficiency = !dynamic.UseDistributionHeatLossProfile;
            ProfileData profileData = isEfficiency ? dynamic.DistributionEfficiency : dynamic.DistributionHeatLossProfile;
            result.Distribution = profileData.ToSAM(isEfficiency);

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