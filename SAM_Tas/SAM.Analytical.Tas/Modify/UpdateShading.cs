using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using SAM.Geometry.Spatial;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateShading(string path_TBD, AnalyticalModel analyticalModel, double tolerance = Core.Tolerance.Distance)
        {
            if(analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD))
            {
                return false;
            }

            bool result = false;
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateShading(sAMTBDDocument?.TBDDocument, analyticalModel, tolerance);
                if (result)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static bool UpdateShading(this TBD.TBDDocument tBDDocument, AnalyticalModel analyticalModel, double tolerance = Core.Tolerance.Distance)
        {
            if(tBDDocument == null || analyticalModel == null)
            {
                return false;
            }

            return UpdateShading(tBDDocument?.Building, analyticalModel, tolerance);
        }

        public static bool UpdateShading(this TBD.Building building, AnalyticalModel analyticalModel, double tolerance = Core.Tolerance.Distance)
        {
            if(building == null || analyticalModel == null)
            {
                return false;
            }

            List<TBD.zone> zones = building.Zones();
            if (zones == null)
            {
                return false;
            }

            List<Tuple<Face3D, BoundingBox3D, TBD.IZoneSurface>> tuples_ZoneSurfaces = new List<Tuple<Face3D, BoundingBox3D, TBD.IZoneSurface>>();
            foreach (TBD.zone zone in zones)
            {
                List<TBD.IZoneSurface> zoneSurfaces_Zone =  zone?.ZoneSurfaces();
                if(zoneSurfaces_Zone == null)
                {
                    continue;
                }

                foreach(TBD.IZoneSurface zoneSurface in zoneSurfaces_Zone)
                {
                    List<TBD.IRoomSurface> roomSurfaces = zoneSurface.RoomSurfaces();
                    if(roomSurfaces == null)
                    {
                        continue;
                    }

                    foreach(TBD.IRoomSurface roomSurface in roomSurfaces)
                    {
                        Face3D face3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter());
                        if(face3D == null || !face3D.IsValid())
                        {
                            continue;
                        }

                        tuples_ZoneSurfaces.Add(new Tuple<Face3D, BoundingBox3D, TBD.IZoneSurface>(face3D, face3D.GetBoundingBox(), zoneSurface));
                    }
                }
            }

            if(tuples_ZoneSurfaces == null || tuples_ZoneSurfaces.Count == 0)
            {
                return false;
            }

            List<Panel> panels = analyticalModel.GetPanels();
            if(panels == null)
            {
                return false;
            }

            List<Tuple<Face3D, Point3D, BoundingBox3D, Geometry.SolarCalculator.SolarFaceSimulationResult>> tuples_solarFaceSimulationResult = new List<Tuple<Face3D, Point3D, BoundingBox3D, Geometry.SolarCalculator.SolarFaceSimulationResult>>();
            foreach(Panel panel in panels)
            {
                List<Geometry.SolarCalculator.SolarFaceSimulationResult> solarFaceSimulationResults = analyticalModel.GetResults<Geometry.SolarCalculator.SolarFaceSimulationResult>(panel);
                if(solarFaceSimulationResults == null || solarFaceSimulationResults.Count == 0)
                {
                    continue;
                }

                Face3D face3D = panel.Face3D;
                if(face3D == null || !face3D.IsValid())
                {
                    continue;
                }

                BoundingBox3D boundingBox3D = face3D.GetBoundingBox();
                Point3D point3D = face3D.GetInternalPoint3D(tolerance);

                foreach(Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult in solarFaceSimulationResults)
                {
                    tuples_solarFaceSimulationResult.Add(new Tuple<Face3D, Point3D, BoundingBox3D, Geometry.SolarCalculator.SolarFaceSimulationResult>(face3D, point3D, boundingBox3D, solarFaceSimulationResult));
                }
            }

            building.ClearShadingData();

            List<TBD.DaysShade> daysShades = new List<TBD.DaysShade>();
            foreach(Tuple<Face3D, Point3D, BoundingBox3D, Geometry.SolarCalculator.SolarFaceSimulationResult> tuple in tuples_solarFaceSimulationResult)
            {
                List<Tuple<Face3D, BoundingBox3D, TBD.IZoneSurface>> tuples_Temp = tuples_ZoneSurfaces.FindAll(x => tuple.Item3.InRange(x.Item2, tolerance));
                tuples_Temp = tuples_Temp.FindAll(x => x.Item1.On(tuple.Item2, tolerance));

                if(tuples_Temp == null || tuples_Temp.Count == 0)
                {
                    continue;
                }

                foreach(Tuple<Face3D, BoundingBox3D, TBD.IZoneSurface> tuple_ZoneSurface in tuples_Temp)
                {
                    UpdateSurfaceShades(building, daysShades, (TBD.zoneSurface)tuple_ZoneSurface.Item3, analyticalModel, tuple.Item4);
                }
            }

            return true;
        }


        public static bool UpdateShading(this AnalyticalModel analyticalModel, TBD.Building building, double tolerance = Core.Tolerance.Distance)
        {
            if (building == null || analyticalModel == null)
            {
                return false;
            }

            List<TBD.zone> zones = building.Zones();
            if (zones == null)
            {
                return false;
            }

            List<Tuple<Face3D, BoundingBox3D, TBD.IZoneSurface, int, int>> tuples_ZoneSurfaces = new List<Tuple<Face3D, BoundingBox3D, TBD.IZoneSurface, int, int>>();
            
            int index_Zone = 0;
            foreach (TBD.zone zone in zones)
            {
                List<TBD.IZoneSurface> zoneSurfaces_Zone = zone?.ZoneSurfaces();
                if (zoneSurfaces_Zone == null)
                {
                    continue;
                }

                int index_Surface = 0;
                foreach (TBD.IZoneSurface zoneSurface in zoneSurfaces_Zone)
                {
                    List<TBD.IRoomSurface> roomSurfaces = zoneSurface.RoomSurfaces();
                    if (roomSurfaces == null)
                    {
                        continue;
                    }

                    foreach (TBD.IRoomSurface roomSurface in roomSurfaces)
                    {
                        Face3D face3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter());
                        if (face3D == null || !face3D.IsValid())
                        {
                            continue;
                        }

                        tuples_ZoneSurfaces.Add(new Tuple<Face3D, BoundingBox3D, TBD.IZoneSurface, int, int>(face3D, face3D.GetBoundingBox(), zoneSurface, index_Zone, index_Surface));
                    }

                    index_Surface++;
                }
                index_Zone++;
            }

            if (tuples_ZoneSurfaces == null || tuples_ZoneSurfaces.Count == 0)
            {
                return false;
            }

            List<Panel> panels = analyticalModel.GetPanels();
            if (panels == null)
            {
                return false;
            }

            List<Tuple<Face3D, Point3D, BoundingBox3D, Panel>> tuples_Panel = new List<Tuple<Face3D, Point3D, BoundingBox3D, Panel>>();
            foreach (Panel panel in panels)
            {
                Face3D face3D = panel.Face3D;
                if (face3D == null || !face3D.IsValid())
                {
                    continue;
                }

                BoundingBox3D boundingBox3D = face3D.GetBoundingBox();
                Point3D point3D = face3D.GetInternalPoint3D(tolerance);

            }

            throw new NotImplementedException();
        }
  }
}