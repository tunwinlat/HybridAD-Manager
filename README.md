# HybridAD-Manager

> ⚠️ **WARNING: WORK IN PROGRESS — NOT PRODUCTION READY**
>
> This project is currently **under active development** and has **not been thoroughly tested** in production environments. Features may be incomplete, unstable, or subject to breaking changes. Use at your own risk.

---

## What is HybridAD-Manager?

**HybridAD-Manager** is a standalone Windows WPF desktop application that provides a single pane of glass for managing **Hybrid Active Directory + Microsoft Entra ID** environments. The UI is modeled after the familiar **Active Directory Users & Computers (dsa.msc)** console, extended with cloud-native tabs for Microsoft 365 / Entra ID management.

No more context-switching between ADUC, the M365 Admin Center, and the Exchange Admin Center.

---

## Features

### Core Shell
- **ADUC-style interface** — Menu, Toolbar, Tree/List split pane, Status Bar
- **Domain/OU tree** — Expandable tree with standard containers and custom OUs
- **Object list view** — Details view with columns, sorting, multi-select, context menus
- **Custom XAML theme** — Mimics Windows 11 admin-tool aesthetics

### Active Directory Management
- Domain enumeration and OU tree walking
- User, group, and computer object retrieval
- **13-tab property sheet**:
  - General
  - Address
  - Account
  - Profile
  - Telephones
  - Organization
  - Member Of
  - Dial-in
  - Environment
  - **Hybrid Status** (live cloud sync state)
- Group membership management
- Context menus (New, Delete, Rename, Move, Refresh, Properties)

### Microsoft Entra ID Integration
- **MSAL.NET authentication** — OAuth2 + PKCE with MFA support
- **Token caching** — Secure platform-specific storage (Windows DPAPI, macOS Keychain, Linux Secret Service)
- **Microsoft Graph SDK v5** — Full Entra ID read/write integration
- Automatic silent token refresh

### Hybrid Management
- **Unified object model** — AD objects correlated with Graph by UPN / immutableId
- **Visual sync indicators**:
  - ✅ In Sync
  - ⏳ Pending
  - ☁️ Cloud-only
  - ❌ Sync Error
- **Hybrid Status tab** — Entra Object ID, Immutable ID, directory source, last sync time, sync errors
- **Force Sync button** — Trigger Azure AD Connect delta sync

### Cloud Management
- **Licenses tab** — Visual SKU/service-plan grid with assign/remove
- **Mailbox tab** — Auto-reply editor, mail forwarding, address list visibility
- **Email Addresses tab** — `proxyAddresses` editor with validation, primary SMTP management

### Search & Operations
- **Find Dialog** — Global LDAP search across domain by name, email, description, phone
- **Saved Queries** — Persist custom LDAP filters to JSON; appear in tree under "Saved Queries"
- **Bulk Operations** — Multi-select enable/disable/delete; context menu on list view
- **CSV Export** — Export objects or container to CSV with full field coverage
- **Force Sync** — Attempts `Start-ADSyncSyncCycle` via PowerShell when on AAD Connect server

### Search & Operations
- Quick search filter in list view
- Graceful fallback to demo `contoso.com` data when AD is unavailable
- Bulk operations framework foundation

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | .NET 8 WPF (`net8.0-windows`) |
| UI Pattern | MVVM with `CommunityToolkit.Mvvm` source generators |
| DI Container | `Microsoft.Extensions.DependencyInjection` |
| AD Connectivity | `System.DirectoryServices` / `System.DirectoryServices.AccountManagement` |
| Entra ID / Graph | `Microsoft.Graph` SDK v5.46.0 |
| Authentication | `Microsoft.Identity.Client` (MSAL.NET) — OAuth2 + PKCE, MFA support |
| Token Cache | `Microsoft.Identity.Client.Extensions.Msal` — Windows DPAPI, macOS Keychain, Linux Secret Service |
| Styling | Custom XAML theme mimicking ADUC + Windows 11 admin-tool aesthetics |

