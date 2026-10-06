# TB-TMAR-NOTIFICATION-AMSC-001-W0 — Analyze (tooba-architecture-analyze)

- Task: `TB-TMAR-NOTIFICATION-AMSC-001-W0`
- Skill: `tooba-architecture-analyze` (ANALYSIS-ONLY; zero production change)
- Target: `src/backend/Modules/Notification/Tooba.Notification.*`
- Lock: `ARCH-COMPLETE-002`
- Starting HEAD: `9bcb01c0` (`HEAD == origin/main` verified)
- Prior certification: **NONE** — Notification is listed in `uncertifiedHttpOwningModules`
  (`docs/architecture/tmar-module-structure-manifests.json`) and is absent from
  `structureLock.certifiedModules`. This AMSC pass is the module's first
  four-wave standardization (`Analyze → Migrate → Structure → Certify`).

---

## 24. Target analyzed

Five production projects, 41 production `.cs` files (plus 3 EF migration files), ~1,900 physical LOC:

```text
Contracts/      Commands/(1) Copy/(1) Dtos/(1) Ports/(1) Routes/(1)
Domain/         Aggregates/UserNotification.cs  ValueObjects/NotificationRecipientKind.cs
Application/    Commands/{6 use-case folders × 1 file}  Queries/{4 use-case folders × 1 file}
                Errors/NotificationErrorCodes.cs  Models/(3)  Ports/INotificationDirectory.cs
                Rendering/NotificationCopy.cs
Endpoints/      NotificationEndpointModule.cs  Customer/(3)  Seller/(2)  Errors/(1)
Infrastructure/ DependencyInjection/NotificationModule.cs  Directories/NotificationDirectory.cs
                Handlers/NotificationEventHandlers.cs  Messaging/NotificationOutboxRegistration.cs
                Observability/NotificationInstrumentation.cs  Projectors/NotificationProjector.cs
                Persistence/NotificationDbContext.cs  Persistence/Migrations/(2 + snapshot)
Tests/          Architecture/(1)  Behavior/(3)
```

No file is oversized: the largest production file is `Infrastructure/Directories/NotificationDirectory.cs`
at **262 LOC**, far below the 800 LOC `ARCH-SIZE-001` ceiling, and no Notification file appears in
`Baselines/tmar-source-size-baseline.json`.

## 25. Responsibility map

