using SAM.Geometry.Planar;
using SAM.Geometry.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool SetLocation(this IDisplaySystemObject<SystemGeometryInstance> displaySystemObject, global::TPD.SystemComponent systemComponent)
        { 
            if(displaySystemObject == null || systemComponent == null)
            {
                return false;
            }

            CoordinateSystem2D coordinateSystem2D = displaySystemObject.SystemGeometry?.CoordinateSystem2D;
            if (coordinateSystem2D == null)
            {
                return false;
            }

            Point2D point2D = coordinateSystem2D.Origin;

            ITransform2D transform2D = Query.Transform2D(displaySystemObject, out global::TPD.tpdDirection tpdDirection);
            if (transform2D != null)
            {
                point2D.Transform(transform2D);
            }

            point2D = point2D.ToTPD();

            dynamic dynamic = systemComponent;

            dynamic.SetPosition(point2D.X, point2D.Y);
            dynamic.SetDirection(tpdDirection);

            return true;
        }

        public static bool SetLocation(this IDisplaySystemObject<SystemGeometryInstance> displaySystemObject, global::TPD.Controller controller)
        {
            if (displaySystemObject == null || controller == null)
            {
                return false;
            }

            CoordinateSystem2D coordinateSystem2D = displaySystemObject.SystemGeometry?.CoordinateSystem2D;
            if (coordinateSystem2D == null)
            {
                return false;
            }

            Point2D point2D = coordinateSystem2D.Origin;

            ITransform2D transform2D = Query.Transform2D(displaySystemObject, out global::TPD.tpdDirection tpdDirection);
            if (transform2D != null)
            {
                point2D.Transform(transform2D);
            }

            point2D = point2D.ToTPD();

            dynamic dynamic = controller;

            dynamic.SetPosition(point2D.X, point2D.Y);

            return true;
        }

        public static bool SetLocation(this IDisplaySystemObject<SystemGeometryInstance> displaySystemObject, global::TPD.PlantController plantController)
        {
            if (displaySystemObject == null || plantController == null)
            {
                return false;
            }

            CoordinateSystem2D coordinateSystem2D = displaySystemObject.SystemGeometry?.CoordinateSystem2D;
            if (coordinateSystem2D == null)
            {
                return false;
            }

            Point2D point2D = coordinateSystem2D.Origin;

            ITransform2D transform2D = Query.Transform2D(displaySystemObject, out global::TPD.tpdDirection tpdDirection);
            if (transform2D != null)
            {
                point2D.Transform(transform2D);
            }

            point2D = point2D.ToTPD();

            dynamic dynamic = plantController;

            dynamic.SetPosition(point2D.X, point2D.Y);

            return true;
        }

        public static bool SetLocation(this IDisplaySystemObject<SystemGeometryInstance> displaySystemObject, global::TPD.PlantComponent plantComponent)
        {
            if (displaySystemObject == null || plantComponent == null)
            {
                return false;
            }

            CoordinateSystem2D coordinateSystem2D = displaySystemObject.SystemGeometry?.CoordinateSystem2D;
            if (coordinateSystem2D == null)
            {
                return false;
            }

            Point2D point2D = coordinateSystem2D.Origin;

            ITransform2D transform2D = Query.Transform2D(displaySystemObject, out global::TPD.tpdDirection tpdDirection);
            if (transform2D != null)
            {
                point2D.Transform(transform2D);
            }

            point2D = point2D.ToTPD();

            dynamic dynamic = plantComponent;

            dynamic.SetPosition(point2D.X, point2D.Y);
            dynamic.SetDirection(tpdDirection);

            return true;
        }
    }
}