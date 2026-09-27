# Host/Customer — Architecture Analyze (skill 1)

Target: `src/backend/Host/Tooba.Host/Customer/` (8 production files)
Skills: `tooba-architecture-analyze` → migrate → certify
Mode: BACKEND_ONLY

## Inventory

| File | LOC ~ | Responsibility |
| --- | --- | --- |
| `CustomerPanelComposer.cs` | ~125 | Cross-module BFF: dashboard/profile composition (Wishlist + AddressBook + CustomerProfile + Identity contact + Order summary DTO) |
| `CustomerPanelEndpoints.cs` | ~160 | HTTP `/v1/customer/{dev-context,dashboard,profile}` + session/dev actor resolution |
| `CustomerPanelModels.cs` | ~40 | Host presentation DTOs for dashboard/profile pages |
| `HostOrderCustomerAuthorizer.cs` | ~55 | Order-specific customer actor seam (`IOrderCustomerAuthorizer`) |
| `HostNotificationCustomerAuthorizer.cs` | ~35 | Notification-specific customer actor seam |
| `HostReturnCustomerAuthorizer.cs` | ~30 | Returns-specific customer actor seam |
| `HostSupportCustomerAuthorizer.cs` | ~30 | Support-specific customer actor seam |
| `HostWalletCustomerAuthorizer.cs` | ~30 | Wallet-specific customer actor seam |

## Structured state

1. **Foundation-State**
   - Order: `FOUNDATION_READY` (Endpoints + CQRS certified)
   - Notification/Returns/Support/Wallet: `FOUNDATION_PARTIAL` (Endpoints exist; customer authorizer still Host-owned)
   - CustomerProfile: `FOUNDATION_PARTIAL` (Contracts/Domain/Infra exist; **no Endpoints/CQRS**)
   - Wishlist: `FOUNDATION_PARTIAL` (no Contracts/Endpoints; Host still owns HTTP)
2. **Ownership-State**: `MUST_SPLIT` (folder mixes module authorizers + cross-module BFF panel)
3. **File-Cohesion-State**: `COHESIVE` per file (each authorizer is single-purpose; panel split across 3 cohesive files)
4. **Oversized/God-File-State**: none
5. **Localization-State**: `HARDCODED_TEXT` (`"مشتری توبا"`, `"مشتری آزمایشی فروشگاه"` in panel)
6. **API-Result-Pattern-State**: `RAW_RESULTS` (panel uses `Results.Json` for success + ad-hoc unauthorized envelope)
7. **Stable-Error-Code-State**: `CATALOGUED` for `customer.session.required` (Foundation-owned descriptor)
8. **Logging-State**: `CANONICAL` (no non-standard logging)
9. **Sensitive-Logging-State**: `NONE`
10. **OpenTelemetry-State**: `CANONICAL`
11. **Correlation-Trace-State**: `CANONICAL`
12. **CQRS-State**: Panel uses `ISender` for Order summary (`COMPLIANT` for Order path); profile write bypasses MediatR (`MISSING` for CustomerProfile)
13. **Validator-Coverage-State**: `NO_VALIDATOR_REQUIRED` for current panel shapes (session-gated; profile write validated in directory)
14. **Contracts-Boundary-State**: `VIOLATION`
    - Authorizers (Notification/Order Host) reference `Order.Application` guest constant
    - Composer references `AddressBook.Application.Ports`, `Wishlist.Application`, `Order.Application.Customer.Models`
15. **Cross-Module-Coupling-State**: `ILLEGAL` (Host→foreign Application) for guest actor + directory ports above; Identity/CustomerProfile Contracts usage is legal
16. **Cross-Module-Join-State**: `NONE`
17. **Persistence-Ownership-State**: `CORRECT` (no DbContext in folder)
18. **Endpoint-Ownership-State**: `HOST_OWNED` for panel; module routes for Order/Notification/Returns/Support/Wallet already module-owned (authorizer only residual)
19. **Host-Residue-State**
    - `MODULE_SPECIFIC_AUTHORIZER` ×5 → evacuate
    - `CROSS_MODULE_BFF_COMPOSITION` (panel) → keep Host until CustomerProfile/Wishlist endpoint foundations exist
    - `DEV_PLATFORM_SEAM` (`/dev-context`, `X-Tooba-Dev-Actor-User-Id`) → Host-allowed