| Responsibility | Location | Classification |
| --- | --- | --- |
| Persistent notification aggregate + invariants (id/recipient/type/route/source idempotency, read/soft-delete transitions) | `Domain/Aggregates/UserNotification.cs` | DOMAIN_RULE |
| Recipient kind value object | `Domain/ValueObjects/NotificationRecipientKind.cs` | DOMAIN_RULE |
| Recipient-scoped inbox persistence seam (create-idempotent, list, unread count, mark read, mark-all, soft delete) | `Infrastructure/Directories/NotificationDirectory.cs` | PERSISTENCE (single cohesive responsibility) |
| Event → notification projection (payment/fulfillment/returns events) | `Infrastructure/Handlers/NotificationEventHandlers.cs` | INTEGRATION_ADAPTER |
| Checkout-recipient projection via Order Contracts reader | `Infrastructure/Projectors/NotificationProjector.cs` | INTEGRATION_ADAPTER |
| Outbox registration (consumer-only) | `Infrastructure/Messaging/NotificationOutboxRegistration.cs` | PERSISTENCE |
| Counters telemetry | `Infrastructure/Observability/NotificationInstrumentation.cs` | PRESENTATION_COMPOSITION |
| Module composition + DI + schema migrator | `Infrastructure/DependencyInjection/NotificationModule.cs` | HOST_COMPOSITION_ROOT (module-owned) |
| EF mapping + `notification` schema | `Infrastructure/Persistence/NotificationDbContext.cs` | PERSISTENCE |
| 10 CQRS requests/handlers (5 customer + 5 seller) | `Application/Commands/*`, `Application/Queries/*` | APPLICATION_USE_CASE |
| Module port | `Application/Ports/INotificationDirectory.cs` | APPLICATION_USE_CASE |
| Read-time bilingual copy rendering | `Application/Rendering/NotificationCopy.cs` | APPLICATION_USE_CASE (presentation content) |
| Recipient-kind mapping | `Application/Models/NotificationRecipientKindMapping.cs` | APPLICATION_USE_CASE |
| HTTP list/unread/marked models + wire mapper | `Application/Models/NotificationHttpModels.cs`, `NotificationModels.cs` | APPLICATION_USE_CASE |
| Stable error codes | `Application/Errors/NotificationErrorCodes.cs` | CONTRACT (misplaced — see §33/§40) |
| Cross-module creation port + command | `Contracts/Ports/INotificationCreationPort.cs`, `Contracts/Commands/CreateNotificationCommand.cs` | CONTRACT |
| Shared semantic types for Wallet/Support consumers | `Contracts/Copy/NotificationSemanticTypes.cs` | CONTRACT |
| Recipient kind DTO | `Contracts/Dtos/NotificationRecipientKind.cs` | CONTRACT |
| Safe deep-link route allowlist | `Contracts/Routes/NotificationTargetRoutes.cs` | CONTRACT |
| Customer HTTP surface (5 routes) | `Endpoints/Customer/NotificationCustomerEndpoints.cs` | HTTP_ENDPOINT |
| Seller HTTP surface (5 routes) | `Endpoints/Seller/NotificationSellerEndpoints.cs` | HTTP_ENDPOINT |
| Module-owned customer actor resolver | `Endpoints/Customer/NotificationCustomerAuthorizer.cs` | AUTHORIZATION_ADAPTER (module-owned) |
| Neutral seller auth seam (Host implements) | `Endpoints/Seller/INotificationSellerAuthorizer.cs` | AUTHORIZATION_ADAPTER (seam) |
| Error catalog contributor | `Endpoints/Errors/NotificationErrorCatalogContributor.cs` | CONTRACT (presentation) |
| Composition entry | `Endpoints/NotificationEndpointModule.cs` | HTTP_ENDPOINT |

## 26. Ownership map

All responsibilities are owned by **Notification**. No responsibility belongs to another module or to
Host. Host residue is exactly: `Program.cs` (module assembly registration line 39/97/178/240/442 —
`AddNotificationEndpointPresentation()`, CQRS assembly scan, seller authorizer DI, `app.MapNotificationEndpoints()`),
`Security/Seller/HostNotificationSellerAuthorizer.cs` (thin seller security adapter, module-declared
seam implemented in Host), `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` (migration descriptor)
and the dev bootstrap `MediaDbContext`-style migrate seam (`MarketplaceDevelopmentBootstrap.cs`
Notification migrate — verified below in §29). All are `ALLOWED_COMPOSITION_ROOT` /
`ALLOWED_SECURITY_ADAPTER` / dev-seam. `Host/Tooba.Host/Notifications` folder is **absent**;
`Tooba.Notification.Endpoints` has **zero** Host dependency; the Host folder-structure guard
(`NotificationArchitectureGuardTests`) already enforces the Host `NotificationDbContext` allowlist.

## 27. MUST_SPLIT decisions

**None.** No production file carries responsibilities owned by different modules, and no file is a
multi-responsibility cohesion violation:

