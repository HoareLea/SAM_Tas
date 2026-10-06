// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        /// <summary>
        /// The deterministic ordering key for a source system component: its own guid.
        /// <para>
        /// <b>Why this exists.</b> When an <c>AirSystemGroup</c> is replicated by
        /// <c>ComponentGroup.SetMultiplicity(n)</c>, the conversion has to decide which analytical room owns
        /// which of the <c>n</c> replicated TAS zones. It used to decide that by list position -
        /// <c>tuples[0]</c> then <c>RemoveAt(0)</c>, over buckets built by a backwards loop - so the answer
        /// was a function of the order the caller's collection happened to be in. Two rooms called
        /// "Bedroom 2" in different flats could swap zones between runs of the same model.
        /// </para>
        /// <para>
        /// Sorting by this key replaces enumeration order with a <b>stated rule</b>: ascending source guid,
        /// which is the same rule PR1 materialises the graph by
        /// (<c>MechanicalVentilationAirSystem</c> walks its space guids ascending). It is a property of the
        /// source object and of nothing else - not a display name, not a position, not an index.
        /// </para>
        /// <para>
        /// A component with no guid sorts to <see cref="Guid.Empty"/>, which is a stable position rather than
        /// a throw; such a component cannot be bound by identity anyway and is refused later, by the binding,
        /// where the refusal can say which room it was.
        /// </para>
        /// </summary>
        public static Guid SourceOrderKey(this Core.Systems.ISystemComponent systemComponent)
        {
            if (systemComponent == null)
            {
                return Guid.Empty;
            }

            try
            {
                object guid = ((dynamic)systemComponent).Guid;

                if (guid is Guid)
                {
                    return (Guid)guid;
                }
            }
            catch
            {
                // A source component that does not carry a guid at all orders as Guid.Empty. It is refused
                // by the binding rather than here, so the message can name the room.
            }

            return Guid.Empty;
        }
    }
}
