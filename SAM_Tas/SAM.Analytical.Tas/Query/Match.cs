using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Space Match(this TAS3D.Zone zone, IEnumerable<Space> spaces)
        {
            if (spaces == null || zone == null)
                return null;

            foreach (Space space in spaces)
            {
                if (zone.name.Equals(space.Name))
                    return space;
            }

            return null;
        }

        public static Space Match(this Core.Tas.UKBR.Zone zone, IEnumerable<Space> spaces)
        {
            if(zone == null || spaces == null)
            {
                return null;
            }

            foreach(Space space in spaces)
            {
                if(space == null)
                {
                    continue;
                }

                if(!space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) || string.IsNullOrWhiteSpace(zoneGuid))
                {
                    continue;
                }

                if(!global::System.Guid.TryParse(zoneGuid, out System.Guid guid))
                {
                    continue;
                }

                if(zone.GUID == guid)
                {
                    return space;
                }
            }

            foreach (Space space in spaces)
            {
                if(space?.Name == zone.Name)
                {
                    return space;
                }
            }

            return null;
        }

        public static TBD.zone Match(this IEnumerable<TBD.zone> zones, string name, bool caseSensitive = true, bool trim = false)
        {
            if (zones == null || zones.Count() == 0)
                return null;

            if (string.IsNullOrWhiteSpace(name))
                return null;

            string name_Temp = name;

            if (trim)
                name_Temp = name_Temp.Trim();

            if (!caseSensitive)
                name_Temp = name_Temp.ToUpper();

            foreach (TBD.zone zone in zones)
            {
                string name_Zone = zone?.name;
                if (string.IsNullOrWhiteSpace(name_Zone))
                    continue;

                if (trim)
                    name_Zone = name_Zone.Trim();

                if (!caseSensitive)
                    name_Zone = name_Zone.ToUpper();

                if (name_Zone.Equals(name_Temp))
                    return zone;
            }

            return null;
        }

        public static TBD.zone Match(this Space space, IEnumerable<TBD.zone> zones)
        {
            if (space == null || zones == null)
            {
                return null;
            }

            TBD.zone result = null;
            if (space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) && !string.IsNullOrWhiteSpace(zoneGuid))
            {
                result = zones.ToList().Find(x => x.GUID == zoneGuid);
            }

            if (result == null)
            {
                result = zones.ToList().Find(x => x.name == space.Name);
            }

            return result;
        }

        public static Panel Match(this TBD.IZoneSurface zoneSurface, List<Panel> panels, double tolerance = Core.Tolerance.MacroDistance)
        {
            if (zoneSurface == null || panels == null  || panels.Count == 0)
            {
                return null;
            }

            foreach (Panel panel in panels)
            {
                if (panel == null)
                {
                    continue;
                }

                if (panel.TryGetValue(PanelParameter.ZoneSurfaceGuid, out string zoneSurfaceGuid) && !string.IsNullOrWhiteSpace(zoneSurfaceGuid))
                {
                    if (zoneSurface.GUID == zoneSurfaceGuid)
                    {
                        return panel;
                    }
                }
            }

            List<TBD.IRoomSurface> roomSurfaces = zoneSurface.RoomSurfaces();
            if(roomSurfaces == null || roomSurfaces.Count == 0)
            {
                return null;
            }

            foreach(TBD.IRoomSurface roomSurface in roomSurfaces)
            {
                Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
                if (polygon3D == null)
                {
                    continue;
                }

                Point3D point3D = polygon3D.InternalPoint3D();
                if(point3D == null)
                {
                    continue;
                }

                foreach(Panel panel in panels)
                {
                    Face3D face3D = panel?.GetFace3D(false);
                    if(face3D == null)
                    {
                        continue;
                    }

                    BoundingBox3D boundingBox3D = panel.GetBoundingBox();
                    if(boundingBox3D == null)
                    {
                        continue;
                    }

                    if(boundingBox3D.InRange(boundingBox3D, tolerance))
                    {
                        if (face3D.InRange(point3D, tolerance))
                        {
                            return panel;
                        }
                    }
                }
            }

            return null;
        }

        public static Aperture Match(this TBD.IZoneSurface zoneSurface, List<Aperture> apertures, double tolerance = Core.Tolerance.MacroDistance)
        {
            if (zoneSurface == null || apertures == null || apertures.Count == 0)
            {
                return null;
            }

            TBD.buildingElement buildingElement = zoneSurface.buildingElement;

            ApertureType apertureType = ApertureType(buildingElement.BEType);
            if (apertureType == Analytical.ApertureType.Undefined)
            {
                return null;
            }

            AperturePart aperturePart = AperturePart(buildingElement.BEType);
            if (aperturePart == Analytical.AperturePart.Undefined)
            {
                return null;
            }

            Enum @enum = aperturePart == Analytical.AperturePart.Frame ? ApertureParameter.FrameZoneSurfaceGuid : ApertureParameter.PaneZoneSurfaceGuid;


            foreach (Aperture aperture in apertures)
            {
                if (aperture == null)
                {
                    continue;
                }

                if (aperture.TryGetValue(@enum, out string zoneSurfaceGuid) && !string.IsNullOrWhiteSpace(zoneSurfaceGuid))
                {
                    if (zoneSurface.GUID == zoneSurfaceGuid)
                    {
                        return aperture;
                    }
                }
            }

            List<TBD.IRoomSurface> roomSurfaces = zoneSurface.RoomSurfaces();
            if (roomSurfaces == null || roomSurfaces.Count == 0)
            {
                return null;
            }

            foreach (TBD.IRoomSurface roomSurface in roomSurfaces)
            {
                Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
                if (polygon3D == null)
                {
                    continue;
                }

                Point3D point3D = polygon3D.InternalPoint3D();
                if (point3D == null)
                {
                    continue;
                }

                foreach (Aperture aperture in apertures)
                {
                    List<Face3D> face3Ds_AperturePart = aperture?.GetFace3Ds(aperturePart);
                    if(face3Ds_AperturePart != null)
                    {
                        foreach(Face3D face3D_AperturePart in face3Ds_AperturePart)
                        {
                            BoundingBox3D boundingBox3D = face3D_AperturePart.GetBoundingBox();
                            if (boundingBox3D == null)
                            {
                                continue;
                            }

                            if (boundingBox3D.InRange(boundingBox3D, tolerance))
                            {
                                if (face3D_AperturePart.InRange(point3D, tolerance))
                                {
                                    return aperture;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }


        public static Construction Match(this TAS3D.Element element, IEnumerable<Construction> constructions)
        {
            if (constructions == null || element == null)
                return null;

            string name = Name(element);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            List<Construction> constructions_Temp = constructions.ToList();
            constructions_Temp.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.Name));

            foreach (Construction construction in constructions_Temp)
            {
                if (name.Equals(construction.Name.Trim()))
                    return construction;
            }

            foreach(Construction construction in constructions_Temp)
            {
                if (name.EndsWith(string.Format(": {0}", construction.Name.Trim())))
                    return construction;
            }

            if (UniqueNameDecomposition(element.name, out string prefix, out name, out System.Guid? guid, out int id))
            {
                foreach (Construction construction in constructions_Temp)
                {
                    if (name.Equals(construction.Name.Trim()))
                        return construction;
                }
            }

            return null;
        }

        public static ApertureConstruction Match(this TAS3D.window window, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if (apertureConstructions == null || window == null)
                return null;

            string name = Name(window);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            List<ApertureConstruction> apertureConstructions_Temp = apertureConstructions.ToList();
            apertureConstructions_Temp.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.Name));

            foreach (ApertureConstruction apertureConstruction in apertureConstructions_Temp)
            {
                if (name.Equals(apertureConstruction.Name.Trim()))
                    return apertureConstruction;
            }

            foreach (ApertureConstruction apertureConstruction in apertureConstructions_Temp)
            {
                if (name.EndsWith(string.Format(": {0}", apertureConstruction.Name.Trim())))
                    return apertureConstruction;
            }

            if (UniqueNameDecomposition(window.name, out string prefix, out name, out System.Guid? guid, out int id))
            {
                foreach (ApertureConstruction apertureConstruction in apertureConstructions_Temp)
                {
                    if (name.Equals(apertureConstruction.Name.Trim()))
                        return apertureConstruction;
                }
            }

            return null;
        }
    }
}