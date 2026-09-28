# TB-TMAR-HOST-ADMIN-CANON-008 — Structure Map

## Before

```
src/backend/Host/Tooba.Host/Admin/                (flat root, 15 .cs)
├── AdminDevActorBootstrap.cs
├── AdminGridQueryEndpoint.cs
├── AdminPanelAccess.cs
├── AdminPanelComposer.cs
├── AdminPanelEndpoints.cs
├── AdminPanelModels.cs
├── HostAdminPanelAccess.cs
├── HostOrderAdminAuthorizer.cs
├── HostOrderAdminEffectiveAccessReader.cs
├── HostPaymentAdminAuthorizer.cs
├── HostPromotionAdminAuthorizer.cs
├── HostReturnAdminAuthorizer.cs
├── HostSettlementAdminAuthorizer.cs
├── HostSupportAdminAuthorizer.cs
└── HostWalletAdminAuthorizer.cs
```

## After

```
src/backend/Host/Tooba.Host/Admin/                (root flat .cs = 0)
├── Access/
│   ├── AdminPanelAccess.cs                       ns Tooba.Host.Admin.Access
│   └── HostAdminPanelAccess.cs                   ns Tooba.Host.Admin.Access
│   └── Authorizers/
│       ├── HostOrderAdminAuthorizer.cs           ns Tooba.Host.Admin.Access.Authorizers
│       ├── HostOrderAdminEffectiveAccessReader.cs ns Tooba.Host.Admin.Access.Authorizers
│       ├── HostPaymentAdminAuthorizer.cs         ns Tooba.Host.Admin.Access.Authorizers
│       ├── HostPromotionAdminAuthorizer.cs       ns Tooba.Host.Admin.Access.Authorizers
│       ├── HostReturnAdminAuthorizer.cs          ns Tooba.Host.Admin.Access.Authorizers
│       ├── HostSettlementAdminAuthorizer.cs      ns Tooba.Host.Admin.Access.Authorizers
│       ├── HostSupportAdminAuthorizer.cs         ns Tooba.Host.Admin.Access.Authorizers
│       └── HostWalletAdminAuthorizer.cs          ns Tooba.Host.Admin.Access.Authorizers
├── Panel/
│   ├── AdminPanelComposer.cs                     ns Tooba.Host.Admin.Panel
│   ├── AdminPanelEndpoints.cs                    ns Tooba.Host.Admin.Panel
│   └── AdminPanelModels.cs                       ns Tooba.Host.Admin.Panel
├── Grid/
│   └── AdminGridQueryEndpoint.cs                 ns Tooba.Host.Admin.Grid
└── Development/
    └── AdminDevActorBootstrap.cs                 ns Tooba.Host.Admin.Development
```

Observed on disk (verified by `HostAdminCanon008GuardTests`):

```
Admin/Access/AdminPanelAccess.cs | Tooba.Host.Admin.Access
Admin/Access/HostAdminPanelAccess.cs | Tooba.Host.Admin.Access
Admin/Access/Authorizers/HostOrderAdminAuthorizer.cs | Tooba.Host.Admin.Access.Authorizers
Admin/Access/Authorizers/HostOrderAdminEffectiveAccessReader.cs | Tooba.Host.Admin.Access.Authorizers
Admin/Access/Authorizers/HostPaymentAdminAuthorizer.cs | Tooba.Host.Admin.Access.Authorizers
Admin/Access/Authorizers/HostPromotionAdminAuthorizer.cs | Tooba.Host.Admin.Access.Authorizers
Admin/Access/Authorizers/HostReturnAdminAuthorizer.cs | Tooba.Host.Admin.Access.Authorizers
Admin/Access/Authorizers/HostSettlementAdminAuthorizer.cs | Tooba.Host.Admin.Access.Authorizers
Admin/Access/Authorizers/HostSupportAdminAuthorizer.cs | Tooba.Host.Admin.Access.Authorizers
Admin/Access/Authorizers/HostWalletAdminAuthorizer.cs | Tooba.Host.Admin.Access.Authorizers
Admin/Development/AdminDevActorBootstrap.cs | Tooba.Host.Admin.Development
Admin/Grid/AdminGridQueryEndpoint.cs | Tooba.Host.Admin.Grid
Admin/Panel/AdminPanelComposer.cs | Tooba.Host.Admin.Panel
Admin/Panel/AdminPanelEndpoints.cs | Tooba.Host.Admin.Panel
Admin/Panel/AdminPanelModels.cs | Tooba.Host.Admin.Panel
```

Counters:

```
Host/Admin root flat .cs      = 0
Host/Admin recursive  .cs     = 15
old flat paths still present  = 0
files still at "namespace Tooba.Host.Admin;" = 0
compatibility wrappers/shim   = 0
```

## Moves

All 15 moves were performed with `git mv` (rename detection preserved, no copy/duplicate).
Content diff for the 15 files is limited to the `namespace` line plus the intra-Admin `using`
lines that the new namespaces require.
