using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static double ThermalTransmittance(this buildingElement buildingElement, double tolerance = Core.Tolerance.MacroDistance)
        {
            TBD.Construction construction = buildingElement?.GetConstruction();
            if (construction == null)
                return double.NaN;

            object @object = construction.GetUValue();
            float[] values = Array<float>(@object);
            if (values == null || values.Length == 0)
                return double.NaN;


            int index = -1;
            switch ((BuildingElementType)buildingElement.BEType)
            {
                case BuildingElementType.SHADEELEMENT:
                case BuildingElementType.SOLARPANEL:
                case BuildingElementType.NOBETYPE:
                case BuildingElementType.NULLELEMENT:
                    index = -1;
                    break;

                case BuildingElementType.UNDERGROUNDWALL:
                case BuildingElementType.VEHICLEDOOR:
                case BuildingElementType.FRAMEELEMENT:
                case BuildingElementType.EXTERNALWALL:
                case BuildingElementType.DOORELEMENT:
                    index = 0;
                    break;

                case BuildingElementType.ROOFELEMENT:
                    index = 1;
                    break;

                case BuildingElementType.UNDERGROUNDCEILING:
                case BuildingElementType.UNDERGROUNDSLAB:
                case BuildingElementType.SLABONGRADE:
                case BuildingElementType.EXPOSEDFLOOR:
                    index = 2;
                    break;

                case BuildingElementType.INTERNALWALL:
                    index = 3;
                    break;

                case BuildingElementType.CEILING:
                    index = 4;
                    break;

                case BuildingElementType.INTERNALFLOOR:
                case BuildingElementType.RAISEDFLOOR:
                    index = 5;
                    break;
                    
                case BuildingElementType.ROOFLIGHT:
                case BuildingElementType.CURTAINWALL:
                case BuildingElementType.GLAZING:
                    index = 6;
                    break;
            }

            return index == -1 || values.Length > index ? double.NaN : Core.Query.Round(values[index], tolerance);
        }
    }
}