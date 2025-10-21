using TPD;
using SAM.Geometry.Planar;
using SAM.Geometry.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Core.Systems.SystemLabel ToSAM(this PlantLabel plantLabel)
        {
            if (plantLabel == null)
            {
                return null;
            }

            dynamic @dynamic = plantLabel;

            Core.Systems.SystemLabel result = new Core.Systems.SystemLabel(plantLabel.Label);

            Modify.SetReference(result, plantLabel.Reference());

            Core.Systems.LabelDirection labelDirection = plantLabel.GetDirection().ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemLabel displaySystemLabel = new DisplaySystemLabel(result, location, labelDirection, plantLabel.GetHeight(), plantLabel.GetWidth());
            if(displaySystemLabel != null)
            {
                result = displaySystemLabel;
            }

            return result;
        }

        public static Core.Systems.SystemLabel ToSAM(this SystemLabel systemLabel)
        {
            dynamic @dynamic = systemLabel;

            Core.Systems.SystemLabel result = new Core.Systems.SystemLabel(systemLabel.Label);

            Modify.SetReference(result, systemLabel.Reference());

            Core.Systems.LabelDirection labelDirection = systemLabel.GetDirection().ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemLabel displaySystemLabel = new DisplaySystemLabel(result, location, labelDirection, systemLabel.GetHeight(), systemLabel.GetWidth());
            if (displaySystemLabel != null)
            {
                result = displaySystemLabel;
            }

            return result;
        }
    }
}