- `NotificationDirectory.cs` (262 LOC) owns exactly one responsibility — the recipient-scoped inbox
  persistence seam. Its six operations (create/list/unread/mark/mark-all/dismiss) serve the single
  notification-inbox capability with one private shared filter helper; this is one seam, not a
  two-responsibility file (contrast Media's upload-pipeline vs library-queries split).
- `NotificationEventHandlers.cs` (~235 LOC) holds 7 integration-event handlers of one mechanical
  shape (event → payload → projector). All change for the same reason (event contracts); splitting
  would be cosmetic.

## 28. Current illegal dependencies

**NONE.** No Notification project references any foreign `*.Application` / `*.Infrastructure` /
`*.Domain` project. The foreign project references are the legal Contracts seams and the generic
platform:

| Project | Foreign references |
| --- | --- |
| `Contracts` | *(none)* |
| `Domain` | `Tooba.BuildingBlocks` (platform; unused in code) |
| `Application` | `Tooba.BuildingBlocks`, own Contracts + Domain |
| `Endpoints` | `Tooba.BuildingBlocks`, own Application, `Order.Contracts` (StorefrontGuestActor / customer context contracts) |
| `Infrastructure` | `Tooba.ModuleContracts`, `Tooba.Persistence`, own Contracts/Application/Domain, `Order.Contracts` (`IOrderNotificationReader`), `Payment.Contracts` / `Fulfillment.Contracts` / `Returns.Contracts` (integration events) |

`foreignAppInfraDomainCoupling = ZERO`. The only legal *consumer* edges into Notification are
Contracts-only: `Wallet.Infrastructure → Tooba.Notification.Contracts` (`INotificationCreationPort`),
`Support.Infrastructure → Tooba.Notification.Contracts` (same port).

## 29. Cross-module join inventory

**NONE.** `NotificationDbContext` owns only the `notification` schema (`user_notifications` + the
shared outbox table). No foreign `DbSet`, no foreign `DbContext`, no EF navigation crossing
ownership, no raw SQL joining another module's tables. The potentially-join-shaped read (recipients
for a checkout) is already the canonical Contracts-only replacement: `IOrderNotificationReader`
(`Order.Contracts.Notifications`) consumed by `NotificationProjector` — Order never shares its
DbContext and Notification never reaches into `order.*`. The existing guard additionally pins the
Order.Contracts notification surface clean (no root dump, `Tooba.Order.Contracts.Notifications`
namespace prefix only, no `TypeForwardedTo`).

## 30. Contracts-only replacement map

No replacement is required — the boundary is already Contracts-only and narrow:

| Consumer | Contract used | Verdict |
| --- | --- | --- |
| Wallet (`WalletDirectory`) | `INotificationCreationPort.CreateIfAbsentAsync` | legal, narrow |
| Support (`SupportDevelopmentSeed`) | `INotificationCreationPort` | legal, narrow (dev seed) |
| Wallet/Support semantic types | `NotificationSemanticTypes` (`wallet.*` / `support.admin_reply` copy types) | legal |
| Order (reverse direction) | `IOrderNotificationReader` consumed by Notification — Notification is the consumer, Order the owner | legal |

## 31. CQRS / MediatR gaps

`CQRS-State = COMPLIANT`.

- Ten real `IRequest`/`IRequest<T>` types with real `IRequestHandler<,>` implementations, dispatched
  through `ISender` from thin endpoints; MediatR `12.5.0` via the foundation
  `AddToobaCqrsFoundation` (registered in Host `Program.cs` against the Application assembly).
- Endpoints contain transport-only code (actor resolution + `api.From(...)`); no Directory/DbContext
  calls from endpoints; no custom dispatcher.

## 32. Validation classification matrix

`Validator-Coverage-State = GAPS` — **zero validators exist** for ten endpoint-reachable requests.

