// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Xml;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Convert
    {
        public static bool ToXml(this Building building, string path)
        {
            if (building == null || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                OmitXmlDeclaration = false,
                Indent = true,
            };

            using (XmlWriter xmlWriter = XmlWriter.Create(path, xmlWriterSettings))
            {
                xmlWriter.WriteStartDocument();
                building.ToXml(xmlWriter);
                xmlWriter.WriteEndDocument();
                xmlWriter.Flush();
            }

            return true;
        }

        public static bool ToXml(this AnalyticalModel analyticalModel, string path, TM59Manager tM59Manager = null)
        {
            if(tM59Manager == null)
            {
                tM59Manager = new TM59Manager();
            }

            Building builidng = analyticalModel.ToTM59(tM59Manager);
            if(builidng == null)
            {
                return false;
            }

            return ToXml(builidng, path);
        }

        /// <summary>
        /// The TM59 XML with the ventilation system type stated by the <c>OverheatingScenario</c>s rather than
        /// derived from the model.
        /// <para>
        /// <b>This is the entry point the official TAS print workflow needs.</b> That workflow -
        /// <c>SAMAnalytical.CreateTBDByTM59</c> - writes the TM59 configuration that TAS then opens with every
        /// space already defined, so a user only steps through the TM59 tabs and prints. The ventilation strategy
        /// therefore has to be right <b>at export time</b>: nothing downstream reads a SAM scenario, and a wrong
        /// "Nat Vent" in this file becomes a wrong official assessment nobody re-derives.
        /// </para>
        /// <para>
        /// <b>The map for this path is keyed on DESIGN space guids</b>, because there is no simulation yet - the
        /// model being exported is the design model. Build it with
        /// <c>SimulationSpaceMap.Identity(analyticalModel.GetSpaces())</c> and an <c>OverheatingScenarioMap</c>;
        /// do not build a name-fallback map, which would refuse three rooms all called "Bedroom 2".
        /// </para>
        /// <para>
        /// Refuses the whole file rather than writing a partial one - see
        /// <c>Convert.ToTM59(AnalyticalModel, TM59Manager, VentilationStrategyMap, out List&lt;string&gt;)</c> for
        /// why that asymmetry with the assessment is deliberate.
        /// </para>
        /// </summary>
        /// <param name="ventilationStrategyRefusals">
        /// Why nothing was written, one sentence per space. Never null; empty on success.
        /// </param>
        public static bool ToXml(this AnalyticalModel analyticalModel, string path, TM59Manager tM59Manager, VentilationStrategyMap ventilationStrategyMap, out List<string> ventilationStrategyRefusals)
        {
            if (tM59Manager == null)
            {
                tM59Manager = new TM59Manager();
            }

            Building building = analyticalModel.ToTM59(tM59Manager, ventilationStrategyMap, out ventilationStrategyRefusals);
            if (building == null)
            {
                return false;
            }

            return ToXml(building, path);
        }
    }
}
