using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Geometry.Tas
{
    public static partial class Convert
    {
        public static Face3D ToSAM(this TBD.Perimeter perimeter)
        {
            if(perimeter == null)
            {
                return null;
            }

            Polygon3D externalEdge3D = perimeter.GetFace()?.ToSAM();
            if(externalEdge3D == null)
            {
                return null;
            }

            Plane plane = externalEdge3D.GetPlane();

            List<Planar.IClosed2D> internalEdge2Ds = null;

            List<TBD.Polygon> holes = perimeter.Holes();
            if(holes != null)
            {
                internalEdge2Ds = new List<Planar.IClosed2D>();
                foreach(TBD.Polygon hole in holes)
                {
                    Polygon3D internalEdge3D = hole?.ToSAM(); 
                    if(internalEdge3D == null)
                    {
                        continue;
                    }

                    Planar.Polygon2D internalEdge2D = plane.Convert(internalEdge3D);

                    internalEdge2Ds.Add(internalEdge2D);
                }
            }

            Planar.Face2D face2D = Planar.Create.Face2D(plane.Convert(externalEdge3D), internalEdge2Ds);
            if(face2D == null)
            {
                return null;
            }

            return plane.Convert(face2D);
        }
    }
}