| Request | Untrusted transport input | Classification | W1 validator |
| --- | --- | --- | --- |
| `ListCustomerNotificationsQuery` | `skip`, `take` (query) | `VALIDATOR_REQUIRED_MISSING` | `ListCustomerNotificationsQueryValidator` |
| `GetCustomerUnreadNotificationCountQuery` | none (authorizer-derived actor) | `NO_VALIDATOR_REQUIRED` (AUTH_SCOPED) | — |
| `MarkCustomerNotificationReadCommand` | `NotificationId` (route Guid) | `VALIDATOR_REQUIRED_MISSING` | `MarkCustomerNotificationReadCommandValidator` |
| `MarkAllCustomerNotificationsReadCommand` | none (authorizer-derived actor) | `NO_VALIDATOR_REQUIRED` (AUTH_SCOPED) | — |
| `DismissCustomerNotificationCommand` | `NotificationId` (route Guid) | `VALIDATOR_REQUIRED_MISSING` | `DismissCustomerNotificationCommandValidator` |
| `ListSellerNotificationsQuery` | `skip`, `take` (query) | `VALIDATOR_REQUIRED_MISSING` | `ListSellerNotificationsQueryValidator` |
| `GetSellerUnreadNotificationCountQuery` | none (authorizer-derived party) | `NO_VALIDATOR_REQUIRED` (AUTH_SCOPED) | — |
| `MarkSellerNotificationReadCommand` | `NotificationId` (route Guid) | `VALIDATOR_REQUIRED_MISSING` | `MarkSellerNotificationReadCommandValidator` |
| `MarkAllSellerNotificationsReadCommand` | none (authorizer-derived party) | `NO_VALIDATOR_REQUIRED` (AUTH_SCOPED) | — |
| `DismissSellerNotificationCommand` | `NotificationId` (route Guid) | `VALIDATOR_REQUIRED_MISSING` | `DismissSellerNotificationCommandValidator` |

`endpointReachableRequests = 10`, `validatorRequiredCount = 6`, `noValidatorRequiredCount = 4`,
gap = 6 missing. Authorizer-derived actor/party values are trusted and never policed (Fulfillment
precedent); route Guids and paging bounds are untrusted transport shape. Missing-wrong-owner cases
stay Application-owned (`notification.missing`), exactly as today.

**Finding (W1):** validators must emit distinct `notification.validation.*` transport codes mapped
through the foundation `validation.failed` descriptor (certified Localization/Media/Content/Cart
precedent), not localized text.

## 33. Localization findings

`Localization-State = MISSING_INFRASTRUCTURE_USE` (module owns no error resources) with two
in-code copy findings.

| Finding | Location | Severity |
| --- | --- | --- |
| No `IErrorResourceSet`, no `.resx` pair for the module key space — `notification.missing` resolves through the contributor's inline `SafeTitleFallback` only, and any new Notification-owned code would have no bilingual resource | module-wide | **high** — W1 adds `NotificationErrorResourceSet` + `NotificationErrors.resx` / `NotificationErrors.fa.resx` claiming `notification.` |
| Stable codes declared outside the canonical Contracts home: `NotificationErrorCodes` lives in `Application/Errors` instead of `Contracts/Errors` | `Application/Errors/NotificationErrorCodes.cs` | medium — W1 relocates (canonical `Contracts.Errors.<Module>ErrorCodes`) |
| Four raw code-style literals thrown as untyped `InvalidOperationException`, not declared codes: `notification.target_route.empty` / `.unsafe` / `.not_allowed` (`Contracts/Routes/NotificationTargetRoutes.cs`), `notification.recipient_kind.invalid` (`Application/Models/NotificationRecipientKindMapping.cs`) | Contracts + Application | **high** — W1 converts to declared stable codes carried by typed `ContractOperationException` and maps them in the module seam |
| Domain aggregate invariants raise literal `InvalidOperationException` (`notification.id_required`, `notification.recipient_required`, `notification.type_required`, `notification.source_event_id_required`, `notification.source_type_required`, `notification.target_route_invalid`) | `Domain/Aggregates/UserNotification.cs` | low — repo-wide Domain aggregate idiom (Order/Catalog/Promotion/Notification-certified precedent); never mapped, never user-facing; retained |
| `notification.outbox.emit_not_supported` literal in the framework outbox invariant | `Infrastructure/Messaging/NotificationOutboxRegistration.cs` | low — repo-wide outbox idiom shared by certified Media/Localization/Content/CustomerProfile; retained |
| Bilingual notification copy (titles/bodies) rendered in code by `NotificationCopy.Resolve` from persisted `Type` + payload with a client-supplied `locale` | `Application/Rendering/NotificationCopy.cs` | low — this is the module's product content rendering (persisted semantic type → read-time copy), not an API error message; the canonical catalog/resx mechanism remains authoritative for all faults; retained as the locked read-time contract |
| `locale ?? "fa"` default in endpoints | `Endpoints/Customer|Seller/*Endpoints.cs` | low — explicit client-driven query parameter (not `Accept-Language` parsing); OPTIONAL_PRESENTATION_LOCALE precedent (Fulfillment); retained |
| `ex.Message` used as a contract | **ZERO** | — |
| `customer.session.required` | declared in `NotificationErrorCodes` for convenience but owned/registered **only** by `FoundationErrorCatalogContributor` (shared cross-cutting code; AddressBook/Wishlist precedent) | correct — duplicate usage allowed, single descriptor owner |

