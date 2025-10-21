using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemLabel ToTPD(this Core.Systems.SystemLabel systemLabel, ISystemComponent systemComponet)
        {
            if (systemLabel == null || systemComponet == null)
            {
                return null;
            }

            SystemLabel result =  systemComponet.AddLabel(systemLabel.Text);

            if(systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                result.SetPosition(System.Convert.ToInt32(displaySystemLabel.Location.X), System.Convert.ToInt32(displaySystemLabel.Location.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
                result.SetSize(System.Convert.ToInt32(displaySystemLabel.Width), System.Convert.ToInt32(displaySystemLabel.Height));
            }

            return result;
        }

        public static SystemLabel ToTPD(this Core.Systems.SystemLabel systemLabel, Duct duct)
        {
            if (systemLabel == null || duct == null)
            {
                return null;
            }

            SystemLabel result = duct.AddLabel(systemLabel.Text);

            if (systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                result.SetPosition(System.Convert.ToInt32(displaySystemLabel.Location.X), System.Convert.ToInt32(displaySystemLabel.Location.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
                result.SetSize(System.Convert.ToInt32(displaySystemLabel.Width), System.Convert.ToInt32(displaySystemLabel.Height));
            }

            return result;
        }

        public static SystemLabel ToTPD(this Core.Systems.SystemLabel systemLabel, Controller controller)
        {
            if (systemLabel == null || controller == null)
            {
                return null;
            }

            SystemLabel result = controller.AddLabel(systemLabel.Text);

            if (systemLabel is Geometry.Systems.DisplaySystemLabel)
            {
                Geometry.Systems.DisplaySystemLabel displaySystemLabel = (Geometry.Systems.DisplaySystemLabel)systemLabel;

                result.SetPosition(System.Convert.ToInt32(displaySystemLabel.Location.X), System.Convert.ToInt32(displaySystemLabel.Location.Y));
                result.SetDirection(displaySystemLabel.LabelDirection.ToTPD());
                result.SetSize(System.Convert.ToInt32(displaySystemLabel.Width), System.Convert.ToInt32(displaySystemLabel.Height));
            }

            return result;
        }
    }
}
