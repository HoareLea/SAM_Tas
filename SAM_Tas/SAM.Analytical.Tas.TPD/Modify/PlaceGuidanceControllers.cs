// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>A controller's drawn size where TAS will not say one: the damper box, the smallest it draws.</summary>
        private const int GuidanceControllerSize = 40;

        /// <summary>
        /// Draws the manufacturer-guidance controllers in a row beneath the unit, each under the component it
        /// controls (<see cref="Query.GuidanceControllerLayout"/>). A controller is found as the read-back finds
        /// it: it senses the stat room's zone and has a control arc to that component.
        /// <para>
        /// <b>Presentation only.</b> Only <c>SetPosition</c> is called, after every value has been read back. A
        /// controller that cannot be found or moved is noted, never refused: a drawing cannot make a design
        /// wrong.
        /// </para>
        /// </summary>
        internal static void PlaceGuidanceControllers(
            SystemVentilationConversionContext systemVentilationConversionContext,
            string label,
            global::TPD.System system,
            SystemZone systemZone_Stat,
            IList<object> controlled,
            Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponent)
        {
            if (systemVentilationConversionContext == null || system == null || systemZone_Stat == null || controlled == null)
            {
                return;
            }

            List<string> failures = new List<string>();

            try
            {
                string reference_Zone = Query.NativeReference(systemZone_Stat);

                List<Controller> controllers = new List<Controller>();
                List<VentilationLayoutRectangle> rectangles_Controlled = new List<VentilationLayoutRectangle>();
                foreach (object component in controlled)
                {
                    string reference_Target = component == null ? null : Query.NativeReference(component);
                    Controller controller = reference_Target == null ? null : StatController(system, reference_Zone, reference_Target);
                    if (controller == null)
                    {
                        failures.Add(string.Format("no controller found for {0}", reference_Target ?? "a missing component"));
                    }

                    controllers.Add(controller);
                    rectangles_Controlled.Add(controller == null ? null : NativeRectangle(component));
                }

                List<VentilationLayoutRectangle> obstacles = new List<VentilationLayoutRectangle>();
                foreach (global::TPD.ISystemComponent systemComponent in dictionary_SystemComponent?.Values ?? (IEnumerable<global::TPD.ISystemComponent>)new global::TPD.ISystemComponent[0])
                {
                    obstacles.Add(NativeRectangle(systemComponent));
                }

                VentilationLayoutRectangle size = null;
                foreach (Controller controller in controllers)
                {
                    size = size ?? NativeRectangle(controller);
                }

                int width = size != null && size.Width > 0 ? size.Width : GuidanceControllerSize;
                int height = size != null && size.Height > 0 ? size.Height : GuidanceControllerSize;

                List<VentilationLayoutRectangle> rectangles = Query.GuidanceControllerLayout(rectangles_Controlled, obstacles, width, height);

                for (int i = 0; i < controllers.Count; i++)
                {
                    if (controllers[i] == null)
                    {
                        continue;
                    }

                    if (i >= rectangles.Count || rectangles[i] == null)
                    {
                        failures.Add(string.Format("the component controller {0} drives has no drawn box", i + 1));
                        continue;
                    }

                    ((dynamic)controllers[i]).SetPosition(rectangles[i].X, rectangles[i].Y);
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception.Message);
            }

            systemVentilationConversionContext.Note(string.Format(
                "{0}: its controllers drawn in a row beneath the unit, each under the component it controls. Presentation only.{1}",
                label,
                failures.Count == 0 ? string.Empty : " Not placed: " + string.Join("; ", failures) + "."));
        }

        /// <summary>The first controller sensing the zone with a control arc to the target, by native reference.</summary>
        private static Controller StatController(global::TPD.System system, string reference_Zone, string reference_Target)
        {
            for (int i = 1; i <= system.GetControllerCount(); i++)
            {
                Controller controller = system.GetController(i);
                if (controller == null)
                {
                    continue;
                }

                object component_Sensed = null;
                try
                {
                    component_Sensed = ((dynamic)controller.SensorArc1)?.GetComponent();
                }
                catch
                {
                }

                if (component_Sensed == null || Query.NativeReference(component_Sensed) != reference_Zone)
                {
                    continue;
                }

                for (int j = 1; j <= controller.GetControlArcCount(); j++)
                {
                    if (Query.NativeReference(controller.GetControlArc(j).GetComponent()) == reference_Target)
                    {
                        return controller;
                    }
                }
            }

            return null;
        }
    }
}
