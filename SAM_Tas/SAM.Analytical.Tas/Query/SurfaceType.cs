namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TBD.SurfaceType SurfaceType(this PanelType panelType)
        {
            switch(panelType)
            {
                case Analytical.PanelType.Air:
                    return TBD.SurfaceType.tbdLink;
                case Analytical.PanelType.Ceiling:
                    return TBD.SurfaceType.tbdLink;
                case Analytical.PanelType.CurtainWall:
                    return TBD.SurfaceType.tbdExposed;
                case Analytical.PanelType.Floor:
                    return TBD.SurfaceType.tbdLink;
                case Analytical.PanelType.FloorExposed:
                    return TBD.SurfaceType.tbdExposed;
                case Analytical.PanelType.FloorInternal:
                    return TBD.SurfaceType.tbdLink;
                case Analytical.PanelType.FloorRaised:
                    return TBD.SurfaceType.tbdExposed;
                case Analytical.PanelType.Roof:
                    return TBD.SurfaceType.tbdExposed;
                case Analytical.PanelType.Shade:
                    return TBD.SurfaceType.tbdExposed;
                case Analytical.PanelType.SlabOnGrade:
                    return TBD.SurfaceType.tbdGround;
                case Analytical.PanelType.SolarPanel:
                    return TBD.SurfaceType.tbdExposed;
                case Analytical.PanelType.Undefined:
                    return TBD.SurfaceType.tbdNullLink;
                case Analytical.PanelType.UndergroundCeiling:
                    return TBD.SurfaceType.tbdGround;
                case Analytical.PanelType.UndergroundSlab:
                    return TBD.SurfaceType.tbdGround;
                case Analytical.PanelType.UndergroundWall:
                    return TBD.SurfaceType.tbdGround;
                case Analytical.PanelType.Wall:
                    return TBD.SurfaceType.tbdExposed;
                case Analytical.PanelType.WallExternal:
                    return TBD.SurfaceType.tbdExposed;
                case Analytical.PanelType.WallInternal:
                    return TBD.SurfaceType.tbdLink;
            }

            return TBD.SurfaceType.tbdNullLink;
        }
    }
}