## 34. API result / error mapping findings

`API-Result-Pattern-State = CANONICAL`.

- All ten endpoints return `api.From(...)` / `api.FromFailure(...)` through `ApiResponseFactory`.
- `Results.Json` / `Results.BadRequest` / `Results.Problem` / local ProblemDetails builders: **ZERO**.
- Failure classification by parsing `ex.Message`: **ZERO**.
- Duplicate descriptor registration: **NONE** — `NotificationErrorCatalogContributor` registers
  exactly `notification.missing`; `customer.session.required` is consumed without re-registration
  (pinned by `HostCustomerFullClosureGuardTests`).
- **Gap (W1):** the four raw route/kind literals above surface today as untyped 500s
  (`platform.unexpected`) instead of catalogued expected failures; W1's typed-fault conversion turns
  them into catalogued 400s — the same accepted bounded expected-failure repair performed by Media/
  Cart/Localization W1 (HTTP 400 via Business classification, envelope unchanged).

## 35. Logging / sensitive-data findings

`Logging-State = CANONICAL`. `Sensitive-Logging-State = NONE`.

No `Console.WriteLine`, no `Debug.WriteLine`, no custom logger framework, no second telemetry
pipeline. The module logs nothing and instead uses the counters-only `NotificationInstrumentation`
(no message content, no payload, no recipient identifiers recorded). No passwords, tokens,
`Authorization` headers, cookies or credentials are logged anywhere.

## 36. OpenTelemetry / correlation findings

`OpenTelemetry-State = CANONICAL`, `Correlation-Trace-State = CANONICAL`.

No second `ActivitySource`, no `Meter`, no custom correlation provider, no custom header, no raw
`AsyncLocal`, no `StartActivity(...)`, no manual `traceparent` parsing. Notification issues **no**
cross-module HTTP calls of its own (it only *consumes* events and a Contracts reader), so no
`IModuleCallTracer` decoration is required; `traceId`/`correlationId` reach responses through the
canonical `ProblemDetailsContextProvider`.

## 37. File cohesion / splitting plan

| File | LOC | Verdict |
| --- | --- | --- |
| `Infrastructure/Directories/NotificationDirectory.cs` | 262 | `COHESIVE` (single inbox-persistence seam) |
| `Application/Rendering/NotificationCopy.cs` | 155 | `COHESIVE` |
| `Infrastructure/Handlers/NotificationEventHandlers.cs` | ~235 | `COHESIVE` (7 same-shape projection handlers) |
| everything else | ≤ 135 | `COHESIVE` |

No god-file, no root dump, no over-splitting candidate.

**Folder-granularity finding (W2, not W1):** `Application` is
`TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED` — `Application/Commands/<UseCase>/<UseCase>Command.cs`
(6 single-file leaves) and `Application/Queries/<UseCase>/<UseCase>Query.cs` (4 single-file leaves).
The module has two real capabilities (**Customer**, **Seller** — mirroring the Endpoints audiences).
W2 restructures to capability-first:

```text
Application/Customer/{Commands,Queries,Validators}/   (5 request files + 3 validators)
Application/Seller/{Commands,Queries,Validators}/     (5 request files + 3 validators)
Application/{Models,Ports,Rendering,Errors,Composition}/  (shared cross-capability concerns)
```

