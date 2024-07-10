using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        internal static List<TBD.SurfaceShade> SurfaceShades(TBD.Building building, List<TBD.DaysShade> daysShades, TBD.zoneSurface zoneSurface, IEnumerable<Tuple<int, short, float>> tuples)
        {
            var result = new List<TBD.SurfaceShade>();

            foreach (var tuple in tuples)
            {
                if (tuple == null) continue;

                var daysShade = daysShades.FirstOrDefault(x => x.day == tuple.Item1) ?? building.AddDaysShade();
                if (daysShade.day != tuple.Item1)
                {
                    daysShade.day = tuple.Item1;
                    daysShades.Add(daysShade);
                }

                var surfaceShade = daysShade.AddSurfaceShade(tuple.Item2);
                surfaceShade.proportion = tuple.Item3;
                surfaceShade.surface = zoneSurface;

                result.Add(surfaceShade);
            }

            return result;
        }
    }
}