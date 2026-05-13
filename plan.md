# HybridAD-Manager — Development Plan

## Overview
Build a **standalone Windows WPF application** that provides a single pane of glass for managing **Hybrid Active Directory + Entra ID** environments. The UI is modeled after the familiar **Active Directory Users & Computers (dsa.msc)** console, extended with cloud-native tabs for Microsoft 365 / Entra ID management — inspired by Easy365Manager.

---

## Tech Stack
| Layer | Technology |
|-------|------------|
| Framework | .NET 8 WPF (Windows-only) |
| UI Pattern | MVVM with `CommunityToolkit.Mvvm` |
| AD Connectivity | `System.DirectoryServices` / `System.DirectoryServices.AccountManagement` |
| Entra ID / Graph | `Microsoft.Graph` SDK v5+ |
| Authentication | `Microsoft.Identity.Client` (MSAL.NET) — OAuth2 + PKCE, supports MFA |
| Styling | Custom XAML theme mimicking ADUC + Windows 11 admin-tool aesthetics |
| Async / Threading | `IAsyncEnumerable`, `Task`, background loading with progress indicators |

---

## Application Architecture

```
HybridADManager/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml              # Shell: Menu, Toolbar, Tree (left), List (right), StatusBar
│
├── Views/
│   ├── DirectoryTreeView.xaml   # Left-pane domain/OU tree
│   ├── ObjectListView.xaml      # Right-pane details list (users/groups/computers)
│   ├── SearchPane.xaml          # Global find / LDAP query builder
│   ├── PropertySheets/          # Multi-tab property dialogs
│   │   ├── UserPropertySheet.xaml
│   │   ├── GroupPropertySheet.xaml
│   │   ├── ComputerPropertySheet.xaml
│   │   └── Tabs/
│   │       ├── GeneralTab.xaml
│   │       ├── AccountTab.xaml
│   │       ├── OrganizationTab.xaml
│   │       ├── MemberOfTab.xaml
│   │       ├── HybridStatusTab.xaml      # NEW: Sync state, Object ID, last sync
│   │       ├── LicensesTab.xaml          # NEW: M365 license & service-plan picker
│   │       ├── MailboxTab.xaml           # NEW: Aliases, delegation, auto-reply, forwarding
│   │       └── EmailAddressesTab.xaml    # NEW: proxyAddresses, targetAddress
│   ├── Dialogs/
│   │   ├── NewUserDialog.xaml
│   │   ├── NewGroupDialog.xaml
│   │   ├── ResetPasswordDialog.xaml
│   │   └── MoveObjectDialog.xaml
│   └── Controls/
│       ├── HybridBadge.xaml              # Cloud-sync status indicator
│       ├── LicensePicker.xaml            # M365 license grid
│       └── BusyOverlay.xaml              # Loading spinner overlay
│
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── DirectoryTreeViewModel.cs
│   ├── ObjectListViewModel.cs
│   ├── PropertySheetViewModelBase.cs
│   └── (Tab VMs…)
│
├── Models/
│   ├── DirectoryObject.cs               # Base: Name, DN, ObjectClass, AD + Cloud IDs
│   ├── HybridUser.cs                    # Unified AD user + Graph user
│   ├── HybridGroup.cs                   # Unified AD group + Graph group
│   ├── HybridComputer.cs
│   ├── OrganizationalUnit.cs
│   ├── DomainNode.cs
│   ├── M365License.cs                   # SKU + service plan definitions
│   └── SyncStatus.cs                    # SyncState enum + metadata
│
├── Services/
│   ├── IActiveDirectoryService.cs       # LDAP queries, CRUD
│   ├── ActiveDirectoryService.cs
│   ├── IGraphService.cs                 # Entra ID / Graph CRUD
│   ├── GraphService.cs
│   ├── IAuthenticationService.cs        # MSAL token acquisition + caching
│   ├── AuthenticationService.cs
│   ├── ISyncMonitorService.cs           # Azure AD Connect sync status (reads sync meta)
│   └── NavigationService.cs             # Dialog/pane coordination
│
└── Infrastructure/
    ├── Converters/                      # Bool→Visibility, SyncState→Brush, etc.
    ├── Behaviors/                       # Double-click → properties, etc.
    ├── Helpers/
    │   ├── LdapFilterBuilder.cs
    │   ├── ProxyAddressValidator.cs
    │   └── PasswordGenerator.cs
    └── Themes/
        ├── ADUCStyles.xaml              # TreeView, ListView, ToolBar, Menu styles
        ├── Colors.xaml                  # Mica/Acrylic-friendly palette
        └── DataTemplates.xaml           # Object→UI template mappings
```

