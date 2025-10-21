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

            PlantLabel result = plantComponent.AddLabel(systemLabel.Text);

            if(systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                result.SetPosition(System.Convert.ToInt32(displaySystemLabel.Location.X), System.Convert.ToInt32(displaySystemLabel.Location.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
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

            if (systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                result.SetPosition(System.Convert.ToInt32(displaySystemLabel.Location.X), System.Convert.ToInt32(displaySystemLabel.Location.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
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

            if (systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                result.SetPosition(System.Convert.ToInt32(displaySystemLabel.Location.X), System.Convert.ToInt32(displaySystemLabel.Location.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
            }

            return result;
        }
    }
}
