using System.Collections.Generic;
using System.Drawing;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static window ToT3D(this Aperture aperture, Building building)
        {
            if (aperture == null)
                return null;

            uint colour;
            if (!Core.Query.TryGetValue(aperture, "Colour", out colour))
                colour = Core.Convert.ToUint(Color.Black);

            double width = double.NaN;
            double height = double.NaN;
            double level = double.NaN;

            PlanarBoundary3D planarBoundary3D = aperture.PlanarBoundary3D;
            if(planarBoundary3D == null)
            {
                if (!Core.Query.TryGetValue(aperture, "Width", out width))
                    return null;

                if (!Core.Query.TryGetValue(aperture, "Height", out height))
                    return null;

                if (!Core.Query.TryGetValue(aperture, "Level", out level))
                    return null;
            }
            else
            {
                width = planarBoundary3D.Width();
                height = planarBoundary3D.Height();
                level = Analytical.Query.MinElevation(planarBoundary3D);
            }

            window result = building.AddWindow(aperture.Name, Query.OpeningType(aperture), colour, height, width, level);
            if (planarBoundary3D != null)
            {
                Geometry.Spatial.Face3D face3D = planarBoundary3D.GetFace3D();

                Geometry.Spatial.IClosedPlanar3D externalEdge3D = face3D?.GetExternalEdge3D();
                if(externalEdge3D != null)
                {
                    List<Geometry.Spatial.IClosedPlanar3D> internalEdge3Ds = face3D.GetInternalEdge3Ds();
                    if (internalEdge3Ds != null && internalEdge3Ds.Count != 0)
                    {
                        double area_External = externalEdge3D.GetArea();
                        double area_Internal = 0;

                        foreach(Geometry.Spatial.IClosedPlanar3D internalEdge3D in internalEdge3Ds)
                        {
                            if(internalEdge3D == null)
                            {
                                continue;
                            }

                            double area = internalEdge3D.GetArea();
                            if(double.IsNaN(area))
                            {
                                continue;
                            }

                            area_Internal += area;
                        }

                        result.isPercFrame = true;
                        result.framePerc = (area_External - area_Internal) / area_External;
                    }
                }
            }

            return result;
        }
    }
}
