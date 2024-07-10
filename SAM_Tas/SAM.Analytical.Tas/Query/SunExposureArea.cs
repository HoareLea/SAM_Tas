using SAM.Geometry.Spatial;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        internal static double SunExposureArea(List<Face3D> sunExposureFace3Ds, List<Face3D> face3Ds)
        {
            double sunExposureArea = 0;
            var plane = sunExposureFace3Ds[0].GetPlane();
            var sunExposureFace2Ds = sunExposureFace3Ds.Select(x => plane.Convert(x)).ToList();
            var face2Ds = face3Ds.Select(x => plane.Convert(x)).ToList();

            foreach (var sunExposureFace2D in sunExposureFace2Ds)
            {
                foreach (var face2D in face2Ds)
                {
                    var intersections = Geometry.Planar.Query.Intersection(sunExposureFace2D, face2D);
                    if (intersections != null)
                    {
                        sunExposureArea += intersections.Sum(x => x.GetArea());
                    }
                }
            }

            return sunExposureArea;
        }

    }
}