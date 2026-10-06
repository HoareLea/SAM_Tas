// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Calls into <c>SAM.Analytical.Tas.TPD</c> methods whose signatures mention embedded interop types.
    /// <para>
    /// <c>SAM.Analytical.Tas.TPD</c> references <c>Interop.TPD</c> with <c>EmbedInteropTypes=True</c>. A method
    /// such as <c>Query.ZoneLoads</c>, which returns <c>List&lt;TPD.ZoneLoad&gt;</c>, therefore cannot be named
    /// from another assembly at all: the C# compiler answers <b>CS1769</b>. That is a compile-time rule about
    /// naming the type, not a runtime rule about calling the method - the CLR unifies the embedded
    /// <c>TPD.ZoneLoad</c> with the one in <c>Interop.TPD</c> by COM type equivalence, so a reflected call
    /// passes managed fakes straight through.
    /// </para>
    /// <para>
    /// The alternative would be to stop embedding <c>Interop.TPD</c> in the production assembly purely so a test
    /// could compile. That is a binary-compatibility change to every consumer of <c>SAM.Analytical.Tas.TPD</c>,
    /// made for a test's benefit, and it is deliberately not done: <b>the production embedding is unchanged.</b>
    /// </para>
    /// <para>
    /// No COM object is created here and no TAS licence is involved. Only managed fakes are passed in.
    /// </para>
    /// </summary>
    internal static class TpdReflection
    {
        private static readonly Assembly assembly = typeof(global::SAM.Analytical.Tas.TPD.ResultantTemperaturePreparation).Assembly;

        /// <summary>
        /// Invokes the production <c>SAM.Analytical.Tas.TPD.Query.ZoneLoads(SystemComponent)</c> and returns
        /// what it produced as a plain list of objects, in the order the production code built it.
        /// </summary>
        /// <remarks>
        /// The overload is selected by its single <c>TPD.SystemComponent</c> parameter, so the
        /// <c>SystemZone</c> and the two <c>TSDData</c> overloads cannot be hit by accident.
        /// </remarks>
        public static List<object> ZoneLoads_SystemComponent(object systemComponent)
        {
            MethodInfo methodInfo = ZoneLoadsMethod("SystemComponent");

            object result = Invoke(methodInfo, new object[] { systemComponent });

            if (result == null)
            {
                return null;
            }

            return ((IEnumerable)result).Cast<object>().ToList();
        }

        /// <summary>
        /// Invokes the production <c>Query.ZoneLoads(SystemZone)</c> - the overload that delegates to the
        /// <c>SystemComponent</c> one - so the delegation itself is covered rather than assumed.
        /// </summary>
        public static List<object> ZoneLoads_SystemZone(object systemZone)
        {
            MethodInfo methodInfo = ZoneLoadsMethod("SystemZone");

            object result = Invoke(methodInfo, new object[] { systemZone });

            if (result == null)
            {
                return null;
            }

            return ((IEnumerable)result).Cast<object>().ToList();
        }

        private static MethodInfo ZoneLoadsMethod(string parameterTypeName)
        {
            Type type = assembly.GetType("SAM.Analytical.Tas.TPD.Query", throwOnError: true);

            List<MethodInfo> methodInfos = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.Name == "ZoneLoads")
                .Where(x => !x.IsGenericMethod)
                .Where(x => x.GetParameters().Length == 1)
                .Where(x => x.GetParameters()[0].ParameterType.Name == parameterTypeName)
                .ToList();

            if (methodInfos.Count != 1)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Expected exactly one Query.ZoneLoads overload taking a single {0}; found {1}.",
                        parameterTypeName,
                        methodInfos.Count));
            }

            return methodInfos[0];
        }

        private static object Invoke(MethodInfo methodInfo, object[] parameters)
        {
            try
            {
                return methodInfo.Invoke(null, parameters);
            }
            catch (TargetInvocationException targetInvocationException) when (targetInvocationException.InnerException != null)
            {
                // Surface what the production code actually threw, not the reflection wrapper.
                throw targetInvocationException.InnerException;
            }
        }

        /// <summary>Reads <c>ZoneLoad.Name</c> off a value the production code returned.</summary>
        public static string Name(object zoneLoad)
        {
            return zoneLoad == null ? null : ((global::TPD.ZoneLoad)zoneLoad).Name;
        }

        /// <summary>Reads <c>ZoneLoad.GUID</c> off a value the production code returned.</summary>
        public static string Guid(object zoneLoad)
        {
            return zoneLoad == null ? null : ((global::TPD.ZoneLoad)zoneLoad).GUID;
        }
    }
}
