// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>What one TBD building element is, and how many physical surfaces still point at it</b> - read out
    /// of COM and into a value, so the decision of which elements the gbXML canonicalisation has left
    /// orphaned is a pure function and testable without an installed TAS.
    /// </summary>
    public sealed class ApertureBuildingElementUsage
    {
        /// <param name="guid">The element's <c>buildingElement.GUID</c>.</param>
        /// <param name="name">Its name, for reporting only - it takes no part in the decision.</param>
        /// <param name="bEType">Its <c>buildingElement.BEType</c>.</param>
        /// <param name="zoneSurfaceCount">How many physical <c>zoneSurface</c>es are bound to it AFTER the rebind pass.</param>
        public ApertureBuildingElementUsage(string guid, string name, int bEType, int zoneSurfaceCount)
        {
            Guid = guid;
            Name = name;
            BEType = bEType;
            ZoneSurfaceCount = zoneSurfaceCount;
        }

        /// <summary>The element's GUID.</summary>
        public string Guid { get; }

        /// <summary>The element's name. Reporting only.</summary>
        public string Name { get; }

        /// <summary>The element's TAS building-element type.</summary>
        public int BEType { get; }

        /// <summary>How many physical surfaces are bound to it after the rebind.</summary>
        public int ZoneSurfaceCount { get; }

        /// <summary>
        /// Whether this element is half of an opening rather than a panel's - asked through
        /// <see cref="Query.AperturePart_BEType(int)"/>, the one definition of that reading, so the sweep
        /// recognises exactly what the import and <c>Query.Match</c> recognise.
        /// <para>
        /// <b>It used to name only <c>GLAZING</c> and <c>FRAMEELEMENT</c>.</b> TAS types an opening's pane
        /// <c>DOORELEMENT</c> or <c>ROOFLIGHT</c> rather than <c>GLAZING</c> for a whole model at a time, and
        /// such an element then failed this test - so once the rebind had moved its surface onto the shared
        /// definition, the emptied per-aperture element could not be marked and stayed in the TBD. Twenty
        /// windows left twenty surface-less elements in TAS's Building Elements list, which is the very thing
        /// the sweep exists to prevent.
        /// </para>
        /// <para>
        /// The signature takes an <c>int</c> and this property returns a <c>bool</c>, so the value type stays
        /// free of the embedded interop types and a caller in another assembly - the test suite - can
        /// construct and interrogate it.
        /// </para>
        /// </summary>
        public bool IsAperture
        {
            get { return Query.AperturePart_BEType(BEType) != AperturePart.Undefined; }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}) BEType {2}, {3} surface(s)", Name, Guid, BEType, ZoneSurfaceCount);
        }
    }
}
