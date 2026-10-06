# SAM.Analytical.Tas.TM59.Tests

Regression coverage for `SAM.Analytical.Tas.TM59.Query.RoomUse` (see [RoomUse.cs](../SAM.Analytical.Tas.TM59/Query/RoomUse.cs)).

## Build order (required before `dotnet test`)

This project does **not** `ProjectReference` `SAM.Analytical.Tas.TM59.csproj`. That project
transitively references `SAM.Analytical.Tas.csproj` / `SAM.Core.Tas.csproj`, which use
`<COMReference>` and require the .NET Framework MSBuild - `ResolveComReference` is not supported
by `dotnet build`/`dotnet test` (.NET Core MSBuild), so a `ProjectReference` here would make even
`dotnet test` on this project alone fail with `MSB4803`.

Instead this project references the **already-built** assemblies via `HintPath`:

- `..\..\build\SAM.Analytical.Tas.TM59.dll` (built by `SAM.Analytical.Tas.TM59.csproj`)
- `..\..\..\SAM\build\SAM.Core.dll`, `..\..\..\SAM\build\SAM.Analytical.dll` (built by the SAM repo)

**Consequence: the solution/library must be built before this test project can build**, whether
that's `dotnet test` run standalone or as part of `SAM_Tas.sln`. Concretely, from a fresh
checkout:

```bash
# 1. Build the SAM assemblies SAM_Tas depends on (produces SAM/build/*.dll)
dotnet build SAM/SAM.Core/SAM.Core.csproj -c Debug
dotnet build SAM/SAM.Units/SAM.Units.csproj -c Debug
dotnet build SAM/SAM.Analytical/SAM.Analytical.csproj -c Debug   # pulls in SAM.Geometry, SAM.Architectural too
dotnet build SAM/SAM.Weather/SAM.Weather.csproj -c Debug

# 2. Build SAM_Tas.sln with the .NET Framework MSBuild (needed for the COM-referencing
#    projects), which produces SAM_Tas/build/SAM.Analytical.Tas.TM59.dll
MSBuild.exe SAM_Tas.sln -restore -p:Configuration=Debug

# 3. Now dotnet test works, standalone or otherwise
dotnet test SAM_Tas/SAM.Analytical.Tas.TM59.Tests -c Debug
```

If step 1/2 is skipped, `dotnet test` fails loudly at build time with
`MSB3245: Could not resolve this reference. Could not locate the assembly "SAM.Analytical.Tas.TM59"`
(and the same for `SAM.Core`/`SAM.Analytical`) - it does **not** silently fall back to some other
copy of these DLLs (e.g. an installed `%APPDATA%\SAM` build, a NuGet cache, or the GAC): `HintPath`
is an explicit, non-probing path, so a missing file is a hard reference-resolution failure, not a
stale-DLL substitution. This has been verified by deleting all `bin`/`obj`/`build` output and
confirming the failure mode above before rebuilding.

`SAM_Tas.sln` itself declares a solution-level `ProjectSection(ProjectDependencies)` from this
project to `SAM.Analytical.Tas.TM59`, so a solution-wide `Rebuild` (e.g. in CI) orders them
correctly without needing a compile-time `ProjectReference`.

## Resource file

`SAM_InternalConditionTextMap_TM59.JSON` is copied from the SAM repo's own resource file (not the
one under a user's `%APPDATA%\SAM` install) via a `<None Include="…" CopyToOutputDirectory=.../>`
link, so tests use the same TextMap the production code ships, without depending on any
machine-specific install state.