---

## Screenshots

*Coming soon.*

---

## Getting Started

### Prerequisites
- Windows 10/11 with .NET 8 runtime
- Visual Studio 2022+ (optional, for development)
- Access to an Active Directory domain (optional — demo data works without AD)

### Build & Run

```bash
cd HybridADManager
dotnet restore
dotnet build
dotnet run
```

### Authentication Setup

Before Entra ID features work, register an application in Azure AD:

1. Go to [Azure Portal](https://portal.azure.com) → Microsoft Entra ID → App registrations
2. Click **New registration**
3. Name it `HybridAD-Manager`
4. Supported account types: **Accounts in this organizational directory only**
5. Redirect URI: **Public client/native (mobile & desktop)** → `http://localhost`
6. Click **Register** and note the **Application (client) ID**
7. Go to **API Permissions** → Add:
   - `User.Read.All`
   - `Group.Read.All`
   - `Directory.Read.All`
   - `Organization.Read.All`
8. Click **Grant admin consent**

Then update `AuthenticationSettings.ClientId` in `Services/IAuthenticationService.cs` with your app's client ID.

> **Note:** The codebase currently ships with the **Microsoft Graph Explorer client ID** as a placeholder for testing. Replace this before production use.

---

## Architecture

```
HybridADManager/
├── App.xaml / App.xaml.cs              # Application entry point + DI container
├── MainWindow.xaml                     # Shell
├── Models/
│   ├── DirectoryObject.cs              # Base observable model
│   ├── HybridUser.cs / HybridGroup.cs  # Unified AD + Cloud models
│   ├── DirectoryNode.cs                # Tree node model
│   └── SyncStatus.cs                   # Sync state model
├── Services/
│   ├── IActiveDirectoryService.cs      # AD operations interface
│   ├── ActiveDirectoryService.cs       # System.DirectoryServices impl
│   ├── IAuthenticationService.cs       # MSAL auth interface
│   ├── AuthenticationService.cs        # OAuth2 + PKCE + token cache
│   ├── IGraphService.cs                # Graph operations interface
│   └── GraphService.cs                 # Microsoft Graph SDK v5
├── ViewModels/                         # MVVM ViewModels (CommunityToolkit.Mvvm)
├── Views/                              # XAML Views
└── Infrastructure/
    ├── Converters/                     # Value converters
    └── Themes/                         # Colors, styles, data templates
```

### Data Flow
1. `DirectoryTreeViewModel` loads domain/OU tree (AD first, demo fallback)
2. `ObjectListViewModel` loads objects for the selected node (AD first, demo fallback)
3. When authenticated, objects are correlated with Graph by UPN to populate cloud metadata
4. Double-click / Properties opens the 13-tab property sheet
5. `HybridStatusTabViewModel` loads live sync state lazily when the tab is opened

---

## Development Status

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ Complete | Foundation & Shell |
| Phase 2 | ✅ Complete | Active Directory Integration |
| Phase 3 | ✅ Complete | Entra ID Integration |
| Phase 4 | ✅ Complete | Cloud Management Tabs (Licenses, Mailbox, Email Addresses) |
| Phase 5 | ✅ Complete | Search / Saved Queries, Bulk Operations, Force Sync, Export |
| Phase 6 | 📋 Planned | Keyboard Shortcuts, Drag-and-Drop, Accessibility, MSIX Packaging |

---

## Security

- **No credentials are stored in code or configuration files**
- Uses MSAL.NET with OAuth2 + PKCE (public client flow)
- Supports MFA
- Tokens are cached via platform-specific secure storage
- Per-admin individual credentials (actions traceable in unified audit log)

---

## Contributing

This project is in early development. Contributions, feedback, and bug reports are welcome!

---

## License

*To be determined.*

---

## Acknowledgments

Inspired by the familiar experience of **Active Directory Users & Computers** and tools like **Easy365Manager**.
