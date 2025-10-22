using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PlantLabel ToTPD(this Core.Systems.SystemLabel systemLabel, IPlantComponent plantComponent)
        {
            if (systemLabel == null || plantComponent == null)
            {
                return null;
            }

            dynamic @dynamic = plantComponent;

            PlantLabel result = @dynamic.AddLabel(systemLabel.Text);
            result.Label = systemLabel.Text;

            if (systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                Point2D point2D = displaySystemLabel.Location.ToTPD();
                result.SetPosition(System.Convert.ToInt32(point2D.X), System.Convert.ToInt32(point2D.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
                result.SetSize(System.Convert.ToInt32(displaySystemLabel.Width), System.Convert.ToInt32(displaySystemLabel.Height));
            }

            return result;
        }

        public static PlantLabel ToTPD(this Core.Systems.SystemLabel systemLabel, Pipe pipe)
        {
            if (systemLabel == null || pipe == null)
            {
                return null;
            }

            PlantLabel result = pipe.AddLabel(systemLabel.Text);
            result.Label = systemLabel.Text;

            if (systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                Point2D point2D = displaySystemLabel.Location.ToTPD();
                result.SetPosition(System.Convert.ToInt32(point2D.X), System.Convert.ToInt32(point2D.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
                result.SetSize(System.Convert.ToInt32(displaySystemLabel.Width), System.Convert.ToInt32(displaySystemLabel.Height));
            }

            return result;
        }

        public static PlantLabel ToTPD(this Core.Systems.SystemLabel systemLabel, PlantController plantController)
        {
            if (systemLabel == null || plantController == null)
            {
                return null;
            }

            PlantLabel result = plantController.AddLabel(systemLabel.Text);
            result.Label = systemLabel.Text;

            if (systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                Point2D point2D = displaySystemLabel.Location.ToTPD();
                result.SetPosition(System.Convert.ToInt32(point2D.X), System.Convert.ToInt32(point2D.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
                result.SetSize(System.Convert.ToInt32(displaySystemLabel.Width), System.Convert.ToInt32(displaySystemLabel.Height));
            }

            return result;
        }
    }
}
