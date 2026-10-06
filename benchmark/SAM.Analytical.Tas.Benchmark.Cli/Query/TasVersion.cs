// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace SAM.Analytical.Tas.Benchmark
{
    public static partial class Query
    {
        // The EDSL Tas install directory is recorded here (same key SAM.Core.Tas reads).
        private const string TasFilesKey = @"HKEY_CURRENT_USER\Software\EDSL\TasManager\TasFiles";

        private const string TasManagerKey = @"HKEY_CURRENT_USER\Software\EDSL\TasManager";

        // Best-effort probe order for the engine executable whose FileVersion identifies the TAS
        // product version. EDSL ships several executables; the first that resolves wins.
        private static readonly string[] TasExecutableCandidates =
        {
            "TAS Engine.exe",
            "TasEngine.exe",
            "TAS.exe",
            "Tas.exe",
            "TBD.exe",
            "Tas3D.exe",
        };

        /// <summary>
        /// Resolves the EDSL Tas engine version, best-effort and side-effect-free: an explicit value
        /// wins, then a version recorded in the registry, then the <see cref="FileVersionInfo"/> of a
        /// known Tas executable under the registered install directory. Returns null when none
        /// resolve (the producer then records a note and emits a null engine version — TAS exposes no
        /// version in code, so this is an accepted gap). Windows-only; any failure yields null.
        /// </summary>
        /// <param name="explicitVersion">An operator-supplied override (CLI/env), used verbatim when non-empty.</param>
        public static string TasVersion(string explicitVersion = null)
        {
            if (!string.IsNullOrWhiteSpace(explicitVersion))
            {
                return explicitVersion.Trim();
            }

            string registryVersion = RegistryValue(TasManagerKey, "Version") ?? RegistryValue(TasManagerKey, "TasVersion");
            if (!string.IsNullOrWhiteSpace(registryVersion))
            {
                return registryVersion.Trim();
            }

            string directory = TasDirectory();
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return null;
            }

            foreach (string candidate in TasExecutableCandidates)
            {
                string version = FileVersion(Path.Combine(directory, candidate));
                if (!string.IsNullOrWhiteSpace(version))
                {
                    return version;
                }
            }

            return null;
        }

        /// <summary>The registered EDSL Tas install directory, or null when the key is absent.</summary>
        public static string TasDirectory()
        {
            return RegistryValue(TasFilesKey, "Path");
        }

        private static string FileVersion(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return null;
                }

                FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
                return string.IsNullOrWhiteSpace(info?.ProductVersion) ? info?.FileVersion : info.ProductVersion;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string RegistryValue(string key, string name)
        {
            try
            {
                return Registry.GetValue(key, name, null) as string;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
