# HybridAD-Manager — Agent Guide

> **Target reader:** AI coding agents with no prior knowledge of this project.  
> **Language:** All source comments, documentation, and UI strings are in English.  
> **Last updated:** 2026-08-03

---

## Project Overview

HybridAD-Manager is a **standalone Windows WPF desktop application** that provides a single pane of glass for managing **Hybrid Active Directory + Microsoft Entra ID** environments. The UI is modeled after the familiar **Active Directory Users & Computers (dsa.msc)** console, extended with cloud-native tabs for Microsoft 365 / Entra ID management.

**Current status:** Phases 1–6 are complete (Foundation & Shell, Active Directory integration, Entra ID integration, Cloud Management tabs, Search/Saved Queries + Bulk Operations + Force Sync + CSV export, and Keyboard Shortcuts/Drag-and-Drop/Accessibility/MSIX packaging). See the root README for the verified feature list.

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Framework | .NET 8 WPF (`net8.0-windows`) |
| UI Pattern | MVVM with `CommunityToolkit.Mvvm` source generators |
| DI Container | `Microsoft.Extensions.DependencyInjection` 8.0.0 |
| AD Connectivity | `System.DirectoryServices` / `System.DirectoryServices.AccountManagement` |
| Entra ID / Graph | `Microsoft.Graph` SDK v5.46.0 |
| Authentication | `Microsoft.Identity.Client` (MSAL.NET) 4.60.3 — OAuth2 + PKCE, MFA support |
| Token Cache | `Microsoft.Identity.Client.Extensions.Msal` — Windows DPAPI, macOS Keychain, Linux Secret Service |
| Styling | Custom XAML theme mimicking ADUC + Windows 11 admin-tool aesthetics |

---

## Project Structure

```
HybridADManager/
├── App.xaml / App.xaml.cs              # Application entry point + DI container setup
├── MainWindow.xaml                     # Shell: Menu, Toolbar, Tree/List split, StatusBar, Busy overlay
├── Models/
│   ├── DirectoryObject.cs              # Base observable model + HybridUser, HybridGroup, HybridComputer
│   ├── DirectoryNode.cs                # Tree node model + NodeType enum
│   └── SyncStatus.cs                   # SyncState enum + SyncStatus observable model
├── Services/
│   ├── IActiveDirectoryService.cs      # AD operations interface + DomainNode
│   ├── ActiveDirectoryService.cs       # System.DirectoryServices implementation
│   ├── IAuthenticationService.cs       # MSAL auth interface + settings
│   ├── AuthenticationService.cs        # OAuth2 + PKCE + token cache
│   ├── IGraphService.cs                # Graph operations interface + DTOs (HybridSyncState, M365License, etc.)
│   └── GraphService.cs                 # Microsoft Graph SDK v5 implementation
├── ViewModels/
│   ├── MainWindowViewModel.cs          # Auth state, command routing, property sheet opening
│   ├── DirectoryTreeViewModel.cs       # Tree population, AD fallback to dummy data
│   ├── ObjectListViewModel.cs          # List loading, Entra correlation, filtering
│   ├── PropertySheetViewModelBase.cs   # Base VM for property sheets + User/Group sheet VMs
│   └── Tabs/
│       ├── GeneralTabViewModel.cs      # General, Account, Address, Organization, MemberOf VMs
│       └── HybridStatusTabViewModel.cs # Live sync state loading, force sync
├── Views/
│   ├── DirectoryTreeView.xaml          # Left-pane TreeView with icons
│   ├── ObjectListView.xaml             # Right-pane GridView with search
│   └── PropertySheets/
│       ├── UserPropertySheet.xaml      # 13-tab property dialog shell
│       └── Tabs/                       # General, Address, Account, Organization, MemberOf, HybridStatus
└── Infrastructure/
    ├── Converters/                     # Bool→Visibility, SyncState→Brush, ObjectType→Icon, InverseBool
    └── Themes/
        ├── Colors.xaml                 # Brushes / palette
        ├── Converters.xaml             # Converter resource declarations
        ├── ADUCStyles.xaml             # Window, Menu, ToolBar, TreeView, ListView, TabControl styles
        └── DataTemplates.xaml          # HierarchicalDataTemplate for DirectoryNode
```

**Solution file:** `HybridADManager.sln` (single project, Visual Studio 2022+ compatible).

---

## Build and Run

```bash
cd HybridADManager
dotnet restore
dotnet build
dotnet run
```

- **Output type:** `WinExe` (Windows-only).
- **Configurations:** `Debug|Any CPU`, `Release|Any CPU`.
- The app requires a Windows environment with .NET 8 runtime. It will gracefully fall back to demo data when Active Directory is unavailable.

---

## Code Style Guidelines

### C# Conventions
- **File-scoped namespaces** are used everywhere.
- **Nullable reference types** are enabled (`<Nullable>enable</Nullable>`).
- **Implicit usings** are enabled (`<ImplicitUsings>enable</ImplicitUsings>`).
- **Private fields:** prefixed with underscore `_`, snake_case for the generated property name (e.g., `_displayName` → `DisplayName`).
- **Source generators:** Heavy use of `CommunityToolkit.Mvvm` attributes:
  - `[ObservableProperty]` on private fields to auto-generate observable properties.
  - `[RelayCommand]` on private methods to auto-generate `ICommand` implementations.
  - `partial` classes are required for source-generator compatibility.
