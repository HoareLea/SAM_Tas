// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Whether two <see cref="FeatureShade"/>s state the same physical shading. <see cref="FeatureShade"/>
        /// has no value equality of its own, so the content is compared field by field - exposed here, pure
        /// and COM-free, so the <c>UpdateBuildingElements</c> split decision can be unit-tested without a
        /// TAS install.
        /// <para>
        /// <b>Name and description are deliberately not compared.</b> The export creates the TBD shade with
        /// <c>building.AddFeatureShade(null)</c>, so TAS assigns the name itself, and neither text changes
        /// what a shade does to a result. Only the geometry and transmittance fields decide.
        /// </para>
        /// <para>
        /// <b>Fields compare as <c>float</c>, not <c>double</c>.</b> TBD stores these values as Single, so a
        /// shade that round-tripped through a TBD file comes back at float precision: a double-exact
        /// comparison would report every round-tripped shade as changed and split it on every update, while
        /// a difference smaller than TBD's own storage precision is not a difference TAS can carry. Two NaNs
        /// compare equal (a field never written on either side is not a difference), NaN against a value is
        /// not.
        /// </para>
        /// </summary>
        public static bool FeatureShadesMatch(this FeatureShade featureShade_1, FeatureShade featureShade_2)
        {
            if (ReferenceEquals(featureShade_1, featureShade_2))
            {
                return true;
            }

            if (featureShade_1 == null || featureShade_2 == null)
            {
                return false;
            }

            return Equal(featureShade_1.SurfaceHeight, featureShade_2.SurfaceHeight)
                && Equal(featureShade_1.SurfaceWidth, featureShade_2.SurfaceWidth)
                && Equal(featureShade_1.LeftFinDepth, featureShade_2.LeftFinDepth)
                && Equal(featureShade_1.LeftFinOffset, featureShade_2.LeftFinOffset)
                && Equal(featureShade_1.LeftFinTransmittance, featureShade_2.LeftFinTransmittance)
                && Equal(featureShade_1.RightFinDepth, featureShade_2.RightFinDepth)
                && Equal(featureShade_1.RightFinOffset, featureShade_2.RightFinOffset)
                && Equal(featureShade_1.RightFinTransmittance, featureShade_2.RightFinTransmittance)
                && Equal(featureShade_1.OverhangDepth, featureShade_2.OverhangDepth)
                && Equal(featureShade_1.OverhangOffset, featureShade_2.OverhangOffset)
                && Equal(featureShade_1.OverhangTransmittance, featureShade_2.OverhangTransmittance);

            static bool Equal(double value_1, double value_2)
            {
                if (double.IsNaN(value_1) || double.IsNaN(value_2))
                {
                    return double.IsNaN(value_1) && double.IsNaN(value_2);
                }

                return (float)value_1 == (float)value_2;
            }
        }
    }
}
