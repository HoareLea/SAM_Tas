using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Geometry.Tas
{
    public static partial class Convert
    {
        public static TBD.Perimeter ToTBD(this Face3D face3D, TBD.RoomSurface roomSurface = null)
        {
            if(face3D == null)
            {
                return null;
            }

            IClosedPlanar3D externalEdge3D = face3D.GetExternalEdge3D();
            if(externalEdge3D == null)
            {
                return null;
            }

            ISegmentable3D segmentable3D = externalEdge3D as ISegmentable3D;
            if(segmentable3D == null)
            {
                throw new System.NotImplementedException();
            }

            TBD.Perimeter result = roomSurface?.CreatePerimeter();
            if(result == null)
            {
                result = new TBD.Perimeter();
            }

            TBD.Polygon polygon = result.CreateFace();

            List<Point3D> point3Ds = segmentable3D.GetPoints();
            if (point3Ds != null)
            {
                foreach (Point3D point3D in point3Ds)
                {
                    if (point3D == null)
                    {
                        continue;
                    }

                    polygon.AddCoordinate(System.Convert.ToSingle(point3D.X), System.Convert.ToSingle(point3D.Y), System.Convert.ToSingle(point3D.Z));
                }
            }

            List<IClosedPlanar3D> internalEdge3Ds = face3D.GetInternalEdge3Ds();
            if(internalEdge3Ds != null && internalEdge3Ds.Count != 0)
            {
                foreach(IClosedPlanar3D closedPlanar3D in internalEdge3Ds)
                {
                    if(closedPlanar3D == null)
                    {
                        continue;
                    }

                    segmentable3D = closedPlanar3D as ISegmentable3D;
                    if (segmentable3D == null)
                    {
                        throw new System.NotImplementedException();
                    }

                    polygon = result.AddHole();

                    point3Ds = segmentable3D.GetPoints();
                    if (point3Ds != null)
                    {
                        foreach (Point3D point3D in point3Ds)
                        {
                            if (point3D == null)
                            {
                                continue;
                            }

                            polygon.AddCoordinate(System.Convert.ToSingle(point3D.X), System.Convert.ToSingle(point3D.Y), System.Convert.ToSingle(point3D.Z));
                        }
                    }
                }
            }

            return result;
        }

        public static TBD.Perimeter ToTBD_Perimeter(this Polygon3D polygon3D)
        {
            if (polygon3D == null)
            {
                return null;
            }

            TBD.Perimeter result = new TBD.Perimeter();
            TBD.Polygon polygon = result.CreateFace();

            List<Point3D> point3Ds = polygon3D.GetPoints();
            if(point3Ds != null)
            {
                foreach (Point3D point3D in point3Ds)
                {
                    if(point3D == null)
                    {
                        continue;
                    }

                    polygon.AddCoordinate(System.Convert.ToSingle(point3D.X), System.Convert.ToSingle(point3D.Y), System.Convert.ToSingle(point3D.Z));
                }
            }



            return result;
        }
    }
}
