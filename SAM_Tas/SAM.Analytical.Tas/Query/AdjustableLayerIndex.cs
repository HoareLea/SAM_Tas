// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>Thinnest layer (in metres) the thermal transmittance calculation may adjust: 10 mm.</summary>
        public const double MinimumAdjustableLayerThickness = 0.01;

        // TCD stores widths as float, so a 10 mm layer reads back as 0.00999999977 m; allow for that.
        private const double AdjustableLayerThicknessTolerance = 1E-6;

        /// <summary>
        /// Whether a layer may be the one whose thickness is varied to reach a target thermal transmittance.
        /// Gas and transparent layers never qualify - decided by material type, not by conductivity, because a
        /// gas gap can have a real, positive conductivity lower than the insulation next to it. Layers thinner
        /// than 10 mm and layers with a NaN or non-positive conductivity do not qualify either.
        /// </summary>
        public static bool IsAdjustableLayer(MaterialType materialType, double conductivity, double thickness)
        {
            if (materialType == Core.MaterialType.Gas || materialType == Core.MaterialType.Transparent)
            {
                return false;
            }

            if (double.IsNaN(conductivity) || conductivity <= 0)
            {
                return false;
            }

            if (double.IsNaN(thickness) || thickness < MinimumAdjustableLayerThickness - AdjustableLayerThicknessTolerance)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Index of the adjustable layer with the lowest conductivity, or -1 when no layer qualifies
        /// (see <see cref="IsAdjustableLayer(MaterialType, double, double)"/>). Ties keep the first layer.
        /// </summary>
        /// <param name="layers">Per layer, in construction order: material type, conductivity [W/mK], thickness [m]. A null entry is never adjustable.</param>
        public static int AdjustableLayerIndex(IEnumerable<Tuple<MaterialType, double, double>> layers)
        {
            if (layers == null)
            {
                return -1;
            }

            int result = -1;
            double min = double.MaxValue;
            int index = -1;
            foreach (Tuple<MaterialType, double, double> layer in layers)
            {
                index++;
                if (layer == null || !IsAdjustableLayer(layer.Item1, layer.Item2, layer.Item3))
                {
                    continue;
                }

                if (layer.Item2 < min)
                {
                    min = layer.Item2;
                    result = index;
                }
            }

            return result;
        }

        /// <summary>
        /// Index of the construction layer that should be adjusted by default, or -1 when there is none.
        /// </summary>
        public static int AdjustableLayerIndex(this Construction construction, MaterialLibrary materialLibrary)
        {
            List<ConstructionLayer> constructionLayers = construction?.ConstructionLayers;
            if (constructionLayers == null || constructionLayers.Count == 0)
            {
                return -1;
            }

            List<Tuple<MaterialType, double, double>> layers = new List<Tuple<MaterialType, double, double>>();
            foreach (ConstructionLayer constructionLayer in constructionLayers)
            {
                IMaterial material = constructionLayer == null ? null : materialLibrary?.GetMaterial(constructionLayer.Name);
                if (!(material is Material material_Temp))
                {
                    // Unknown material: never adjustable (keeps the index aligned with the construction layers).
                    layers.Add(null);
                    continue;
                }

                layers.Add(new Tuple<MaterialType, double, double>(Core.Query.MaterialType(material), material_Temp.ThermalConductivity, constructionLayer.Thickness));
            }

            return AdjustableLayerIndex(layers);
        }

        /// <summary>
        /// Maps the TCD material type to the SAM material type used by <see cref="IsAdjustableLayer(MaterialType, double, double)"/>.
        /// </summary>
        public static MaterialType ToMaterialType(this TCD.material material)
        {
            if (material == null)
            {
                return Core.MaterialType.Undefined;
            }

            switch ((TBD.MaterialTypes)material.type)
            {
                case TBD.MaterialTypes.tcdGasLayer:
                    return Core.MaterialType.Gas;

                case TBD.MaterialTypes.tcdTransparentLayer:
                    return Core.MaterialType.Transparent;

                case TBD.MaterialTypes.tcdOpaqueLayer:
                case TBD.MaterialTypes.tcdOpaqueMaterial:
                    return Core.MaterialType.Opaque;
            }

            return Core.MaterialType.Undefined;
        }

        /// <summary>
        /// Index of the TCD layer that should be adjusted by default, or -1 when there is none.
        /// </summary>
        public static int AdjustableLayerIndex(IEnumerable<TCD.material> materials)
        {
            if (materials == null)
            {
                return -1;
            }

            List<Tuple<MaterialType, double, double>> layers = new List<Tuple<MaterialType, double, double>>();
            foreach (TCD.material material in materials)
            {
                layers.Add(material == null ? null : new Tuple<MaterialType, double, double>(ToMaterialType(material), material.conductivity, material.width));
            }

            return AdjustableLayerIndex(layers);
        }
    }
}