---

## Core UI Layout (ADUC-Style Shell)

```
┌─────────────────────────────────────────────────────────────────────┐
│  File   Action   View   Window   Help                               │  ← Menu
├─────────────────────────────────────────────────────────────────────┤
│  [New▼] [🔍Find] [⬆] [⬇] [Delete] [Refresh] [Properties] [Help]   │  ← Toolbar
├──────────────────┬──────────────────────────────────────────────────┤
│                  │                                                  │
│  🖥 contoso.com   │  Name          Type      Email          Sync    │
│  ├─ Builtin      │  ─────────────────────────────────────────────  │
│  ├─ Computers    │  Alice Alisson  User   alice@co...   ✅ Hybrid │
│  ├─ Domain Cont..│  Bob Builder    User   bob@co...     ⏳ Pending│
│  ├─ Users        │  IT Support     Group  it@co...      ✅ Hybrid │
│  │   └─ …        │  …                                                    │
│  └─ 🏢 Custom OU │                                                  │
│      └─ …        │                                                  │
│                  │                                                  │
│  📌 Saved Queries│                                                  │
│                  │                                                  │
└──────────────────┴──────────────────────────────────────────────────┘
│  Ready    3 objects    Domain: contoso.com    Logged in: admin@…   │  ← StatusBar
└─────────────────────────────────────────────────────────────────────┘
```

**Left Pane (Tree):**
- Expandable domain root with standard containers (Builtin, Computers, Domain Controllers, ForeignSecurityPrincipals, Managed Service Accounts, Users)
- All custom OUs discovered recursively
- Saved Queries node with user-defined LDAP filters
- Icons reflect object type + hybrid state overlay

**Right Pane (List):**
- Details view with configurable columns
- Default columns: Name, Type, Description, Email / UPN, Hybrid Sync Status
- Sorting, multi-select, context menu
- Double-click opens Properties sheet

---

## Property Sheet Tabs

### Standard AD Tabs (familiar to every admin)
| Tab | Contents |
|-----|----------|
| **General** | First/Last name, Display name, Description, Office, Telephone, Email, Web page, Hybrid sync badge |
| **Address** | Street, City, State, ZIP, Country |
| **Account** | UPN, logon name, Account options (unlock, expire, require password change), SIDs |
| **Profile** | Profile path, Logon script, Home folder |
| **Telephones** | Home, Pager, Mobile, Fax, IP phone, Notes |
| **Organization** | Title, Department, Company, Manager, Direct reports |
| **Member Of** | AD group memberships (with Add/Remove) |
| **Dialin** | Network Access Permission, Callback options |
| **Environment** | Starting program, Client devices |

### Hybrid / Cloud Tabs (new)
| Tab | Contents |
|-----|----------|
| **Hybrid Status** | Entra Object ID, Immutable ID, Sync status (In Sync / Pending / Cloud-only / Sync Error), Last sync time, Directory source (Windows Server AD / Cloud), Force sync button |
| **Licenses** | Visual grid of available M365 SKUs with expand-down service plans; checkboxes to assign/remove; usage counters (used / available) |
| **Mailbox** | Mailbox type (User / Shared / Room / Equipment), Alias list, Primary SMTP, Calendar permissions editor, Delegation (Send-As, Send-on-Behalf, Full Access), Auto-reply (OOO) editor, Mail forwarding, Hide from address lists, Archive settings, Mailbox usage bar |
| **Email Addresses** | `proxyAddresses` editor with SMTP/SIP/X500 type indicators, uniqueness & format validation, targetAddress (remote routing) field |

---

## Key Features

### 1. Unified Hybrid Object Model
- Every user/group fetched from AD attempts a **correlated Graph lookup** by `immutableId` or `onPremisesSecurityIdentifier` / `userPrincipalName`
- Display a single row with both on-prem and cloud metadata
- **Lazy loading**: cloud tabs load async only when opened; list view shows cached sync state

### 2. Visual Sync Indicators
- **✅ In Sync** — AD object has matching Entra object, last sync recent
- **⏳ Pending** — AD change detected, awaiting next AAD Connect sync cycle
- **☁️ Cloud-only** — No on-prem counterpart (e.g., guest, cloud-only admin account)
- **❌ Sync Error** — Directory sync error detected (tooltip shows error detail)
- **🔄 Force Sync** — One-click trigger of Azure AD Connect delta sync (via remote PowerShell or local `Start-ADSyncSyncCycle` if run on sync server)

