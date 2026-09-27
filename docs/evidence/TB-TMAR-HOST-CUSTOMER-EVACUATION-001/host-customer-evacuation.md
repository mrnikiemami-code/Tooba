# Host/Customer evacuation — Migrate + Certify

Task family: `TB-TMAR-HOST-CUSTOMER-EVACUATION-001`
Skills: analyze → migrate → certify
Commit parent: `d1646717`

## Migrate summary

### Content Disposition Map → destinations

| Host file | Disposition | Destination |
| --- | --- | --- |
| `HostOrderCustomerAuthorizer.cs` | REHOMED + DELETED | `Order.Endpoints/Customer/OrderCustomerAuthorizer.cs` |
| `HostNotificationCustomerAuthorizer.cs` | REHOMED + DELETED | `Notification.Endpoints/Customer/NotificationCustomerAuthorizer.cs` |
| `HostReturnCustomerAuthorizer.cs` | REHOMED + DELETED | `Returns.Endpoints/Customer/ReturnCustomerAuthorizer.cs` |
| `HostSupportCustomerAuthorizer.cs` | REHOMED + DELETED | `Support.Endpoints/Customer/SupportCustomerAuthorizer.cs` |
| `HostWalletCustomerAuthorizer.cs` | REHOMED + DELETED | `Wallet.Endpoints/Customer/WalletCustomerAuthorizer.cs` |
| `CustomerPanelComposer.cs` | KEEP (Host BFF) | Host — CustomerProfile.Endpoints foundation deferred |
| `CustomerPanelEndpoints.cs` | KEEP + repaired | Guest → `StorefrontGuestActor`; unauthorized → `ApiResponseFactory` |
| `CustomerPanelModels.cs` | KEEP (Host BFF) | Host |

### Semantics preserved per authorizer

- **Order / Notification**: authenticated → Dev/Testing header → guest fallback (`StorefrontGuestActor.ActorId`)
- **Returns**: authenticated → Dev/Testing header; **no guest**
- **Support / Wallet**: authenticated → **Development-only** header; **no guest**

### DI

- Registered in `AddOrderEndpointPresentation`, `AddNotificationEndpointPresentation`, `AddSupportEndpointPresentation`, `AddWalletEndpointPresentation`, new `AddReturnEndpointPresentation`
- Host `Program.cs` no longer binds Host*CustomerAuthorizer types
- All authorizers depend on `ICurrentAuthenticatedUser` + `IHostEnvironment` (no Host types)

### Contracts boundary

- Guest Guid authority: `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor` (Notification.Endpoints + Order.Endpoints ProjectReference)
- No new Application↔Application edges
- No schema/migration change
- Routes unchanged

## Certify (Host/Customer folder slice)

| Check | State |
| --- | --- |
| Host Customer module-specific authorizer files | **ZERO** |
| Module-owned customer authorizers present | **5/5** |
| Host → module authorizer DI | **NONE** (module presentation owns registration) |
| Panel BFF residual | **DOCUMENTED_DEBT** (CustomerProfile/Wishlist foundations) |
| API failure path for panel | `ApiResponseFactory.FromFailure(SemanticError)` |
| Machine code `customer.session.required` | Foundation-owned catalog (unchanged) |
| Behavior / routes / schema | **UNCHANGED** |

### Focused validation

| Suite | Result |
| --- | --- |
| Host build | PASS |
| Notification Architecture | Passed 7 / Failed 0 |
| Returns Architecture | Passed 3 / Failed 0 |
| Support Architecture | Passed 3 / Failed 0 |
| Wallet Architecture | Passed 8 / Failed 0 |
| Host CustomerPanel + CustomerProfile + Order reverse-audit + WalletFoundation + ErrorCatalogUnique | Passed 25 / Failed 0 / Skipped 4 |

### Remaining Host/Customer debt (next bounded tasks)

1. CustomerProfile.Endpoints foundation + move `/profile` (+ optional dashboard composition via Contracts ports)
2. Wishlist Contracts + Endpoints (remove Host Wishlist HTTP + Application port from Host composer)
3. Hard-coded Persian display fallbacks (`مشتری توبا`) → localization when CustomerProfile presentation stack exists

Folder checkpoint: **Customer authorizer evacuation COMPLETE**; panel BFF retained intentionally.