W1 creates the six new validator files co-located with their requests inside the existing
use-case folders (making them legitimate multi-file leaves) plus the shared `Composition/` and
`Validators/` folders; W2 performs the capability-first normalization in one bounded
behavior-preserving move set (namespaces follow physical paths; `Program.cs` CQRS assembly-scan
reference and guards updated).

## 38. Exact target paths / namespaces

W1 (behaviour-preserving canonical mechanisms):

```text
Contracts/Errors/NotificationErrorCodes.cs        (moved from Application/Errors; + IsKnown + 4 new codes)
Contracts/Errors/NotificationErrorResources.cs    (new: ResourceManager marker + IErrorResourceSet claiming notification.)
Contracts/Resources/NotificationErrors.resx       (new: en)
Contracts/Resources/NotificationErrors.fa.resx    (new: fa)
Contracts/Tooba.Notification.Contracts.csproj     (+ Tooba.BuildingBlocks reference for typed faults/resource set)
Application/Composition/NotificationOperation.cs  (new: typed-fault → Result seam, IsKnown filter)
Application/Validators/NotificationValidationCodes.cs (new: notification.validation.* transport codes)
Application/Commands|Queries/<UseCase>/*Validator.cs  (new: 6 co-located validators)
Infrastructure/DependencyInjection/NotificationModule.cs (+ IErrorResourceSet registration)
Endpoints/Errors/NotificationErrorCatalogContributor.cs (+ 4 descriptors)
```

W2 (structure lock):

```text
Application/Customer/{Commands,Queries,Validators}/  Application/Seller/{Commands,Queries,Validators}/
src/backend/Tooba.slnx                               (/Modules/Notification/ grouping already present — verified, 6 projects)
```

## 39. Behaviour-preservation checklist

Routes: `GET /v1/customer/notifications`, `GET /v1/customer/notifications/unread-count`,
`POST /v1/customer/notifications/{id:guid}/read`, `POST /v1/customer/notifications/read-all`,
`DELETE /v1/customer/notifications/{id:guid}` and the five seller mirrors — unchanged. Methods,
status codes, response shapes (`NotificationListHttpResponse`, `NotificationUnreadCountResponse`,
`NotificationMarkedCountResponse`, camelCase wire fields) — unchanged. Stable codes
`notification.missing`, `customer.session.required` (Foundation-owned) — unchanged. Authorization
(module customer authorizer + Host seller adapter) — unchanged. Idempotency (`SourceEventId`
unique-suppression, idempotent mark/delete) — unchanged. Paging clamp (1..100 take, skip ≥ 0),
ordering (`CreatedAt desc, NotificationId desc`), `IsDeleted` filtering — unchanged. Bilingual copy
rendering and category mapping — unchanged. `notification` schema, migration
`20260827111240_InitialNotification`, snapshot, table/column/index semantics — unchanged
(`migrationFilesChanged = 0`). `INotificationCreationPort` contract for Wallet/Support — unchanged.
`IOrderNotificationReader` consumption — unchanged. Localization keys and their semantics —
unchanged.

## 40. Migration order

1. Relocate `NotificationErrorCodes` to `Contracts/Errors/` with `IsKnown` declared-code guard and
   the four new codes (`target_route.empty/unsafe/not_allowed`, `recipient_kind.invalid`).
2. Reference `Tooba.BuildingBlocks` from Contracts; convert `NotificationTargetRoutes.RequireAllowed`
   and `NotificationRecipientKindMapping` throws to typed `ContractOperationException` carrying the
   declared codes.
3. Add `Application/Composition/NotificationOperation.cs` (typed-fault → `Result` seam, `IsKnown`
   filter, unknown codes propagate) and route the six mutate/list handlers through it where faults
   can originate (create-path consumers already go through the port; handlers map missing via
   `SemanticError` directly — the seam covers directory-raised contract faults).
