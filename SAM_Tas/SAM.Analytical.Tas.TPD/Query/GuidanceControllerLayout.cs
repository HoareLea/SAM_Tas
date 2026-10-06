// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        //TAS schematic units. The band sits one lane below the unit; controllers are a lane apart.
        private const int GuidanceControllerBandGap = 40;
        private const int GuidanceControllerGap = 40;
        private const int GuidanceControllerBandStep = 20;
        private const int GuidanceControllerBandSteps = 50;

        /// <summary>
        /// Where the manufacturer-guidance controllers are drawn: one row in a band <b>below</b> the unit, each
        /// controller centred beneath the component it controls, and no two controllers - and no controller and
        /// drawn component - on top of each other.
        /// <para>
        /// <b>Presentation only</b> (a position is never read by the conversion, the reconciliation or the
        /// simulation - see <see cref="VentilationLayout"/>). Before 2026-09-29 the three controllers were never
        /// given a position, so TAS drew all three at its default, on top of each other.
        /// </para>
        /// <para>
        /// <b>Deterministic.</b> Only the boxes given are read: the row's top is a lane below the lowest
        /// controlled component; columns are taken in order of the controlled component's centre (then the order
        /// given) and pushed right just far enough to keep a lane between neighbours; the whole row then steps
        /// down until it overlaps no box in <paramref name="obstacles"/>, and past a bounded number of steps it is
        /// put a lane below all of them.
        /// </para>
        /// </summary>
        /// <param name="controlled">One box per controller, in controller order - the component it controls. A
        /// null entry (a component TAS would not size) gives a null result entry: that controller is not moved.</param>
        /// <param name="obstacles">Every box already drawn in the air system.</param>
        /// <param name="width">A controller's drawn width.</param>
        /// <param name="height">A controller's drawn height.</param>
        /// <returns>One box per entry of <paramref name="controlled"/>, or null for that entry.</returns>
        public static List<VentilationLayoutRectangle> GuidanceControllerLayout(
            IList<VentilationLayoutRectangle> controlled,
            IEnumerable<VentilationLayoutRectangle> obstacles,
            int width,
            int height)
        {
            List<VentilationLayoutRectangle> result = new List<VentilationLayoutRectangle>();
            if (controlled == null)
            {
                return result;
            }

            List<int> indexes = new List<int>();
            int top = int.MinValue;
            for (int i = 0; i < controlled.Count; i++)
            {
                result.Add(null);

                if (controlled[i] != null)
                {
                    indexes.Add(i);
                    top = global::System.Math.Max(top, controlled[i].Bottom + GuidanceControllerBandGap);
                }
            }

            if (indexes.Count == 0 || width <= 0 || height <= 0)
            {
                return result;
            }

            indexes.Sort((x, y) =>
            {
                int compare = CentreX(controlled[x]).CompareTo(CentreX(controlled[y]));
                return compare != 0 ? compare : x.CompareTo(y);
            });

            int[] xs = new int[controlled.Count];
            int right = int.MinValue;
            foreach (int index in indexes)
            {
                int x = CentreX(controlled[index]) - (width / 2);
                if (right != int.MinValue)
                {
                    x = global::System.Math.Max(x, right + GuidanceControllerGap);
                }

                xs[index] = x;
                right = x + width;
            }

            List<VentilationLayoutRectangle> obstacles_NotNull = new List<VentilationLayoutRectangle>();
            foreach (VentilationLayoutRectangle obstacle in obstacles ?? new VentilationLayoutRectangle[0])
            {
                if (obstacle != null)
                {
                    obstacles_NotNull.Add(obstacle);
                }
            }

            for (int step = 0; step <= GuidanceControllerBandSteps; step++)
            {
                int y = top + (step * GuidanceControllerBandStep);
                if (Clear(indexes, xs, y, width, height, obstacles_NotNull))
                {
                    return Band(result, indexes, xs, y, width, height);
                }
            }

            VentilationLayoutRectangle union = VentilationLayoutRectangle.Union(obstacles_NotNull);
            return Band(result, indexes, xs, global::System.Math.Max(top, union.Bottom + GuidanceControllerBandGap), width, height);
        }

        private static int CentreX(VentilationLayoutRectangle ventilationLayoutRectangle)
        {
            return ventilationLayoutRectangle.X + (ventilationLayoutRectangle.Width / 2);
        }

        private static bool Clear(List<int> indexes, int[] xs, int y, int width, int height, List<VentilationLayoutRectangle> obstacles)
        {
            foreach (int index in indexes)
            {
                VentilationLayoutRectangle ventilationLayoutRectangle = new VentilationLayoutRectangle(xs[index], y, width, height);
                if (obstacles.Exists(ventilationLayoutRectangle.Overlaps))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<VentilationLayoutRectangle> Band(List<VentilationLayoutRectangle> result, List<int> indexes, int[] xs, int y, int width, int height)
        {
            foreach (int index in indexes)
            {
                result[index] = new VentilationLayoutRectangle(xs[index], y, width, height);
            }

            return result;
        }
    }
}
