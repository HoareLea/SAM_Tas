// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>Not a statement about which half of an opening a physical surface is.</b> This answers
        /// <c>Frame</c> for <c>BEType</c> 14, which is TAS's DOOR type - the pairing the aperture-type WRITE
        /// relies on, where "the half that is not glazing" is what is wanted. A door leaf is an opening's
        /// glazed half, so reading a surface this way classifies a door's own surface as the frame and leaves
        /// the opening with no pane.
        /// <para>
        /// To read a TBD element, use <see cref="AperturePart_BuildingElementType(TBD.buildingElement)"/>,
        /// which is what <c>Convert.ToSAM</c> and <c>Query.Match</c> both use. The two sides of a round trip
        /// have to agree: when they did not, <c>Modify.UpdateIds</c> collected a door-typed pane surface into
        /// the FRAME set and no aperture part could be rebound afterwards.
        /// </para>
        /// </summary>
        public static AperturePart AperturePart(this int bEType)
        {
            switch(bEType)
            {
                case 14:
                    return Analytical.AperturePart.Frame;
                case 12:
                    return Analytical.AperturePart.Pane;
                case 13:
                    return Analytical.AperturePart.Pane;
                case 15:
                    return Analytical.AperturePart.Frame;
            }

            return Analytical.AperturePart.Undefined;
        }
    }
}