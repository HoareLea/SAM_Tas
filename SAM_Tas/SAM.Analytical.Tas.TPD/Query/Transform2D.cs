using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static ITransform2D Transform2D(this tpdDirection tpdDirection, Point2D location, AnalyticalSystemComponentType analyticalSystemComponentType)
        {
            Point2D location_Temp = location;

            List<ITransform2D> transforms = null;

            Vector2D vector2D = null;

            switch (analyticalSystemComponentType)
            {
                case Analytical.Systems.AnalyticalSystemComponentType.SystemFan:
                    switch(tpdDirection)
                    {
                        case tpdDirection.tpdTopBottom:
                            location_Temp = new Point2D(location_Temp.X + 0.2, location_Temp.Y - 0.2);
                            transforms = new List<ITransform2D>()
                            {
                                    Geometry.Planar.Transform2D.GetRotation(location_Temp, global::System.Math.PI / 2),
                                    Geometry.Planar.Transform2D.GetMirrorX(location_Temp),
                                    Geometry.Planar.Transform2D.GetMirrorY(location_Temp),
                            };
                            return new TransformGroup2D(transforms);


                        case tpdDirection.tpdBottomTop:
                            location_Temp = new Point2D(location_Temp.X + 0.2, location_Temp.Y - 0.2);
                            transforms = new List<ITransform2D>()
                            {
                                    Geometry.Planar.Transform2D.GetRotation(location_Temp, global::System.Math.PI / 2),
                                    Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.2)),
                            };
                            return new TransformGroup2D(transforms);

                        default:
                            location_Temp = new Point2D(location_Temp.X + 0.3, location_Temp.Y - 0.2);
                            break;
                    }
                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemExchanger:
                
                case Analytical.Systems.AnalyticalSystemComponentType.SystemDesiccantWheel:
                    location_Temp = new Point2D(location_Temp.X + 0.2, location_Temp.Y - 0.2);
                    vector2D = new Vector2D(0.8, 0);
                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemValve:

                    switch (tpdDirection)
                    {
                        case tpdDirection.tpdLeftRight:
                            return null;

                        case tpdDirection.tpdRightLeft:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetMirrorY(location),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.3, 0)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdBottomTop:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.3)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdTopBottom:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.2, 0)),
                            };
                            return new TransformGroup2D(transforms);
                    }

                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemPump:

                    switch (tpdDirection)
                    {
                        case tpdDirection.tpdLeftRight:
                            return null;

                        case tpdDirection.tpdRightLeft:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetMirrorY(location),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.6, 0)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdBottomTop:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.6)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdTopBottom:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.4, 0)),
                            };
                            return new TransformGroup2D(transforms);
                    }

                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemTank:

                    switch (tpdDirection)
                    {
                        case tpdDirection.tpdLeftRight:
                            return null;

                        case tpdDirection.tpdRightLeft:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetMirrorY(location),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.6, 0)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdBottomTop:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.6)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdTopBottom:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(1, 0)),
                            };
                            return new TransformGroup2D(transforms);
                    }

                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemPipeLossComponent:
                    switch (tpdDirection)
                    {
                        case tpdDirection.tpdLeftRight:
                            return null;

                        case tpdDirection.tpdRightLeft:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetMirrorY(location),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.6, 0)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdBottomTop:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.6)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdTopBottom:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.2, 0)),
                            };
                            return new TransformGroup2D(transforms);
                    }

                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemLiquidExchanger:
                    switch (tpdDirection)
                    {
                        case tpdDirection.tpdLeftRight:
                            return null;

                        case tpdDirection.tpdRightLeft:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetMirrorY(location),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.5, 0)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdBottomTop:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.5)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdTopBottom:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(1, 0)),
                            };
                            return new TransformGroup2D(transforms);
                    }
                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemAbsorptionChiller:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemAirSourceChiller:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemAirSourceDirectAbsorptionChiller:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemIceStorageChiller:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemMultiChiller:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceAbsorptionChiller:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceChiller:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceDirectAbsorptionChiller:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceIceStorageChiller:

                case Analytical.Systems.AnalyticalSystemComponentType.SystemAirSourceHeatPump:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemWaterSourceHeatPump:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemWaterToWaterHeatPump:

                case Analytical.Systems.AnalyticalSystemComponentType.SystemBoiler:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemMultiBoiler:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemCHP:

                case Analytical.Systems.AnalyticalSystemComponentType.CoolingSystemCollection:
                case Analytical.Systems.AnalyticalSystemComponentType.DomesticHotWaterSystemCollection:
                case Analytical.Systems.AnalyticalSystemComponentType.ElectricalSystemCollection:
                case Analytical.Systems.AnalyticalSystemComponentType.FuelSystemCollection:
                case Analytical.Systems.AnalyticalSystemComponentType.HeatingSystemCollection:
                case Analytical.Systems.AnalyticalSystemComponentType.RefrigerantSystemCollection:

                    switch (tpdDirection)
                    {
                        case tpdDirection.tpdLeftRight:
                            return null;

                        case tpdDirection.tpdRightLeft:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetMirrorY(location),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.6, 0)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdBottomTop:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.6)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdTopBottom:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(1, 0)),
                            };
                            return new TransformGroup2D(transforms);
                    }

                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemDryCooler:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemVerticalBorehole:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemSlinkyCoil:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemSurfaceWaterExchanger:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemHorizontalExchanger:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemCoolingTower:

                    switch (tpdDirection)
                    {
                        case tpdDirection.tpdLeftRight:
                            return null;

                        case tpdDirection.tpdRightLeft:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetMirrorY(location),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.6, 0)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdBottomTop:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.6)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdTopBottom:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.8, 0)),
                            };
                            return new TransformGroup2D(transforms);
                    }

                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemPhotovoltaicPanel:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemSolarPanel:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemWindTurbine:
                    switch (tpdDirection)
                    {
                        case tpdDirection.tpdLeftRight:
                            return null;

                        case tpdDirection.tpdRightLeft:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetMirrorY(location),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.6, 0)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdBottomTop:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, -0.6)),
                            };
                            return new TransformGroup2D(transforms);

                        case tpdDirection.tpdTopBottom:
                            transforms = new List<ITransform2D>()
                            {
                                Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                                Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0.6, 0)),
                            };
                            return new TransformGroup2D(transforms);
                    }

                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemSpace:
                    location_Temp = new Point2D(location_Temp.X + 0.3, location_Temp.Y - 0.3);
                    vector2D = new Vector2D(0, 0);
                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemDamper:
                    location_Temp = new Point2D(location_Temp.X + 0.2, location_Temp.Y - 0.2);
                    vector2D = new Vector2D(0, 0);
                    break;

                case Analytical.Systems.AnalyticalSystemComponentType.SystemAirJunction:
                case Analytical.Systems.AnalyticalSystemComponentType.SystemLiquidJunction:
                    location_Temp = new Point2D(location_Temp.X + 0.1, location_Temp.Y - 0.1);
                    vector2D = new Vector2D(0, 0);
                    break;

                default:
                    location_Temp = new Point2D(location_Temp.X + 0.1, location_Temp.Y - 0.1);
                    vector2D = new Vector2D(0.2, 0);
                    break;
            }


            switch (tpdDirection)
            {
                case tpdDirection.tpdTopBottom:

                    transforms = new List<ITransform2D>()
                    {
                        Geometry.Planar.Transform2D.GetRotation(location_Temp, - global :: System.Math.PI / 2),
                        Geometry.Planar.Transform2D.GetTranslation(vector2D),
                    };
                    return new TransformGroup2D(transforms);

                case tpdDirection.tpdLeftRight:
                    return null;

                case tpdDirection.tpdRightLeft:
                    return Geometry.Planar.Transform2D.GetMirrorY(location_Temp);

                case tpdDirection.tpdBottomTop:
                    //return Geometry.Planar.Transform2D.GetRotation(location_Temp, Math.PI / 2);
                    transforms = new List<ITransform2D>()
                    {
                        Geometry.Planar.Transform2D.GetRotation(location_Temp,  global :: System.Math.PI / 2),
                        //Geometry.Planar.Transform2D.GetTranslation(vector2D),
                    };
                    return new TransformGroup2D(transforms);
            }

            return null;
        }

        public static ITransform2D Transform2D(this ISystemComponent systemComponent)
        {
            if(systemComponent == null)
            {
                return null;
            }

            dynamic @dynamic = systemComponent;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            tpdDirection tpdDirection = (tpdDirection)(int)@dynamic.GetDirection();

            return Transform2D(tpdDirection, location, AnalyticalSystemComponentType(systemComponent));
        }

        public static ITransform2D Transform2D(this IPlantComponent plantComponent)
        {
            if (plantComponent == null)
            {
                return null;
            }

            dynamic @dynamic = plantComponent;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            tpdDirection tpdDirection = (tpdDirection)(int)@dynamic.GetDirection();

            return Transform2D(tpdDirection, location, AnalyticalSystemComponentType(plantComponent));
        }

        public static ITransform2D Transform2D(this IDisplaySystemObject<SystemGeometryInstance> displaySystemObject, out tpdDirection tpdDirection)
        {
            tpdDirection = tpdDirection.tpdLeftRight;

            CoordinateSystem2D coordinateSystem2D = displaySystemObject?.SystemGeometry.CoordinateSystem2D;
            if (coordinateSystem2D == null)
            {
                return null;
            }


            Vector2D axis_X = coordinateSystem2D.AxisX;
            Vector2D axis_Y = coordinateSystem2D.AxisY;

            Point2D point2D = coordinateSystem2D.Origin;

            if (displaySystemObject is HeatingSystemCollection ||
                displaySystemObject is CoolingSystemCollection ||
                displaySystemObject is RefrigerantSystemCollection ||
                displaySystemObject is FuelSystemCollection ||
                displaySystemObject is DomesticHotWaterSystemCollection ||
                displaySystemObject is ElectricalSystemCollection)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-1.0, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemSolarPanel ||
                displaySystemObject is SystemPhotovoltaicPanel ||
                displaySystemObject is SystemWindTurbine)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemCoolingTower ||
                displaySystemObject is SystemDryCooler ||
                displaySystemObject is SystemVerticalBorehole ||
                displaySystemObject is SystemSlinkyCoil ||
                displaySystemObject is SystemSurfaceWaterExchanger ||
                displaySystemObject is SystemHorizontalExchanger)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.8, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemChiller ||
                displaySystemObject is SystemMultiChiller ||
                displaySystemObject is SystemMultiBoiler ||
                displaySystemObject is SystemHeatPump ||
                displaySystemObject is SystemBoiler ||
                displaySystemObject is SystemCHP)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-1.0, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemPipeLossComponent)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.2, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemValve)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.3, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.3));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.2, 0));
                }

                return null;
            }

            if (displaySystemObject is SystemTank)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-1.0, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemPump)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.4, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemLiquidExchanger)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.5, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-1.0, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.5));
                }

                return null;
            }

            if (displaySystemObject is SystemHumidifier ||
                displaySystemObject is SystemSprayHumidifier ||
                displaySystemObject is SystemLoadComponent ||
                displaySystemObject is SystemDirectEvaporativeCooler)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.2, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.4, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.2));
                }

                return null;
            }

            if (displaySystemObject is SystemExchanger ||
                displaySystemObject is SystemDesiccantWheel)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.4, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-1.2, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.4));
                }

                return null;
            }

            if (displaySystemObject is SystemMixingBox ||
                displaySystemObject is SystemEconomiser)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.2, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.4, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.2));
                }

                return null;
            }

            if (displaySystemObject is SystemDamper)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.4, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.4, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.4));
                }

                return null;
            }

            if (displaySystemObject is SystemSpace)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemHeatingCoil ||
                displaySystemObject is SystemCoolingCoil ||
                displaySystemObject is SystemDXCoil)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.2, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.4, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.2));
                }

                return null;
            }

            if (displaySystemObject is SystemFan)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.6, 0));
                }

                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.4, 0));
                }

                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.6));
                }

                return null;
            }

            if (displaySystemObject is SystemAirJunction ||
                displaySystemObject is SystemLiquidJunction)
            {
                if (Vector2D.WorldX.AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    return null;
                }

                if (Vector2D.WorldX.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldY.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdRightLeft;
                    return Geometry.Planar.Transform2D.GetMirrorY(new Point2D(point2D.X - 0.1, point2D.Y - 0.1));
                }
                
                if (Vector2D.WorldY.AlmostEqual(axis_X) && Vector2D.WorldX.GetNegated().AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdBottomTop;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(0, 0.2));
                }
                
                if (Vector2D.WorldY.GetNegated().AlmostEqual(axis_X) && Vector2D.WorldX.AlmostEqual(axis_Y))
                {
                    tpdDirection = tpdDirection.tpdTopBottom;
                    return Geometry.Planar.Transform2D.GetTranslation(new Vector2D(-0.2, 0));
                }

                return null;
            }

            return null;
        }
    }
}