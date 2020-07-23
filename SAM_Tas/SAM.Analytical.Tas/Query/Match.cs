using System.Collections.Generic;

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

        public static Construction Match(this TAS3D.Element element, IEnumerable<Construction> constructions)
        {
            if (constructions == null || element == null)
                return null;

            string name = Query.Name(element);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            foreach (Construction construction in constructions)
            {
                if (construction == null)
                    continue;

                if (name.Equals(construction.Name))
                    return construction;
            }

            return null;
        }
    }
}