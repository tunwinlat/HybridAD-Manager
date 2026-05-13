# HybridAD-Manager

## Phase 1, 2 & 3 Complete — Foundation, AD Integration & Entra ID

### What Was Built

#### Phase 1 — Foundation & Shell
- .NET 8 WPF solution with full MVVM architecture using `CommunityToolkit.Mvvm`
- ADUC-style main window: Menu, Toolbar, Tree/List split pane, Status Bar
- Full styling system mimicking Windows admin tools
- Dummy hybrid data with sync status indicators

#### Phase 2 — Active Directory Integration
- `IActiveDirectoryService` / `ActiveDirectoryService` — Full `System.DirectoryServices` implementation
- Domain/OU enumeration, object retrieval, group memberships, cloud sync detection
- 13-tab property sheet with General, Address, Account, Organization, Member Of
- Graceful fallback to demo data when AD is unavailable

#### Phase 3 — Entra ID Integration

**Authentication Service:**
- `IAuthenticationService` / `AuthenticationService` — MSAL.NET OAuth2 + PKCE
- Supports MFA via Microsoft identity platform
- Token caching via Windows DPAPI (`Microsoft.Identity.Client.Extensions.Msal`)
- Cross-platform cache support (Windows DPAPI, macOS Keychain, Linux Secret Service)
- Sign-in / Sign-out with state events

**Graph Service:**
- `IGraphService` / `GraphService` — Microsoft Graph SDK v5 implementation
- User lookup by UPN or `immutableId`
- Group lookup by display name
- Cloud-only user/group enumeration
- License SKU enumeration with friendly names (E3, E5, etc.)
- License assignment/removal
- Hybrid sync state retrieval with provisioning error detection
- Force sync placeholder (requires AAD Connect server access)

**Hybrid Status Tab (Live):**
- Visual sync status badge with color coding
- Entra Object ID display
- Immutable ID (Base64 encoded)
- Directory source indicator (AD-synced vs Cloud-only)
- On-premises metadata: DN, Domain, SAM, SID
- **Sync error panel** — shows provisioning errors with error codes
- **Force Sync button** — triggers Azure AD Connect delta sync

**Main Window Updates:**
- Sign-in button in toolbar showing current auth state
- Status bar shows Entra username when authenticated
- Automatic token silent refresh
- Busy overlay during authentication

**DI Container:**
- `Microsoft.Extensions.DependencyInjection` for service registration
- `App.Services` provider accessible throughout the app
- Services injected into ViewModels

### Architecture
```
HybridADManager/
├── App.xaml / App.xaml.cs                    # DI container setup
├── MainWindow.xaml                           # Shell + auth button
├── Models/
│   ├── DirectoryObject.cs
│   ├── DirectoryNode.cs
│   └── SyncStatus.cs
├── Services/
│   ├── IActiveDirectoryService.cs            # AD operations interface
│   ├── ActiveDirectoryService.cs             # DirectoryServices impl
│   ├── IAuthenticationService.cs             # MSAL auth interface
│   ├── AuthenticationService.cs              # OAuth2 + PKCE + token cache
│   ├── IGraphService.cs                      # Graph operations interface
│   └── GraphService.cs                       # Microsoft Graph SDK v5
├── ViewModels/
│   ├── MainWindowViewModel.cs                # Auth state management
│   ├── DirectoryTreeViewModel.cs
│   ├── ObjectListViewModel.cs                # Entra correlation on load
│   ├── PropertySheetViewModelBase.cs
│   └── Tabs/
│       ├── GeneralTabViewModel.cs
│       └── HybridStatusTabViewModel.cs       # NEW: Live sync state
├── Views/
│   ├── DirectoryTreeView.xaml
│   ├── ObjectListView.xaml
│   └── PropertySheets/
│       ├── UserPropertySheet.xaml            # 13 tabs
│       └── Tabs/
│           ├── GeneralTab.xaml
│           ├── AccountTab.xaml
│           ├── AddressTab.xaml
│           ├── OrganizationTab.xaml
│           ├── MemberOfTab.xaml
│           └── HybridStatusTab.xaml          # NEW: Full sync details
└── Infrastructure/
    ├── Converters/
    └── Themes/
```

### Package Dependencies
| Package | Version | Purpose |
|---------|---------|---------|
| CommunityToolkit.Mvvm | 8.2.2 | MVVM source generators |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | DI container |
| Microsoft.Identity.Client | 4.60.3 | MSAL.NET authentication |
| Microsoft.Identity.Client.Extensions.Msal | 4.60.3 | Token cache helpers |
| Microsoft.Graph | 5.46.0 | Graph API SDK |
| System.DirectoryServices | 8.0.0 | Active Directory |
| System.DirectoryServices.AccountManagement | 8.0.0 | AD account management |

### How to Build
```bash
cd HybridAD-Manager/HybridADManager
dotnet restore
dotnet build
dotnet run
```

### Authentication Setup
Before signing in, you need to register an application in Azure AD:

1. Go to [Azure Portal](https://portal.azure.com) → Microsoft Entra ID → App registrations
2. Click **New registration**
3. Name it `HybridAD-Manager`
4. Supported account types: **Accounts in this organizational directory only**
5. Redirect URI: **Public client/native (mobile & desktop)** → `http://localhost`
6. Click **Register**
7. Note the **Application (client) ID**
8. Go to **API Permissions** → Add:
   - `User.Read.All`
   - `Group.Read.All`
   - `Directory.Read.All`
   - `Organization.Read.All`
9. Click **Grant admin consent**

Then update `AuthenticationSettings.ClientId` in `Services/IAuthenticationService.cs` with your app's client ID.

### Behavior
- **With AD**: Discovers domain, enumerates OUs, loads real objects
- **With Entra auth**: Object list correlates AD users with Graph data; Hybrid Status tab shows live sync state
- **Without AD**: Demo `contoso.com` data for UI exploration
- **Without Entra auth**: Hybrid Status tab shows "Not signed in" placeholder

### Next Phase
**Phase 4 — Cloud Management Tabs**
- **Licenses tab**: Visual SKU/service-plan grid with assign/remove
- **Mailbox tab**: Aliases, delegation, auto-reply, forwarding
- **Email Addresses tab**: `proxyAddresses` editor with validation
- Group cloud tab: type, visibility, external senders, owners
