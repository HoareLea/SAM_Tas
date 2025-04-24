using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<TBD.SurfaceShade> UpdateSurfaceShades(this TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, AnalyticalModel analyticalModel,  Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult, double tolerance = 0.01)
        {
            if (daysShades == null || analyticalModel == null || solarFaceSimulationResult == null || zoneSurface == null)
            {
                return null;
            }

            Panel panel = analyticalModel.GetRelatedObjects<Panel>(solarFaceSimulationResult)?.FirstOrDefault();
            if(panel == null)
            {
                return null;
            }

            Face3D face3D = panel.GetFace3D(true);
            if (face3D == null)
            {
                return null;
            }

            return UpdateSurfaceShades(building, daysShades, zoneSurface, face3D, solarFaceSimulationResult, tolerance);
        }

        public static List<TBD.SurfaceShade> UpdateSurfaceShades(this TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, AdjacencyCluster adjacencyCluster, Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult, double tolerance = 0.01)
        {
            if (daysShades == null || adjacencyCluster == null || solarFaceSimulationResult == null || zoneSurface == null)
            {
                return null;
            }

            Panel panel = adjacencyCluster.GetRelatedObjects<Panel>(solarFaceSimulationResult)?.FirstOrDefault();
            if (panel == null)
            {
                return null;
            }

            Face3D face3D = panel.GetFace3D(true);
            if (face3D == null)
            {
                return null;
            }

            return UpdateSurfaceShades(building, daysShades, zoneSurface, face3D, solarFaceSimulationResult, tolerance);
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

            List<TBD.SurfaceShade> result = new List<TBD.SurfaceShade>();

            List<DateTime> dateTimes = solarFaceSimulationResult.DateTimes;
            if (dateTimes == null || dateTimes.Count == 0)
            {
                return result;
            }

            List<Tuple<int, short, float>> tuples = Enumerable.Repeat<Tuple<int, short, float>>(null, dateTimes.Count).ToList();
            //Parallel.For(0, dateTimes.Count, (int i) =>   
            //when using parallel for, value where missing in the list in Tas
            for (int i =0; i < dateTimes.Count; i++)
            {
                List<Face3D> face3Ds = Geometry.SolarCalculator.Query.SunExposureFace3Ds(solarFaceSimulationResult, face3D, dateTimes[i]); //TODO Optimze this function! https://github.com/HoareLea/SAM_Tas/issues/72 
                float proportion = 0;
                if (face3Ds != null && face3Ds.Count != 0)
                {
                    double area_Temp = face3Ds.ConvertAll(x => x.GetArea()).Sum();
                    proportion = System.Convert.ToSingle(area_Temp / area);
                }

                if (proportion <= tolerance)
                {
                    proportion = 0;
                }

                tuples.Add(new Tuple<int, short, float>(dateTimes[i].DayOfYear, System.Convert.ToInt16(dateTimes[i].Hour), proportion));
            };//);

            Dictionary<int, Dictionary<short, float>> dictionary = new Dictionary<int, Dictionary<short, float>>();
            foreach (Tuple<int, short, float> tuple in tuples)
            {
                if(tuple == null)
                {
                    continue;
                }

                if(!dictionary.TryGetValue(tuple.Item1, out Dictionary<short, float> dictionary_Temp) || dictionary_Temp == null)
                {
                    dictionary_Temp = new Dictionary<short, float>();
                    dictionary[tuple.Item1] = dictionary_Temp;
                }

                dictionary_Temp[tuple.Item2] = tuple.Item3;
            }

            foreach (KeyValuePair<int, Dictionary<short, float>> keyValuePair in dictionary)
            {
                TBD.DaysShade daysShade = daysShades.Find(x => x.day == keyValuePair.Key);
                if (daysShade == null)
                {
                    daysShade = building.AddDaysShade();

                    daysShade.day = keyValuePair.Key;
                    
                    daysShades.Add(daysShade);
                }

                foreach (KeyValuePair<short, float> keyValuePair_Temp in keyValuePair.Value)
                {
                    TBD.SurfaceShade surfaceShade = daysShade.AddSurfaceShade(keyValuePair_Temp.Key);

                    surfaceShade.proportion = keyValuePair_Temp.Value;
                    surfaceShade.surface = zoneSurface;

                    result.Add(surfaceShade);
                }
            }

            return result;
        }

        public static List<TBD.SurfaceShade> UpdateSurfaceShades(this TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult, double tolerance = 0.01)
        {
            if (daysShades == null || solarFaceSimulationResult == null || zoneSurface == null)
            {
                return null;
            }

            List<DateTime> dateTimes = solarFaceSimulationResult.DateTimes;
            if (dateTimes == null || dateTimes.Count == 0)
            {
                return null;
            }

            List<TBD.IRoomSurface> roomSurfaces = zoneSurface.RoomSurfaces();
            if (roomSurfaces == null)
            {
                return null;
            }

            List<Face3D> face3Ds = new List<Face3D>();
            double area = 0;
            foreach (TBD.IRoomSurface roomSurface in roomSurfaces)
            {
                Face3D face3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter());
                if (face3D == null || !face3D.IsValid())
                {
                    continue;
                }

                double area_Temp = face3D.GetArea();
                if(area_Temp < tolerance)
                {
                    continue;
                }

                face3Ds.Add(face3D);
                area += area_Temp;
            }

            if(area < tolerance)
            {
                return null;
            }

            List<TBD.SurfaceShade> result = new List<TBD.SurfaceShade>();

            foreach (DateTime dateTime in dateTimes)
            {
                List<Face3D> sunExposureFace3Ds = solarFaceSimulationResult.GetSunExposureFace3Ds(dateTime);
                if (sunExposureFace3Ds == null || sunExposureFace3Ds.Count == 0)
                {
                    continue;
                }

                int dayIndex = dateTime.DayOfYear;

                TBD.DaysShade daysShade = daysShades.Find(x => x.day == dayIndex);
                if (daysShade == null)
                {
                    daysShade = building.AddDaysShade();
                    daysShade.day = dayIndex;
                    daysShades.Add(daysShade);
                }

                Plane plane = sunExposureFace3Ds[0].GetPlane();
                List<Geometry.Planar.Face2D> sunExposureFace2Ds = sunExposureFace3Ds.ConvertAll(x => plane.Convert(x));
                List<Geometry.Planar.Face2D> face2Ds = face3Ds.ConvertAll(x => plane.Convert(x));

                double sunExposureArea = 0;
                foreach (Geometry.Planar.Face2D sunExposureface2D in sunExposureFace2Ds)
                {
                    if(sunExposureface2D == null)
                    {
                        continue;
                    }

                    foreach(Geometry.Planar.Face2D face2D in face2Ds)
                    {
                        if(face2D == null)
                        {
                            continue;
                        }

                        List<Geometry.Planar.Face2D> face2Ds_Intersection = Geometry.Planar.Query.Intersection(sunExposureface2D, face2D);
                        if(face2Ds_Intersection == null || face2Ds_Intersection.Count == 0)
                        {
                            continue;
                        }

                        sunExposureArea += face2Ds_Intersection.ConvertAll(x => x.GetArea()).Sum();
                    }
                }

                float proportion = System.Convert.ToSingle(Core.Query.Round(sunExposureArea / area, tolerance));
                if (proportion <= tolerance)
                {
                    proportion = 0;
                }

                TBD.SurfaceShade surfaceShade = daysShade.AddSurfaceShade(System.Convert.ToInt16(dateTime.Hour - 1));
                surfaceShade.proportion = proportion;  //TODO: TAS MEETING: Discuss why Tas returns value below 1
                surfaceShade.surface = zoneSurface;

                result.Add(surfaceShade);
            }

            return result;
        }
    }
}