using SAM.Core;
using System.Reflection;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static OpeningSimulationResult ToSAM_OpeningSimulationResult(this TAS3D.window window)
        {
            if (window == null)
            {
                return null;
            }

            OpeningSimulationResult result = new OpeningSimulationResult(window.name, Assembly.GetExecutingAssembly().GetName()?.Name, window.name);
            result.SetValue("Color", Core.Convert.ToColor(window.colour));
            result.SetValue("Description", window.description);
            result.SetValue("Frame Depth", window.frameDepth);
            result.SetValue("Frame Width", window.frameWidth);
            result.SetValue("Height", window.height);
            result.SetValue("Internal Shadows", window.internalShadows);
            result.SetValue("Position Type", window.positionType);
            result.SetValue("Pane Depth", window.paneDepth);
            result.SetValue("Pane Offset", window.paneOffset);
            result.SetValue("Transparent", window.paneOffset);
            result.SetValue("Width", window.width);
            result.SetValue("Level", window.level);
            result.SetValue("Frame Percentage", window.level);
            result.SetValue("Frame Guid", window.frameGUID);
            result.SetValue("Pane Guid", window.paneGUID);
            result.SetValue("Is Percentage Frame", window.isPercFrame);
            result.SetValue("Is Used", window.isUsed);

            return result;
        }
    }
}
