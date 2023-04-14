using SAM.Core.Tas;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateAdiabatic(this string path_TBD, AnalyticalModel analyticalModel, double tolerance = Core.Tolerance.Distance)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateAdiabatic(sAMTBDDocument, analyticalModel, tolerance);
                if (result)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static bool UpdateAdiabatic(this SAMTBDDocument sAMTBDDocument, AnalyticalModel analyticalModel, double tolerance = Core.Tolerance.Distance)
        {
            if (sAMTBDDocument == null)
                return false;

            return UpdateAdiabatic(sAMTBDDocument.TBDDocument, analyticalModel, tolerance);
        }

        public static bool UpdateAdiabatic(this TBDDocument tBDDocument, AnalyticalModel analyticalModel, double tolerance = Core.Tolerance.Distance)
        {
            if (tBDDocument == null || analyticalModel == null)
            {
                return false;
            }
                
            Building building = tBDDocument.Building;
            if (building == null)
            {
                return false;
            }
                
            List<zone> zones = building.Zones();
            if(zones == null)
            {
                return false;
            }

            List<Panel> panels = analyticalModel.AdjacencyCluster?.GetPanels();
            if(panels == null || panels.Count == 0)
            {
                return false;
            }

            BoundingBox3D boundingBox3D = new BoundingBox3D(panels.ConvertAll(x => x.GetBoundingBox()));
            Point3D point3D = boundingBox3D.GetCentroid();
            point3D = new Point3D(point3D.X, point3D.Y, 0);//change without height

            Vector3D vector3D = new Vector3D(point3D, Point3D.Zero); 


            for (int i = panels.Count - 1; i >= 0; i--)
            {
                if(!Analytical.Query.Adiabatic(panels[i]))
                {
                    panels.RemoveAt(i);
                }
            }

            List<Tuple<BoundingBox3D, Face3D>> tuples = new List<Tuple<BoundingBox3D, Face3D>>();
            foreach(Panel panel in panels)
            {
                Face3D face3D = panel?.GetFace3D(false);
                if(face3D == null)
                {
                    continue;
                }

                face3D = face3D.GetMoved(vector3D) as Face3D;

                tuples.Add(new Tuple<BoundingBox3D, Face3D>(face3D.GetBoundingBox(), face3D));
            }

            if(tuples == null || tuples.Count == 0)
            {
                return true;
            }
            
            foreach(zone zone in zones)
            {
                List<IZoneSurface> zoneSurfaces = zone?.ZoneSurfaces();
                if(zoneSurfaces == null || zoneSurfaces.Count == 0)
                {
                    continue;
                }

                foreach(IZoneSurface zoneSurface in zoneSurfaces)
                {
                    List<IRoomSurface> roomSurfaces = zoneSurface?.RoomSurfaces();
                    if(roomSurfaces == null || roomSurfaces.Count == 0)
                    {
                        continue;
                    }

                    foreach(IRoomSurface roomSurface in roomSurfaces)
                    {
                        Face3D face3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter());
                        if(face3D == null)
                        {
                            continue;
                        }

                        Point3D point3D_Temp = face3D.InternalPoint3D();
                        if (point3D == null)
                        {
                            continue;
                        }

                        BoundingBox3D boundingBox3D_Temp = face3D.GetBoundingBox();

                        List<Tuple<BoundingBox3D, Face3D>> tuples_Temp = tuples.FindAll(x => x.Item1.InRange(boundingBox3D_Temp, tolerance) && x.Item1.InRange(point3D_Temp, tolerance));
                        if(tuples_Temp == null || tuples_Temp.Count == 0)
                        {
                            continue;
                        }

                        Tuple<BoundingBox3D, Face3D> tuple = tuples_Temp.Find(x => x.Item2.InRange(point3D_Temp, tolerance));
                        if(tuple == null)
                        {
                            continue;
                        }

                        zoneSurface.type = SurfaceType.tbdNullLink;
                        break;
                    }
                }
            }


            return true;
        }
    }
}