# HybridAD-Manager — Project Folder

This folder contains the .NET 8 WPF application project (`HybridADManager.csproj`) and all source code.

Full documentation — features, quick start, Entra ID configuration, architecture, and development commands — lives in the [repository root README](../README.md).

Notes specific to this folder:

- `Resources/` holds MSIX/icon assets; see `Resources/README.txt`. The image files are not committed yet, so add them (or remove the `<ApplicationIcon>` line from the `.csproj`) before building.
- Runtime state (MSAL token cache, saved queries) is written to `%LocalAppData%\HybridADManager`, never into this folder.