- **Async patterns:** Services return `Task<T>`. AD-bound work is wrapped in `Task.Run()` to keep UI responsive.
- **Error handling:** Services swallow exceptions broadly (empty `catch` blocks) and return fallback data to keep the UI functional when AD/Graph is unreachable. **Do not change this philosophy without discussion.**

### XAML Conventions
- Custom styles are stored in `Infrastructure/Themes/`.
- Static resources follow naming convention: `{Control}{State}Brush` (e.g., `WindowBackgroundBrush`, `SyncInSyncBrush`).
- Emoji are used as toolbar icons (no image assets yet).
- TreeView and ListView use custom `ControlTemplate`s for ADUC-like selection/hover visuals.

---

## Architecture & Key Patterns

### MVVM
- **Views** are XAML `Window` / `UserControl` with minimal code-behind (usually just event forwarding).
- **ViewModels** use `ObservableObject` + source generators; no code-behind logic.
- **Models** are also `ObservableObject`-derived so they can bind directly to property sheet tabs.

### Dependency Injection
- Setup lives in `App.ConfigureServices(IServiceCollection)`.
- Services are singletons:
  - `IAuthenticationService` → `AuthenticationService`
  - `IGraphService` → `GraphService`
  - `IActiveDirectoryService` → `ActiveDirectoryService`
- ViewModels resolve services via `App.Current.Services.GetService(...)` with manual fallback instantiation.

### Data Flow
1. `DirectoryTreeViewModel` loads domain/OU tree (AD first, demo fallback).
2. `ObjectListViewModel` loads objects for the selected node (AD first, demo fallback).
3. When authenticated, `ObjectListViewModel` correlates AD objects with Graph by UPN to populate `EntraObjectId`, `ImmutableId`, and `SyncStatus`.
4. Double-click / Properties opens `UserPropertySheet`, which creates `UserPropertySheetViewModel` and child tab VMs.
5. `HybridStatusTabViewModel` calls `IGraphService.GetSyncStateAsync` lazily when the tab is instantiated.

---

## Security Considerations

### Authentication
- Uses **MSAL.NET** with **OAuth2 + PKCE** (public client flow).
- Supports MFA.
- **No credentials are stored in code or configuration files.**
- Tokens are cached via platform-specific secure storage (Windows DPAPI by default).

### App Registration
Before Entra ID features work, an app must be registered in Azure AD:
1. Azure Portal → Microsoft Entra ID → App registrations → New registration.
2. Name: `HybridAD-Manager`.
3. Supported account types: **Accounts in this organizational directory only**.
4. Redirect URI: **Public client/native** → `http://localhost`.
5. Required API permissions:
   - `User.Read.All`
   - `Group.Read.All`
   - `Directory.Read.All`
   - `Organization.Read.All`
6. Grant **admin consent**.
7. Update `AuthenticationSettings.ClientId` in `Services/IAuthenticationService.cs` with your client ID.

> **Note:** The codebase currently ships with the **Microsoft Graph Explorer client ID** (`d3590ed6-...`) as a placeholder for testing. Replace this before production use.

---

## Testing

- **No test projects exist yet.** The codebase relies on:
  - Graceful fallback to demo data when AD is unavailable.
  - Manual UI testing via the demo `contoso.com` dataset.
- If adding tests, create an `xUnit` or `MSTest` project alongside the main project.

---

## Deployment

- Standard .NET 8 WPF executable.
- Future phases plan for **MSIX packaging** (not yet implemented).
- The app references `Resources\app.ico` and `Resources\**` folder (images are currently placeholders).

---

## Common Tasks for Agents

### Adding a New Property Sheet Tab
1. Add the ViewModel in `ViewModels/Tabs/` (derive from `ObservableObject` or extend `PropertySheetViewModelBase`).
2. Add the XAML `UserControl` in `Views/PropertySheets/Tabs/`.
3. Wire the VM into the sheet VM (e.g., `UserPropertySheetViewModel`).
4. Add a `TabItem` in `Views/PropertySheets/UserPropertySheet.xaml`.

### Adding a New Service
1. Define the interface in `Services/`.
2. Implement the concrete class in `Services/`.
3. Register the singleton in `App.ConfigureServices`.
4. Inject via constructor or resolve with `App.Current.Services.GetService(...)`.

### Updating the ADUC Theme
- Modify `Infrastructure/Themes/Colors.xaml` for palette changes.
- Modify `Infrastructure/Themes/ADUCStyles.xaml` for control styles.
- Converters are declared in `Infrastructure/Themes/Converters.xaml`.

---

## Out of Scope (MVP)

- Group Policy management
- Certificate / PKI management
- Exchange on-premises mailbox management
- Detailed audit log viewer
- Multi-forest support (single domain only)

---

## Additional Resources

- `plan.md` — Full development plan with phased feature breakdown and UI mockups.
- `HybridADManager/README.md` — Build instructions, auth setup, and architecture summary.
