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

        public static Construction Match(this TAS3D.Element element, IEnumerable<Construction> constructions)
        {
            if (constructions == null || element == null)
                return null;

            string name = Query.Name(element);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            List<Construction> constructions_Temp = constructions.ToList();
            constructions_Temp.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.Name));

            foreach (Construction construction in constructions_Temp)
            {
                if (name.Equals(construction.Name))
                    return construction;
            }

            foreach(Construction construction in constructions_Temp)
            {
                if (name.EndsWith(string.Format(": {0}", construction.Name)))
                    return construction;
            }

            return null;
        }
    }
}