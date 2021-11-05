using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;
using System.Reflection;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static PartitionSimulationResult ToSAM_PartitionSimulationResult(this TAS3D.Element element)
        {
            if (element == null)
                return null;

            PartitionSimulationResult result = new PartitionSimulationResult(element.name, Assembly.GetExecutingAssembly().GetName()?.Name, element.GUID);
            result.SetValue("Description", element.description);
            result.SetValue("Ground", element.ground);
            result.SetValue("Ghost", element.ghost);
            result.SetValue("BuildingElementType", ((TBD.BuildingElementType)element.BEType).ToString());
            result.SetValue("Color", Core.Convert.ToColor(element.colour));
            result.SetValue("Internal Shadows", element.internalShadows);
            result.SetValue("Secondary Proportion", element.secondaryProportion);
            result.SetValue("Transparent", element.transparent);
            result.SetValue("Width", element.width);
            result.SetValue("Zone Floor Area", element.zoneFloorArea);
            result.SetValue("Is Used", element.isUsed);
            result.SetValue("Is Preset", element.isPreset);

            if (element.secondaryElement != null)
            {
                result.SetValue("Secondary Element", element.secondaryElement.ToSAM_PartitionSimulationResult());
            }

            return result;
        }

        public static PartitionSimulationResult ToSAM_PartitionSimulationResult(this TAS3D.shade shade)
        {
            if (shade == null)
                return null;

            PartitionSimulationResult result = new PartitionSimulationResult(shade.name, Assembly.GetExecutingAssembly().GetName()?.Name, shade.name);
            result.SetValue("Color", Core.Convert.ToColor(shade.colour));
            result.SetValue("Description", shade.description);
            result.SetValue("Centre Offset", shade.centreOffset);
            result.SetValue("Frame Depth", shade.frameDepth);
            result.SetValue("Frame Offset", shade.frameOffset);
            result.SetValue("Frame Width", shade.frameWidth);
            result.SetValue("Height", shade.height);
            result.SetValue("Internal Shadows", shade.internalShadows);
            result.SetValue("Level", shade.level);
            result.SetValue("Position Type", shade.positionType);
            result.SetValue("Transparent", shade.transparent);
            result.SetValue("Width", shade.height);
            result.SetValue("Frame Guid", shade.frameGUID);
            result.SetValue("Horizontal Fins Guid", shade.horizfinsGUID);
            result.SetValue("Vertical Fins Guid", shade.vertfinsGUID);
            result.SetValue("Has Frame", shade.hasFrame);
            result.SetValue("Has Horizontal Fins", shade.hasHorizFins);
            result.SetValue("Has Vertical Fins", shade.hasVertFins);
            result.SetValue("Is Used", shade.isUsed);

            return result;
        }

        public static PartitionSimulationResult ToSAM_PartitionSimulationResult(this SurfaceData surfaceData, IEnumerable<PanelDataType> panelDataTypes = null)
        {
            if (surfaceData == null)
                return null;

            PartitionSimulationResult result = new PartitionSimulationResult(surfaceData.BEName, Assembly.GetExecutingAssembly().GetName()?.Name, surfaceData.surfaceNumber.ToString());
            result.SetValue("BuildingElementType", ((TBD.BuildingElementType)surfaceData.BEType).ToString());
            result.SetValue("Area", surfaceData.area);
            result.SetValue("Orientation", surfaceData.orientation);

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, surfaceData);

            if (panelDataTypes != null)
            {
                foreach (PanelDataType panelDataType in panelDataTypes)
                {
                    List<double> values = surfaceData.AnnualSurfaceResult<double>(panelDataType);
                    if (values == null)
                        continue;

                    JArray jArray = new JArray();
                    values.ForEach(x => jArray.Add(x));

                    parameterSet.Add(panelDataType.Text(), jArray);
                }
            }

            result.Add(parameterSet);

            return result;
        }
    }
}
