# TB-TMAR-NOTIFICATION-AMSC-001-W3-R3 — boundary-audit

Cross-module boundary / Host / persistence audit re-verified live at HEAD `028ef769` (all five
production projects + Host).

## 1. Project-reference graph (exhaustive, from `.csproj`)

| Project | References |
| --- | --- |
| `Tooba.Notification.Contracts` | `Tooba.BuildingBlocks` only |
| `Tooba.Notification.Domain` | none |
| `Tooba.Notification.Application` | `Tooba.BuildingBlocks`, own Contracts, own Domain |
| `Tooba.Notification.Infrastructure` | own Contracts/Application/Domain + `Tooba.ModuleContracts`, `Tooba.Persistence` (platform persistence seam), `Order.Contracts`, `Payment.Contracts`, `Fulfillment.Contracts`, `Returns.Contracts` |
| `Tooba.Notification.Endpoints` | own Application + `Order.Contracts` + `Tooba.BuildingBlocks` |

## 2. Foreign coupling results

- Foreign `*.Application` edges: **ZERO** (0 project refs, 0 `using` hits).
- Foreign `*.Infrastructure` edges: **ZERO** (0 project refs, 0 `using` hits).
- Foreign `*.Domain` edges: **ZERO** (0 project refs, 0 `using` hits).
- Foreign `*.Endpoints` edges: **ZERO** (0 project refs, 0 `using` hits).
- `Tooba.Host` edges: **ZERO** (0 project refs, 0 `using` hits).
- Repo-wide scan of every Notification `.cs` for `using Tooba.(Order|Payment|Fulfillment|Returns|Wallet|Support|Cart|Catalog|Identity|Media|Localization).(Application|Infrastructure|Domain)` and `using Tooba.Host` → the only hit is a **guard's own assertion string** inside `Tooba.Notification.Tests/Architecture/NotificationArchitectureGuardTests.cs` (a test proving absence, not a dependency).
- Textual `Wallet.`/`Support.` occurrences in Contracts/Rendering are semantic notification-type
  string constants (`wallet.gift_card.redeemed` etc.) and copy text — no type dependency.

## 3. Legal foreign seams (Contracts-only)

- `Order.Contracts`: `IOrderNotificationReader` + storefront/customer context contracts (consumer
  projection seam). Also referenced by Endpoints for the storefront guest-actor contract.
- `Payment.Contracts` / `Fulfillment.Contracts` / `Returns.Contracts`: integration-event contract
  types consumed by `Infrastructure/Handlers/NotificationEventHandlers.cs`.
- Consumers of Notification (Wallet, Support) reach only `INotificationCreationPort` /
  `CreateNotificationCommand` / `NotificationSemanticTypes` in `Tooba.Notification.Contracts`.

## 4. Cross-module join / persistence ownership

- `crossModuleJoinState = ZERO`: no foreign `DbContext`/`DbSet`, no cross-module SQL/EF join, no
  foreign table reach-through (guards: `Infrastructure_uses_public_contracts_not_foreign_application`,
  `Notification_golden_boundaries_and_physical_layout_remain_clean`).
- `NotificationDbContext` owns exactly the `notification` schema
  (`modelBuilder.HasDefaultSchema(Schema)`, `Schema == "notification"`); outbox uses the shared
  platform `Tooba.Persistence.OutboxMessage` mechanism (certified module precedent).
- Migration set unchanged: exactly `20260827111240_InitialNotification` (+ Designer + ModelSnapshot);
  `git diff 7b8ab79e..HEAD` over Infrastructure = **empty** — `crossModulePersistenceState = ZERO`,
  `schemaMigrationState = UNCHANGED`.

## 5. Host authority classification

| Host artifact | Classification |
| --- | --- |
| `Program.cs` module assembly scan + `MapNotificationEndpoints()` + `AddNotificationEndpointPresentation()` + `HostNotificationSellerAuthorizer` DI | `ALLOWED_COMPOSITION_ROOT` |
| `Security/Seller/HostNotificationSellerAuthorizer.cs` (thin `sellerAccess.RequireAuthorizedAsync` adapter over the module-declared `INotificationSellerAuthorizer` seam; no business/persistence authority) | `ALLOWED_SECURITY_ADAPTER` |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` Notification descriptor | `ALLOWED_COMPOSITION_ROOT` (platform migration seam) |
| `Host/Tooba.Host/Notifications` folder | absent — ZERO |
| `ILLEGAL_BUSINESS_AUTHORITY` / `ILLEGAL_PERSISTENCE_AUTHORITY` / `ILLEGAL_ENDPOINT_OWNERSHIP` | all ZERO |

Closed-folder regression check: zero Notification files added to any Host folder between `7b8ab79e`
and `028ef769` (diff scope proof in `validation.md`); `Host-Final-Closure-State = PRESERVED`.

## 6. Error / localization / API-result canonical seams

- Single stable-code owner: `Contracts/Errors/NotificationErrorCodes.cs` — 5 declared codes
  (`notification.missing`, `notification.target_route.empty/unsafe/not_allowed`,
  `notification.recipient_kind.invalid`) + `IsKnown`; zero raw code literals outside this owner
  (W1 guard `Production_code_has_zero_raw_notification_error_code_literals`).
- `customer.session.required` stays Foundation-owned (`FoundationErrorCatalogContributor`
  registers it exactly once); Notification consumes via `NotificationSharedErrorCodes` without
  re-registration — duplicate ownership: NONE (`ErrorCatalogUniqueCodeGuardTests` PASS).
- Typed-fault seam: `Application/Composition/NotificationOperation.cs` catches
  `ContractOperationException` with `NotificationErrorCodes.IsKnown(ex.Code)` filter; unknown codes
  and unknown exceptions propagate untouched to the global boundary; zero message-prose
  classification.
- Localization: `NotificationErrorResourceSet` claims the `notification.` keyspace;
  `NotificationErrors.resx` + `NotificationErrors.fa.resx` bilingual pair covers all declared keys;
  registered once in `NotificationModule` (W1 guard `Bilingual_resource_pair_covers_the_declared_module_keyspace`).
- API result: all 10 routes map via `ApiResponseFactory.From`/`FromFailure`; zero
  `Results.Json/BadRequest/Problem`, zero `ex.Message` (W3 cert guard `Every_route_maps_through_isender_and_api_response_factory`).
- Logging/telemetry: counters-only `NotificationInstrumentation`; no second pipeline; no sensitive
  data; no `DateTime.UtcNow`/`Guid.NewGuid()` bypass; no silent catch; no localized exception prose
  (`Notification_golden_boundaries...` guard).

## 7. Microservice extractability verdict

With zero foreign Application/Infrastructure/Domain/Endpoints edges, Contracts-only foreign seams,
own schema, unchanged migrations and Host composition-only residue:
`microserviceExtractable = true`, `blockingResidualDebt = ZERO`.