### 3. License Management
- Enumerate tenant SKUs (`subscribedSkus`) — display friendly names (e.g., *Microsoft 365 E3*)
- Expand each SKU to show service plans (Exchange Online, Teams, etc.)
- Assign / remove licenses with conflict detection
- Show tenant-level usage counts

### 4. Mailbox & Calendar Management
- Edit `proxyAddresses` with validation (must be unique in tenant, valid SMTP format)
- Set primary / alias / remove
- Calendar permissions editor (replace PowerShell `Set-MailboxFolderPermission`)
- Mailbox delegation with auto-mapping toggle
- Out-of-office configuration with rich-text / scheduling

### 5. Search & Saved Queries
- **Quick Search**: Filter current container by name
- **Find Dialog**: Global LDAP search across domain (name, email, description, phone)
- **Saved Queries**: Persist custom LDAP filters; appear in left tree under "Saved Queries"

### 6. Bulk Operations
- Multi-select objects in list view → right-click → bulk actions
- Bulk enable/disable, move, delete, reset password
- Bulk license assignment / removal
- Bulk proxy address edits

### 7. Security & Authentication
- MSAL.NET OAuth2 with PKCE (supports MFA)
- Token cached via Windows DPAPI in user profile (same security model as Easy365Manager)
- Per-admin individual credentials (actions traceable in unified audit log)
- No credentials stored in code or config

---

## Development Phases

### Phase 1 — Foundation & Shell
- [ ] Create .NET 8 WPF solution and project structure
- [ ] Implement ADUC-style MainWindow shell (Menu, Toolbar, Tree, List, StatusBar, Splitter)
- [ ] Style system: `ADUCStyles.xaml`, icons, colors, typography matching Windows admin tools
- [ ] `DirectoryTreeView` with dummy data + expand/collapse
- [ ] `ObjectListView` with columns and selection

### Phase 2 — Active Directory Integration
- [ ] `ActiveDirectoryService`: domain enumeration, OU tree walking, object listing
- [ ] Object type icons (user, group, computer, OU, contact)
- [ ] Context menus (New, Delete, Rename, Move, Refresh, Properties)
- [ ] Basic Property Sheet shell with tab control
- [ ] Implement standard AD tabs: General, Account, Address, Organization, Member Of

### Phase 3 — Entra ID Integration
- [ ] MSAL auth flow + token caching (`AuthenticationService`)
- [ ] `GraphService`: user/group lookups, license enumeration
- [ ] Correlate AD objects with Graph objects
- [ ] **Hybrid Status** tab
- [ ] Sync state detection & badge overlays

### Phase 4 — Cloud Management Tabs
- [ ] **Licenses** tab with SKU/service-plan grid
- [ ] **Mailbox** tab: aliases, delegation, auto-reply UI
- [ ] **Email Addresses** tab with `proxyAddresses` editor + validation
- [ ] Group cloud tab: type, visibility, external senders, owners

### Phase 5 — Advanced Features
- [ ] Search / Find dialog with LDAP builder
- [ ] Saved Queries persistence (JSON settings file)
- [ ] Bulk operations framework
- [ ] Force sync integration (AAD Connect)
- [ ] Export / reporting (CSV)

### Phase 6 — Polish
- [ ] Keyboard shortcuts (F5 refresh, F3 find, Enter properties, Delete)
- [ ] Drag-and-drop move between OUs
- [ ] Accessibility labels & high-contrast theme support
- [ ] Error handling & user-friendly message dialogs
- [ ] Installer / MSIX packaging setup

---

## Design Principles
1. **Familiarity First** — Every admin already knows ADUC; minimize relearning.
2. **Progressive Disclosure** — Cloud data loads only on demand; standard AD is instant.
3. **Single Pane of Glass** — No context switching between ADUC, M365 Admin Center, and Exchange Admin Center.
4. **Safety** — Confirm destructive actions; validate before Graph writes; show preview where possible.
5. **Performance** — Virtualized lists, async Graph calls, background tree population.

---

## Out of Scope (for MVP)
- Group Policy management
- Certificate / PKI management
- Exchange on-premises mailbox management (cloud-only / hybrid sync model)
- Detailed audit log viewer (link to M365 portal instead)
- Multi-forest support (single domain MVP)
