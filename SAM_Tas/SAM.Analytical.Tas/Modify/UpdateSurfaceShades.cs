using SAM.Geometry.Spatial;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<TBD.SurfaceShade> UpdateSurfaceShades(this TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, AnalyticalModel analyticalModel, Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult, double tolerance = 0.01)
        {
            if (daysShades == null || analyticalModel == null || solarFaceSimulationResult == null || zoneSurface == null)
            {
                return null;
            }

            var panel = analyticalModel.GetRelatedObjects<Panel>(solarFaceSimulationResult)?.FirstOrDefault();

            return panel == null ? null : UpdateSurfaceShades(building, daysShades, zoneSurface, panel.GetFace3D(true), solarFaceSimulationResult, tolerance);
        }

        public static List<TBD.SurfaceShade> UpdateSurfaceShades(this TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, AdjacencyCluster adjacencyCluster, Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult, double tolerance = 0.01)
        {
            if (daysShades == null || adjacencyCluster == null || solarFaceSimulationResult == null || zoneSurface == null)
            {
                return null;
            }

            var panel = adjacencyCluster.GetRelatedObjects<Panel>(solarFaceSimulationResult)?.FirstOrDefault();
            
            return panel == null ? null : UpdateSurfaceShades(building, daysShades, zoneSurface, panel.GetFace3D(true), solarFaceSimulationResult, tolerance);
        }

        public static List<TBD.SurfaceShade> UpdateSurfaceShades(this TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, Face3D face3D, Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult, double tolerance = 0.01)
        {
            if (daysShades == null || face3D == null || solarFaceSimulationResult == null || zoneSurface == null)
            {
                return null;
            }

            double area = face3D.GetArea();
            if (double.IsNaN(area) || area == 0)
            {
                return null;
            }

            var dateTimes = solarFaceSimulationResult.DateTimes;
            if (dateTimes == null || dateTimes.Count == 0)
            {
                return new List<TBD.SurfaceShade>();
            }

            var tuples = new ConcurrentBag<Tuple<int, short, float>>();

            Parallel.ForEach(dateTimes, dateTime =>
            {
                var face3Ds = Geometry.SolarCalculator.Query.SunExposureFace3Ds(solarFaceSimulationResult, face3D, dateTime);
                float proportion = 0;
                if (face3Ds != null && face3Ds.Count > 0)
                {
                    double areaTemp = face3Ds.Sum(x => x.GetArea());
                    proportion = (float)(areaTemp / area);
                }

                if (proportion <= tolerance)
                {
                    proportion = 0;
                }

                tuples.Add(new Tuple<int, short, float>(dateTime.DayOfYear, (short)dateTime.Hour, proportion));
            });

            return Create.SurfaceShades(building, daysShades, zoneSurface, tuples);
        }

        public static List<TBD.SurfaceShade> UpdateSurfaceShades(this TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult, double tolerance = 0.01)
        {
            if (daysShades == null || solarFaceSimulationResult == null || zoneSurface == null)
            {
                return null;
            }

            var dateTimes = solarFaceSimulationResult.DateTimes;
            if (dateTimes == null || dateTimes.Count == 0)
            {
                return null;
            }

            var roomSurfaces = zoneSurface.RoomSurfaces();
            if (roomSurfaces == null)
            {
                return null;
            }

            var face3Ds = new List<Face3D>();
            double totalArea = 0;
            foreach (var roomSurface in roomSurfaces)
            {
                var face3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter());
                if (face3D == null || !face3D.IsValid())
                {
                    continue;
                }

                double area = face3D.GetArea();
                if (area < tolerance)
                {
                    continue;
                }

                face3Ds.Add(face3D);
                totalArea += area;
            }

            if (totalArea < tolerance)
            {
                return null;
            }

            var result = new List<TBD.SurfaceShade>();
            foreach (var dateTime in dateTimes)
            {
                var sunExposureFace3Ds = solarFaceSimulationResult.GetSunExposureFace3Ds(dateTime);
                if (sunExposureFace3Ds == null || sunExposureFace3Ds.Count == 0)
                {
                    continue;
                }

                int dayIndex = dateTime.DayOfYear;
                var daysShade = daysShades.FirstOrDefault(x => x.day == dayIndex) ?? building.AddDaysShade();
                daysShade.day = dayIndex;
                if (!daysShades.Contains(daysShade))
                {
                    daysShades.Add(daysShade);
                }

                double sunExposureArea = Query.SunExposureArea(sunExposureFace3Ds, face3Ds);
                float proportion = (float)Math.Round(sunExposureArea / totalArea, 2);
                if (proportion <= tolerance)
                {
                    proportion = 0;
                }

                var surfaceShade = daysShade.AddSurfaceShade((short)(dateTime.Hour - 1));
                surfaceShade.proportion = proportion;
                surfaceShade.surface = zoneSurface;
                result.Add(surfaceShade);
            }

            return result;
        }
    }
}
