using SAM.Core;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static double ThermalTransmittance(this buildingElement buildingElement, double tolerance = Tolerance.MacroDistance)
        {
            TBD.Construction construction = buildingElement?.GetConstruction();
            if (construction == null)
                return double.NaN;

            object @object = construction.GetUValue();
            float[] values = Array<float>(@object);
            if (values == null || values.Length == 0)
                return double.NaN;


            int index = -1;
            switch ((TBD.BuildingElementType)buildingElement.BEType)
            {
                case TBD.BuildingElementType.SHADEELEMENT:
                case TBD.BuildingElementType.SOLARPANEL:
                case TBD.BuildingElementType.NOBETYPE:
                case TBD.BuildingElementType.NULLELEMENT:
                    index = -1;
                    break;

                case TBD.BuildingElementType.UNDERGROUNDWALL:
                case TBD.BuildingElementType.VEHICLEDOOR:
                case TBD.BuildingElementType.FRAMEELEMENT:
                case TBD.BuildingElementType.EXTERNALWALL:
                case TBD.BuildingElementType.DOORELEMENT:
                    index = 0;
                    break;

                case TBD.BuildingElementType.ROOFELEMENT:
                    index = 1;
                    break;

                case TBD.BuildingElementType.UNDERGROUNDCEILING:
                case TBD.BuildingElementType.UNDERGROUNDSLAB:
                case TBD.BuildingElementType.SLABONGRADE:
                case TBD.BuildingElementType.EXPOSEDFLOOR:
                    index = 2;
                    break;

                case TBD.BuildingElementType.INTERNALWALL:
                    index = 3;
                    break;

                case TBD.BuildingElementType.CEILING:
                    index = 4;
                    break;

                case TBD.BuildingElementType.INTERNALFLOOR:
                case TBD.BuildingElementType.RAISEDFLOOR:
                    index = 5;
                    break;
                    
                case TBD.BuildingElementType.ROOFLIGHT:
                case TBD.BuildingElementType.CURTAINWALL:
                case TBD.BuildingElementType.GLAZING:
                    index = 6;
                    break;
            }

            return index == -1 || values.Length <= index ? double.NaN : Core.Query.Round(values[index], tolerance);
        }

        public static double ThermalTransmittance(this TBD.Construction construction, PanelType panelType, double tolerance = Tolerance.MacroDistance)
        {
            object @object = construction?.GetUValue();
            if(@object == null)
            {
                return double.NaN;
            }

            return ThermalTransmittance(Array<float>(@object), panelType, tolerance);
        }

        public static double ThermalTransmittance(this TCD.Construction construction, PanelType panelType, double tolerance = Tolerance.MacroDistance)
        {
            object @object = construction?.GetUValue();
            if (@object == null)
            {
                return double.NaN;
            }

            return ThermalTransmittance(Array<float>(@object), panelType, tolerance);
        }

        public static double ThermalTransmittance(this TCD.Construction construction, HeatFlowDirection heatFlowDirection, bool external, double tolerance = Tolerance.MacroDistance)
        {
            if(construction == null || heatFlowDirection == Analytical.HeatFlowDirection.Undefined)
            {
                return double.NaN;
            }

            object @object = construction?.GetUValue();
            if (@object == null)
            {
                return double.NaN;
            }

            float[] values = Array<float>(@object);
            if (values == null || values.Length == 0)
            {
                return double.NaN;
            }

            if(construction.type == TCD.ConstructionTypes.tcdTransparentConstruction)
            {
                return values[6];
            }

            int index = -1;
            switch (heatFlowDirection)
            {
                case Analytical.HeatFlowDirection.Down:
                    index = external ? 2 : 5;
                    break;

                case Analytical.HeatFlowDirection.Up:
                    index = external ? 1 : 4;
                    break;

                case Analytical.HeatFlowDirection.Horizontal:
                    index = external ? 0 : 3;
                    break;
            }

            return index == -1 || values.Length <= index ? double.NaN : Core.Query.Round(values[index], tolerance);

        }

        public static double ThermalTransmittance(this PanelType panelType, out HeatFlowDirection heatFlowDirection, out bool external)
        {
            heatFlowDirection = Analytical.HeatFlowDirection.Undefined;
            external = false;

            if(panelType == Analytical.PanelType.Undefined)
            {
                return double.NaN;
            }

            double result = double.NaN;
            switch (panelType.PanelGroup())
            {
                case PanelGroup.Wall:
                    result = 0.24;
                    heatFlowDirection = Analytical.HeatFlowDirection.Horizontal;
                    break;

                case PanelGroup.Roof:
                    result = 0.16;
                    heatFlowDirection = Analytical.HeatFlowDirection.Up;
                    break;

                case PanelGroup.Floor:
                    result = 0.14;
                    heatFlowDirection = Analytical.HeatFlowDirection.Down;
                    break;
            }

            if (panelType != Analytical.PanelType.Undefined)
            {
                switch (panelType)
                {
                    case Analytical.PanelType.Wall:
                    case Analytical.PanelType.Roof:
                    case Analytical.PanelType.Floor:
                        external = true;
                        break;

                    default:
                        external = panelType.External();
                        break;
                }
            }

            return result;

        }

        private static double ThermalTransmittance(float[] values, PanelType panelType, double tolerance = Tolerance.MacroDistance)
        {
            if (values == null || values.Length == 0)
            {
                return double.NaN;
            }

            int index = -1;
            switch (panelType)
            {
                case Analytical.PanelType.Shade:
                case Analytical.PanelType.SolarPanel:
                case Analytical.PanelType.Undefined:
                    index = -1;
                    break;

                case Analytical.PanelType.UndergroundWall:
                case Analytical.PanelType.WallExternal:
                    index = 0;
                    break;

                case Analytical.PanelType.Roof:
                    index = 1;
                    break;

                case Analytical.PanelType.UndergroundCeiling:
                case Analytical.PanelType.UndergroundSlab:
                case Analytical.PanelType.SlabOnGrade:
                case Analytical.PanelType.FloorExposed:
                    index = 2;
                    break;

                case Analytical.PanelType.WallInternal:
                    index = 3;
                    break;

                case Analytical.PanelType.Ceiling:
                    index = 4;
                    break;

                case Analytical.PanelType.FloorInternal:
                case Analytical.PanelType.FloorRaised:
                    index = 5;
                    break;

                case Analytical.PanelType.CurtainWall:
                    index = 6;
                    break;
            }

            return index == -1 || values.Length <= index ? double.NaN : Core.Query.Round(values[index], tolerance);
        }
    }
}