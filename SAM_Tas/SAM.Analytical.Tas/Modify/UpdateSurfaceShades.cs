using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<TBD.SurfaceShade> UpdateSurfaceShades(this TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, AnalyticalModel analyticalModel,  Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult)
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

            Geometry.Spatial.Face3D face3D = panel.Face3D;
            if (face3D == null)
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

            foreach(DateTime dateTime in dateTimes)
            {
                double sunExposureArea = solarFaceSimulationResult.GetSunExposureArea(dateTime);
                if(double.IsNaN(sunExposureArea) || sunExposureArea == 0)
                {
                    continue;
                }

                int dayIndex = dateTime.DayOfYear;

                TBD.DaysShade daysShade = daysShades.Find(x => x.day == dayIndex);
                if(daysShade == null)
                {
                    daysShade = building.AddDaysShade();
                    daysShade.day = dayIndex;
                    daysShades.Add(daysShade);
                }

                TBD.SurfaceShade surfaceShade = daysShade.AddSurfaceShade(System.Convert.ToInt16(dateTime.Hour));
                surfaceShade.proportion = System.Convert.ToSingle(sunExposureArea / area);
                surfaceShade.surface = zoneSurface;

                result.Add(surfaceShade);
            }

            return result;
        }
    }
}