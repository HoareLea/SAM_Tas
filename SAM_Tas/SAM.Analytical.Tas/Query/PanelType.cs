namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static PanelType PanelType(this int bEType)
        {
            switch(bEType)
            {
                case 0:
                    return Analytical.PanelType.Undefined;
                case 1:
                    return Analytical.PanelType.WallInternal;
                case 2:
                    return Analytical.PanelType.WallExternal;
                case 3:
                    return Analytical.PanelType.Roof;
                case 4:
                    return Analytical.PanelType.FloorInternal;
                case 5:
                    return Analytical.PanelType.Shade;
                case 6:
                    return Analytical.PanelType.UndergroundWall;
                case 7:
                    return Analytical.PanelType.UndergroundSlab;
                case 8:
                    return Analytical.PanelType.Ceiling;
                case 9:
                    return Analytical.PanelType.UndergroundCeiling;
                case 10:
                    return Analytical.PanelType.FloorRaised;
                case 11:
                    return Analytical.PanelType.SlabOnGrade;
                case 16:
                    return Analytical.PanelType.CurtainWall;
                case 18:
                    return Analytical.PanelType.SolarPanel;
                case 19:
                    return Analytical.PanelType.FloorExposed;
            }

            return Analytical.PanelType.Undefined;
        }
    }
}