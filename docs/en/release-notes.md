# Release Notes

This page tracks framework updates for the VitePress site.

## v1.2.0

- **Breaking change**: the core NuGet package `SyZero` has been renamed to `SyZero.Core`. Replace `dotnet add package SyZero` with `dotnet add package SyZero.Core`. The assembly name (`SyZero.dll`) and root namespace (`SyZero`) are unchanged, so no source code changes are required.
- **Repository restructure**: the 18 pluggable module projects under `src/SyZero.Core/SyZero.*` have been moved to a new top-level folder `src/SyZero.Module/`; the contents of `src/SyZero.Core/SyZero/` have been promoted to `src/SyZero.Core/`.

The authoritative changelog remains in [docs/release-notes.md](../release-notes.md).
