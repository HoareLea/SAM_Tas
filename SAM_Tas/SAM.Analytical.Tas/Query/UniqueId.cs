// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string UniqueId(this TCD.IMaterial material)
        {
            if(material == null)
            {
                return null;
            }

            return new TCDMaterialData(material).UniqueId();
        }
    }
}