20. **Schema-Migration-State**: `UNCHANGED`
21. **Behavior-Preservation-Risk**: `LOW` for authorizer rehome (Fulfillment precedent); `MEDIUM` for panel API-result repair if envelope changes — preserve success DTO shape; only canonicalize failure path
22. **Canonical-Reference-Used**: Fulfillment Host evacuation (`ICurrentAuthenticatedUser` + `StorefrontGuestActor` + module `Add*EndpointPresentation`); AddressBook actor resolver; `HOST-MODULE-ENDPOINT-001`; Foundation `customer.session.required`
23. **Final-Disposition**: `READY_TO_MIGRATE` (bounded: evacuate 5 customer authorizers + repair panel guest/error path; **do not** invent CustomerProfile.Endpoints in this slice)

## Content Disposition Map

### HostOrderCustomerAuthorizer → `Order.Endpoints.Customer.OrderCustomerAuthorizer`
- Keep exact semantics: authenticated session → Dev/Testing header → guest fallback
- Error: `CustomerOrderErrors.SessionRequired`
- Guest authority: `StorefrontGuestActor.ActorId` (Contracts; remove Application constant dependency from Host)
- Register in `AddOrderEndpointPresentation`

### HostNotificationCustomerAuthorizer → `Notification.Endpoints.Customer.NotificationCustomerAuthorizer`
- Same guest-fallback semantics as today
- Register in `AddNotificationEndpointPresentation`
- Add `Order.Contracts` ProjectReference for `StorefrontGuestActor` only

### HostReturnCustomerAuthorizer → `Returns.Endpoints.Customer.ReturnCustomerAuthorizer`
- Preserve: authenticated OR Dev/Testing header; **no guest fallback**
- Add `AddReturnEndpointPresentation` + Host call

### HostSupportCustomerAuthorizer → `Support.Endpoints.Customer.SupportCustomerAuthorizer`
- Preserve: authenticated OR **Development-only** header (not Testing); **no guest fallback**

### HostWalletCustomerAuthorizer → `Wallet.Endpoints.Customer.WalletCustomerAuthorizer`
- Preserve: authenticated OR **Development-only** header; **no guest fallback**

### CustomerPanelComposer / Endpoints / Models
- **KEEP** in Host as temporary cross-module BFF (CustomerProfile.Endpoints + Wishlist Contracts missing)
- Repair: guest → `StorefrontGuestActor.ActorId`; unauthorized → `ApiResponseFactory.FromFailure(SemanticError)`
- Do **not** start Wishlist/CustomerProfile independent recovery here

## Why Foundation is not the panel owner
Panel is not a platform auth/session boundary; it is customer-facing composition. Foundation ownership of dashboard/profile would be convenience centralization. Natural eventual owner is CustomerProfile (+ Contracts ports), blocked by missing Endpoints foundation → deferred bounded task.

## Migration order
1. Create 5 module authorizers + DI registration
2. Remove Host authorizer files + Program.cs Host bindings
3. Update module architecture guards (expect module-owned, Host absent)
4. Repair panel guest/error path
5. Update Order reverse-audit inventory + Host tests
6. Focused validation → certify Host/Customer authorizer residue ZERO

## Certification blockers (post-migrate)
- Panel still Host-owned BFF (documented debt)
- Hard-coded Persian display fallbacks
- Host→AddressBook.Application / Wishlist.Application directory ports (Host composition debt until Contracts ports exist)
- Stale inventory previously listed deleted `HostFulfillmentCustomerAuthorizer.cs`
