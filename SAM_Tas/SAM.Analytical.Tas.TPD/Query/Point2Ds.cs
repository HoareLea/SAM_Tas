using SAM.Geometry.Planar;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<Point2D> Point2Ds(Duct duct)
        {
            if (duct == null)
            {
                return null;
            }

            int nodeCount = duct.GetNodeCount();
            if (nodeCount < 1)
            {
                return null;
            }

            List<Point2D> result = new List<Point2D>();
            for (int i = 1; i <= nodeCount; i++)
            {
                result.Add(Convert.ToSAM(duct.GetNodePosX(i), duct.GetNodePosY(i)));
            }

            ComponentGroup componentGroup = duct.GetGroup();
            if (componentGroup != null)
            {
                Transform2D transform2D = null;

                Point2D location = ((TasPosition)(componentGroup as dynamic).GetPosition())?.ToSAM();
                if (location != null)
                {
                    transform2D = Geometry.Planar.Transform2D.GetTranslation(location.ToVector());
                }

                if(transform2D != null)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        result[i].Transform(transform2D);
                    }
                }
            }

            return result;
        }

        public static List<Point2D> Point2Ds(ControlArc controlArcs)
        {
            if (controlArcs == null)
            {
                return null;
            }

            int nodeCount = controlArcs.GetNodeCount();
            if (nodeCount < 1)
            {
                return null;
            }

            List<Point2D> result = new List<Point2D>();
            for (int i = 1; i <= nodeCount; i++)
            {
                result.Add(Convert.ToSAM(controlArcs.GetNodePosX(i), controlArcs.GetNodePosY(i)));
            }

            ComponentGroup componentGroup = controlArcs.GetGroup();
            if (componentGroup != null)
            {
                Transform2D transform2D = null;

                Point2D location = ((TasPosition)(componentGroup as dynamic).GetPosition())?.ToSAM();
                if (location != null)
                {
                    transform2D = Geometry.Planar.Transform2D.GetTranslation(location.ToVector());
                }

                if (transform2D != null)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        result[i].Transform(transform2D);
                    }
                }
            }

            return result;
        }
    }
}