4. Add `NotificationErrorResourceSet` + bilingual resx pair covering all five Notification-owned
   codes; register the resource set in the module composition; extend the catalog contributor with
   the four new descriptors (single owner, no duplicates).
5. Add `NotificationValidationCodes` (`notification.validation.*`) and the six FluentValidation
   transport validators co-located with their requests.
6. Add the durable W1 migrate guard.

W2 then normalizes Application to capability-first (Customer/Seller) and issues
`READY_FOR_CERTIFY`; W3 promotes the manifest entry, adds the certification guard, SoT and Master
Recovery checkpoint.

## 41. Verification plan

- Build `Tooba.Notification.*` (5 projects) + `Tooba.Host.Tests`.
- Focused: `NotificationArchitectureGuardTests`, `NotificationCqrsAndHttpContractTests`,
  `NotificationBehaviorCharacterizationTests`, `NotificationProjectorParityTests`,
  `ErrorCatalogUniqueCodeGuardTests`, `HostCustomerFullClosureGuardTests`,
  `TmarCompleteReferenceStructureGateTests`, plus the new `NotificationModuleAmsc001W*` guards.
- `git fetch` + `HEAD == origin/main` after each wave push.

## 42. Certification blockers

- None blocking. W1 must complete items 1–6 before W2 can issue `READY_FOR_CERTIFY`. The only
  accepted, explicit, documented non-conformances retained are the Domain aggregate literal
  invariants, the outbox framework invariant, the in-code bilingual copy rendering and the
  client-driven `locale` query parameter — none is a user-facing API error message.

---

## Structured state fields

| Field | Value |
| --- | --- |
| Foundation-State | `FOUNDATION_READY` (5 production projects + tests, no parallel structure) |
| Ownership-State | `correct` (no ownership migration required) |
| File-Cohesion-State | `COHESIVE` (no split required) |
| Oversized/God-File-State | none ≥ 800 LOC; largest 262 LOC |
| Folder-Granularity-State | `TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED` (Application Commands/Queries use-case leaves; repaired in W2) |
| Localization-State | `MISSING_INFRASTRUCTURE_USE` (no module resource set/resx) + 4 raw untyped code literals |
| API-Result-Pattern-State | `CANONICAL` |
| Stable-Error-Code-State | `CATALOGUED` (1 owned descriptor) with 4 `UNREGISTERED_CODES` literals |
| Logging-State | `CANONICAL` |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `COMPLIANT` |
| Validator-Coverage-State | `GAPS` (0 of 6 required present) |
| Contracts-Boundary-State | `CLEAN` |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` (zero foreign App/Infra/Domain) |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (own `notification` schema) |
| Endpoint-Ownership-State | `MODULE_OWNED` (10 routes; Host ZERO) |
| Host-Residue-State | `ALLOWED_COMPOSITION_ROOT` + `ALLOWED_SECURITY_ADAPTER` ×1 + migration descriptor |
| Schema-Migration-State | `UNCHANGED` (1 migration + snapshot, no drift) |
| Behavior-Preservation-Risk | `LOW` |
| Canonical-Reference-Used | Media/Localization (typed fault seam + declared-code guard + transport validation codes + Contracts resx pair), Fulfillment (validator classification + OPTIONAL_PRESENTATION_LOCALE), Offer/BuildingBlocks (Result/`ApiResponseFactory`/`IErrorCatalogContributor`/`IErrorResourceSet`), CustomerProfile (capability-first audience layout) |
| Structure-Handoff-State | `REQUIRED` (W2 must normalize Application and issue `READY_FOR_CERTIFY`) |
| Final-Disposition | `READY_TO_MIGRATE` |

## W0 stop

`ANALYZE_COMPLETE` / `READY_TO_MIGRATE`. Zero production change in this wave.
Stop gate: `USER_REVIEW_NOTIFICATION_AMSC_001_W0`. `automaticNextImplementationTask = NONE`.
