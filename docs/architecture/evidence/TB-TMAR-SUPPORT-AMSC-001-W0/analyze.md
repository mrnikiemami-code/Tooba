# TB-TMAR-SUPPORT-AMSC-001 — Wave 0 (Analyze)

- **Skill:** `tooba-architecture-analyze` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Support/Tooba.Support.*`
- **Starting HEAD:** `c886698dd644088a2b3a5b31cbd3ec24d492c23d`
- **Branch:** `main` (`HEAD == origin/main`)
- **Production code changed in this wave:** NONE (analysis only)
- **Host touched in this wave:** NONE

---

## 0. Prior art — Support already carries a closed Host-evacuation lineage

Support is **not** an unrecovered module. `docs/architecture/tmar-current-state.json` already records
`hostSupportAmc` (`TB-TMAR-HOST-SUPPORT-AMC-001`, `CLOSED_HOST_ZERO`) and `hostSupportAmcR1`
(`CLOSED_HOST_ZERO_R1_ACCESSCONTROL_CONTRACTS_SEAM`, implementation commit
`646a2a445705c76d7b6890e4597ff2999d8bff4e`). `src/backend/Host/Tooba.Host/Support` is **absent**;
the durable guard is `HostSupportAmcGuardTests`.

Support is, however, the **only HTTP-owning module in the repository with no `Contracts` project**
(`tmar-module-structure-manifests.json` → `uncertifiedHttpOwningModules: ["Support","Wallet"]`; every
other one of the 32 modules has `Tooba.<Module>.Contracts`). It also has **no `structureCertified`
manifest entry**, **no AMSC lineage**, and **no module-scoped AMSC guard**.

AMSC is therefore a **completion + hardening pass** on an ownership-correct module, not an evacuation.
The honest question for W0 is: *which AMSC gates are not yet actually proven on the shipped surface?*
This analysis answers that with evidence and names the real defects instead of fabricating debt.

---

## 1. Target analyzed

| Project | Production `.cs` (excl. generated) | Largest file |
| --- | --- | --- |
| `Tooba.Support.Contracts` | **ABSENT** | — |
| `Tooba.Support.Domain` | 3 | `Aggregates/SupportTicket.cs` (244 LOC) |
| `Tooba.Support.Application` | 24 | `Ports/ISupportDirectory.cs` (105 LOC) |
| `Tooba.Support.Infrastructure` | 8 + 3 migrations | `Directories/SupportDirectory.cs` (511 LOC) |
| `Tooba.Support.Endpoints` | 11 + 2 resx | `Customer/SupportCustomerEndpoints.cs` (130 LOC) |
| `Tooba.Support.Tests` | 5 | `Architecture/SupportArchitectureGuardTests.cs` (309 LOC) |

Totals: **5 projects, 46 production `.cs`, 2 `.resx`, 3 migration files** (1 migration + 1 designer +
1 snapshot). Host owns **zero** Support production files.

Host-side Support surface (all legitimate, all already classified by the AMC-001 lineage):

| Host path | Classification | Justification |
| --- | --- | --- |
| `Security/Seller/HostSupportSellerAuthorizer.cs` | `ALLOWED_SECURITY_ADAPTER` | Implements the module's `ISupportSellerAuthorizer` seam against neutral platform seams (`ISellerPanelAccess`, `IPlatformEffectiveAccessReader`). Consumes **zero** `Tooba.Support.Application/Domain/Infrastructure`. |
| `Admin/Access/Authorizers/HostSupportAdminAuthorizer.cs` | `ALLOWED_SECURITY_ADAPTER` | Implements `ISupportAdminAuthorizer`; consumes only `Tooba.Support.Endpoints.Admin` (the colocated code surface) + BuildingBlocks. |
| `Composition/SupportDevelopmentSeedHost.cs` | `ALLOWED_COMPOSITION_ROOT` | Binds CommerceContext/Admin/Seller/AccessControl Contracts prelude, then delegates to `SupportDevelopmentSeedBootstrap.ApplyAsync`. |
| `Program.cs` lines 97/178/240/241/383/438 | `ALLOWED_COMPOSITION_ROOT` | `AddSupportEndpointPresentation`, `AddToobaCqrsFoundation(…Support.Application…)`, authorizer adapter registration, `MapSupportEndpoints()`, dev seed call. |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs:63` | `ALLOWED_COMPOSITION_ROOT` | `Descriptor<SupportDbContext>("Support", SupportDbContext.Schema)`. |

**Host `ILLEGAL_*` categories = ZERO.**

---

## 2. Structured State Fields

| Field | Value |
| --- | --- |
| **Applicability** | `HTTP_OWNING` — 17 real shipped routes across `/v1/customer/support`, `/v1/seller/support`, `/v1/admin/support` |
| **Foundation-State** | `FOUNDATION_PARTIAL` — Domain/Application/Infrastructure/Endpoints exist and are capability-foldered; **`Tooba.Support.Contracts` is MISSING** although the module owns stable cross-boundary error codes and its Host admin adapter already consumes a module code surface |
| **Ownership-State** | `correct` (every responsibility belongs to Support; Host owns no Support business authority) |
| **File-Cohesion-State** | `COHESIVE` (no `MUST_SPLIT`; 511-LOC `SupportDirectory.cs` is single-responsibility persistence for one aggregate) |
| **Oversized/God-File-State** | NONE above the 800-LOC ceiling; `Directories/SupportDirectory.cs` 511 LOC = `WATCH`, single responsibility |
| **Localization-State** | `HARDCODED_TEXT` / `MISSING_INFRASTRUCTURE_USE` (see §9 / blocker B2) |
| **API-Result-Pattern-State** | `CANONICAL` — `ApiResponseFactory.From/FromFailure` everywhere; zero `Results.Json`-as-envelope, zero local `ProblemDetails` builder |
| **Stable-Error-Code-State** | `UNREGISTERED_CODES` — 9 public outcome codes declared in `Application.Errors`, **5 of them registered nowhere** in the composed catalog (see §10 / blocker B3) |
| **Logging-State** | `CANONICAL` — zero `ILogger`, zero `Console/Debug.WriteLine`, zero second telemetry pipeline in the module |
| **Sensitive-Logging-State** | `NONE` |
| **OpenTelemetry-State** | `CANONICAL` — zero `ActivitySource.StartActivity`, zero second `ActivitySource`/`Meter` |
| **Correlation-Trace-State** | `CANONICAL` — no parallel correlation, no `AsyncLocal`, no manual `traceparent` |
| **CQRS-State** | `COMPLIANT` — 17 real `IRequest<T>` + 17 real `IRequestHandler<,>`, all dispatched from Endpoints through `ISender`, registered via `AddToobaCqrsFoundation` (MediatR 12.5) |
| **Validator-Coverage-State** | `GAPS` — **0 validators**; 9 of 17 endpoint-reachable requests carry caller-controlled malformable transport text (see §12 / blocker B4) |
| **Contracts-Boundary-State** | `CLEAN` for coupling (no illegal reference) but **`MISSING`** as a project: module-boundary stable codes live in `Application.Errors` |
| **Cross-Module-Coupling-State** | `LEGAL_CONTRACTS_ONLY` — the single foreign edge is `Support.Infrastructure → Tooba.Notification.Contracts` (port + DTOs + semantic types + routes). No foreign `Application`/`Domain`/`Infrastructure`. |
| **Cross-Module-Join-State** | `NONE` — `SupportDbContext` only ever queries `Tickets`/`Messages` in schema `support`; `RelatedEntityId` is a soft id with an explicit no-JOIN comment |
| **Persistence-Ownership-State** | `CORRECT` — one `SupportDbContext`, schema `support`, own migrations |
| **Endpoint-Ownership-State** | `MODULE_OWNED` — 17/17 routes in `Tooba.Support.Endpoints`; Host-owned route count **0** |
| **Host-Residue-State** | `ALLOWED_SECURITY_ADAPTER` + `ALLOWED_COMPOSITION_ROOT` only (see §1) |
| **Schema-Migration-State** | `UNCHANGED` — `20260827120000_InitialSupport` (+ designer + snapshot), no drift |
| **Behavior-Preservation-Risk** | `LOW` |
| **Canonical-Reference-Used** | BuildingBlocks (`Result`, `ApiResponseFactory`, `IErrorCatalogContributor`, `IErrorResourceSet`, `ContractOperationException`, `AddToobaCqrsFoundation`); `Returns` for Contracts-Errors + typed-fault + `Application/Composition/<Module>Operation` + `Validation/` precedent; `Story` for validator + resx + Infrastructure/Messaging split; `docs/architecture/32-persian-code-documentation-standard.md` |
| **Final-Disposition** | `READY_TO_MIGRATE` |

---

## 3. Responsibility map

| Responsibility | Owner | Where today | Verdict |
| --- | --- | --- | --- |
| Ticket aggregate / lifecycle invariants | Support.Domain | `Aggregates/SupportTicket.cs`, `Entities/TicketMessage.cs`, `ValueObjects/SupportEnums.cs` | correct |
| Application use cases (17) | Support.Application | `Commands/<UseCase>/`, `Queries/<UseCase>/` | correct ownership, `TECHNICAL_AXIS_FIRST` foldering (W2) |
| Directory port + CQRS-facing inputs/models | Support.Application | `Ports/ISupportDirectory.cs`, `Ports/ISupportDemoPreviewPort.cs`, `Models/*` | correct |
| Stable machine error codes (public outcomes) | **Support.Contracts (missing)** | `Application/Errors/SupportErrorCodes.cs` | `MISSING_BOUNDARY` |
| Typed fault mapping | Support.Application | `Errors/SupportExceptionMapper.cs` | correct shape, wrong fault primitive (W1) |
| Ticket persistence | Support.Infrastructure | `Directories/SupportDirectory.cs`, `Persistence/SupportDbContext.cs` | correct |
| Notification dispatch (cross-module) | Support.Infrastructure → Notification.Contracts | `SupportDirectory.NotifyRequesterAsync` | `LEGAL_CONTRACTS_ONLY` |
| Outbox registration | Support.Infrastructure | `Messaging/SupportOutboxRegistration.cs` | correct |
| Development seed + demo snapshot | Support.Infrastructure | `Seeds/SupportDevelopmentSeed.cs`, `Adapters/SupportDemoSnapshot.cs`, `Development/SupportDevelopmentSeedBootstrap.cs` | correct |
| HTTP endpoints (17) | Support.Endpoints | `Customer/`, `Seller/`, `Admin/` | correct |
| Error catalog + resources | Support.Endpoints | `Errors/SupportErrorCatalogContributor.cs`, `Resources/*` | correct location, incomplete coverage |
| Authorization seams | Support.Endpoints (iface) + Host (adapter) | `Admin/ISupportAdminAuthorizer.cs`, `Seller/ISupportSellerAuthorizer.cs`, `Customer/ISupportCustomerAuthorizer.cs` + 3 Host adapters | correct |
| Module composition | Support.Infrastructure | `DependencyInjection/SupportModule.cs` | correct |

**MUST_SPLIT: NONE.** No Support file mixes two modules' responsibilities.

---

## 4. Illegal dependencies

`rg` over all Support production sources and `.csproj` `ProjectReference` edges found **exactly one**
foreign module edge:

```
Tooba.Support.Infrastructure -> Tooba.Notification.Contracts
```

Used symbols: `INotificationCreationPort`, `CreateNotificationCommand`, `NotificationRecipientKind`,
`NotificationSemanticTypes`, `NotificationTargetRoutes`. These are Contracts-boundary types.

Forbidden-form search results (all ZERO):

- foreign `*.Application` / `*.Domain` / `*.Infrastructure` reference — **0**
- foreign `DbContext` / `DbSet` — **0**
- cross-module EF/SQL join — **0**
- `Tooba.Host` reference from any Support project — **0**
- `namespace` alias hiding placement — **0**
- `TypeForwardedTo` / duplicate compatibility type — **0**
- Support → `Tooba.Notification.Application|Domain|Infrastructure` — **0**

`Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY` is therefore valid (no direct foreign
Application/Infrastructure/Domain dependency remains).

---

## 5. Cross-module join inventory

**NONE.** `SupportDirectory` issues only `_db.Tickets` / `_db.Messages` queries on schema `support`.
`RelatedEntityType`/`RelatedEntityId` are deliberately soft (comment at `SupportDirectory.cs:500-510`:
"اعتبارسنجی soft بدون JOIN"). Notification is invoked through the Contracts port, never by joining
Notification persistence.

---

## 6. Contracts-only replacement map

| Interaction | Mechanism | State |
| --- | --- | --- |
| Support → Notification (admin public reply notification) | `Notification.Contracts.Ports.INotificationCreationPort.CreateIfAbsentAsync` | already correct |
| Host → Support admin auth code | `Support.Endpoints.Admin.SupportAdminAuthorizationCodes` | already correct (AMC-R1 seam) |
| Host → Support seller/customer auth | `Support.Endpoints.{Seller,Customer}.I*Authorizer` | already correct |
| Support stable codes → Host/composed catalog | **should be `Tooba.Support.Contracts.Errors.SupportErrorCodes`** | **W1 creates the boundary** |

No join, shim, or god-contract is required.

---

## 7. CQRS / MediatR gaps

**NONE.** All 17 endpoint-reachable requests are real `IRequest<T>` records with real
`IRequestHandler<,>` classes, dispatched by `ISender` from transport-only endpoint handlers, and
registered through `AddToobaCqrsFoundation(typeof(CreateCustomerTicketCommand).Assembly)` in Host
`Program.cs:178`. No custom dispatcher, no endpoint business logic, no endpoint→DbContext/Directory
call.

CQRS inventory (17):

| # | Request | Kind | Handler file |
| --- | --- | --- | --- |
| 1 | `ListCustomerTicketsQuery` | Query | `Queries/ListCustomerTickets` |
| 2 | `CreateCustomerTicketCommand` | Command | `Commands/CreateCustomerTicket` |
| 3 | `GetCustomerTicketQuery` | Query | `Queries/GetCustomerTicket` |
| 4 | `ReplyCustomerTicketCommand` | Command | `Commands/ReplyCustomerTicket` |
| 5 | `CloseCustomerTicketCommand` | Command | `Commands/CloseCustomerTicket` |
| 6 | `ReopenCustomerTicketCommand` | Command | `Commands/ReopenCustomerTicket` |
| 7 | `ListSellerTicketsQuery` | Query | `Queries/ListSellerTickets` |
| 8 | `CreateSellerTicketCommand` | Command | `Commands/CreateSellerTicket` |
| 9 | `GetSellerTicketQuery` | Query | `Queries/GetSellerTicket` |
| 10 | `ReplySellerTicketCommand` | Command | `Commands/ReplySellerTicket` |
| 11 | `CloseSellerTicketCommand` | Command | `Commands/CloseSellerTicket` |
| 12 | `ReopenSellerTicketCommand` | Command | `Commands/ReopenSellerTicket` |
| 13 | `ListAdminTicketsQuery` | Query | `Queries/ListAdminTickets` |
| 14 | `GetAdminTicketQuery` | Query | `Queries/GetAdminTicket` |
| 15 | `ReplyAdminTicketCommand` | Command | `Commands/ReplyAdminTicket` |
| 16 | `PatchAdminTicketCommand` | Command | `Commands/PatchAdminTicket` |
| 17 | `GetSupportDemoPreviewQuery` | Query | `Queries/GetSupportDemoPreview` |

---

## 8. Route ownership inventory (17 shipped routes)

| # | Verb + route | Audience | Request |
| --- | --- | --- | --- |
| 1 | `GET /v1/customer/support/tickets` | Customer | `ListCustomerTicketsQuery` |
| 2 | `POST /v1/customer/support/tickets` | Customer | `CreateCustomerTicketCommand` |
| 3 | `GET /v1/customer/support/tickets/{ticketId:guid}` | Customer | `GetCustomerTicketQuery` |
| 4 | `POST /v1/customer/support/tickets/{ticketId:guid}/replies` | Customer | `ReplyCustomerTicketCommand` |
| 5 | `POST /v1/customer/support/tickets/{ticketId:guid}/close` | Customer | `CloseCustomerTicketCommand` |
| 6 | `POST /v1/customer/support/tickets/{ticketId:guid}/reopen` | Customer | `ReopenCustomerTicketCommand` |
| 7 | `GET /v1/seller/support/tickets` | Seller | `ListSellerTicketsQuery` |
| 8 | `POST /v1/seller/support/tickets` | Seller | `CreateSellerTicketCommand` |
| 9 | `GET /v1/seller/support/tickets/{ticketId:guid}` | Seller | `GetSellerTicketQuery` |
| 10 | `POST /v1/seller/support/tickets/{ticketId:guid}/replies` | Seller | `ReplySellerTicketCommand` |
| 11 | `POST /v1/seller/support/tickets/{ticketId:guid}/close` | Seller | `CloseSellerTicketCommand` |
| 12 | `POST /v1/seller/support/tickets/{ticketId:guid}/reopen` | Seller | `ReopenSellerTicketCommand` |
| 13 | `GET /v1/admin/support/tickets` | Admin | `ListAdminTicketsQuery` |
| 14 | `GET /v1/admin/support/tickets/{ticketId:guid}` | Admin | `GetAdminTicketQuery` |
| 15 | `POST /v1/admin/support/tickets/{ticketId:guid}/replies` | Admin | `ReplyAdminTicketCommand` |
| 16 | `PATCH /v1/admin/support/tickets/{ticketId:guid}` | Admin | `PatchAdminTicketCommand` |
| 17 | `GET /v1/admin/support/demo-preview` | Admin (Development only) | `GetSupportDemoPreviewQuery` |

Host-owned routes for Support: **0**.

---

## 9. Input-provenance matrix (17 rows, set-equality gate)

Legend — **VR** = `VALIDATOR_REQUIRED`, **NV** = `NO_VALIDATOR_REQUIRED`.

| # | Route (verb path) | Request | Caller-controlled inputs → binding source | Transport-shape risk | Current owner | Class |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | GET `/tickets` (customer) | `ListCustomerTicketsQuery` | `status`(query,string), `page`(query,int), `pageSize`(query,int); `ActorUserId` server-derived (authorizer) | `status` is free text → `SupportEnumParsing.TryParseStatus` throws `support.status_invalid`; `page/pageSize` clamped by `NormalizePaging` + framework int binding | none | **VR** |
| 2 | POST `/tickets` (customer) | `CreateCustomerTicketCommand` | `subject`,`category`,`priority`,`body`,`relatedEntityType`,`relatedEntityId`(body); `Idempotency-Key`(header); `ActorUserId` server-derived | `category`/`priority` free text → `support.category_invalid`/`support.priority_invalid`; `subject`>200 → `support.subject_invalid`; `body`>4000 → `support.message.body_invalid`; `relatedEntityType`>64 → `support.related_type_invalid`; `idempotencyKey`>128 → `support.idempotency_key_invalid` | none | **VR** |
| 3 | GET `/tickets/{id}` (customer) | `GetCustomerTicketQuery` | `ticketId`(route,guid); `ActorUserId` server-derived | none (guid-constrained route) | — | **NV** |
| 4 | POST `/tickets/{id}/replies` (customer) | `ReplyCustomerTicketCommand` | `body`(body,string); `Idempotency-Key`(header) | `body` empty/>4000 → `support.message.body_invalid`; key>128 | none | **VR** |
| 5 | POST `/tickets/{id}/close` (customer) | `CloseCustomerTicketCommand` | `ticketId`(route,guid); `ActorUserId` server-derived | none | — | **NV** |
| 6 | POST `/tickets/{id}/reopen` (customer) | `ReopenCustomerTicketCommand` | `ticketId`(route,guid) | none | — | **NV** |
| 7 | GET `/tickets` (seller) | `ListSellerTicketsQuery` | `status`(query,string), `page`, `pageSize`; `SellerPartyId` server-derived | `status` free text | none | **VR** |
| 8 | POST `/tickets` (seller) | `CreateSellerTicketCommand` | same as #2 + server-derived `SellerPartyId` | same as #2 | none | **VR** |
| 9 | GET `/tickets/{id}` (seller) | `GetSellerTicketQuery` | `ticketId`(route,guid) | none | — | **NV** |
| 10 | POST `/tickets/{id}/replies` (seller) | `ReplySellerTicketCommand` | `body`(body), `Idempotency-Key`(header) | same as #4 | none | **VR** |
| 11 | POST `/tickets/{id}/close` (seller) | `CloseSellerTicketCommand` | `ticketId`(route,guid) | none | — | **NV** |
| 12 | POST `/tickets/{id}/reopen` (seller) | `ReopenSellerTicketCommand` | `ticketId`(route,guid) | none | — | **NV** |
| 13 | GET `/tickets` (admin) | `ListAdminTicketsQuery` | `status`,`requesterKind`,`category`,`priority`,`q`(query,string), `page`,`pageSize` | **four** free-text enum-ish filters (`support.status_invalid`, `support.requester_kind_invalid`, `support.category_invalid`, `support.priority_invalid`) + unbounded `q` reaching `ILike` | none | **VR** |
| 14 | GET `/tickets/{id}` (admin) | `GetAdminTicketQuery` | `ticketId`(route,guid) | none | — | **NV** |
| 15 | POST `/tickets/{id}/replies` (admin) | `ReplyAdminTicketCommand` | `body`(body), `isInternalNote`(body,bool), `Idempotency-Key`(header) | `body` empty/>4000 | none | **VR** |
| 16 | PATCH `/tickets/{id}` (admin) | `PatchAdminTicketCommand` | `status`,`priority`,`assignedOperatorActorUserId`(body) | `status`/`priority` free text → `support.status_invalid`/`support.priority_invalid` | none | **VR** |
| 17 | GET `/demo-preview` (admin) | `GetSupportDemoPreviewQuery` | none (Development-gated, no input) | none | — | **NV** |

**Set-equality proof.**

- Endpoint-reachable request set derived from the 17 shipped `group.Map*` calls = 17 types.
- Classified set = **9 VR + 8 NV = 17**, each type exactly once, no orphan, no duplicate.
- `VR` (9): `ListCustomerTicketsQuery`, `ListSellerTicketsQuery`, `ListAdminTicketsQuery`,
  `CreateCustomerTicketCommand`, `CreateSellerTicketCommand`, `ReplyCustomerTicketCommand`,
  `ReplySellerTicketCommand`, `ReplyAdminTicketCommand`, `PatchAdminTicketCommand`.
- `NV` (8): `GetCustomerTicketQuery`, `GetSellerTicketQuery`, `GetAdminTicketQuery`,
  `CloseCustomerTicketCommand`, `CloseSellerTicketCommand`, `ReopenCustomerTicketCommand`,
  `ReopenSellerTicketCommand`, `GetSupportDemoPreviewQuery`.
- Concrete `AbstractValidator<T>` classes currently present: **0**. → `Validator-Coverage-State = GAPS`.

`NV` justification is non-circular in every case: the only caller-controlled value is a
`{ticketId:guid}` route segment whose malformed shape is rejected by the ASP.NET Core route
constraint before the handler is reached (never by parsing a string), plus server-derived actor ids
supplied by the authorization seam. `GetSupportDemoPreviewQuery` takes no input at all and is
`IsDevelopment()`-gated. No optional-value exemption is claimed.

---

## 10. Localization findings

**Present and correct**

- `Resources/SupportErrors.resx` + `SupportErrors.fa.resx` + `SupportErrorResourceSet` (owns the
  `support.` key prefix) + `SupportErrorCatalogContributor`.
- Machine-stable codes only; the module never emits Persian/English prose. The architecture guard
  already rejects any `throw new *Exception("...")` whose message is not a `support.*` machine code.
- Zero `Accept-Language` parsing; zero `ex.Message`-based response decisions in Endpoints.

**Defects (blocker B2)**

1. **`Application/Errors/SupportErrorCodes.cs` declares 9 codes but only 4 are ever referenced.**

   | Code | Declared | Referenced | Catalogued |
   | --- | --- | --- | --- |
   | `customer.session.required` | yes | yes (Customer endpoints) | **no** — owner is `FoundationErrorCatalogContributor` (correct: duplicate usage, single owner) |
   | `support.missing` | yes | yes | yes |
   | `support.rejected` | yes | yes | yes |
   | `support.reply.rejected` | yes | yes | yes |
   | `support.action.rejected` | yes | **no** | yes |
   | `support.patch.rejected` | yes | yes | yes |
   | `seller.authorization.denied` | yes | **no** | **no** — owner is Foundation (correct) |
   | `support.authorization.unavailable` | yes | via `SupportAdminAuthorizationCodes` | yes |
   | `support.demo.not_ready` | yes | yes | yes |

   `support.action.rejected` is registered but never emitted — a **localization/registration dead
   entry**. It must be either genuinely emitted or removed from the catalog (W1 decides, preserving
   the FE fallback string `support.action.rejected` — see §11).

2. **`Resources/SupportErrors.resx` localizes exactly ONE key** (`support.authorization.unavailable`).
   The five client-observable outcome codes (`support.missing`, `support.rejected`,
   `support.reply.rejected`, `support.patch.rejected`, `support.demo.not_ready`) resolve to an
   `ErrorDescriptor` whose `LocalizationKey` is the raw machine code with **no `.resx` entry**, so
   Persian text cannot be produced for them. This is `MISSING_INFRASTRUCTURE_USE`, not
   `HARDCODED_TEXT` — the code never hardcodes prose, but the resource set is hollow.

**Non-defect observations**

- The 20 internal directory/domain fault codes (`support.ticket_not_found`, `support.reply_closed`,
  …) are *not* client-visible contracts; they are mapped to the 5 public outcome codes by
  `SupportExceptionMapper`. They correctly need no `ErrorDescriptor`. They do need durable identity
  and localizable display text once the W1 typed-fault repair lands.
- Persian seed strings in `Seeds/SupportDevelopmentSeed.cs` are demo display values, not API text.

---

## 11. API result / error mapping findings

- **Success/expected-failure path: `CANONICAL`.** Every endpoint uses `ApiResponseFactory.From(result)`
  or `FromFailure(SemanticError)`. Zero local `ProblemDetails` builder, zero `catch`-and-map.
- `Results.NotFound()` appears once (demo-preview Development gate) and `Results.Json(..., 201)` twice
  (customer/seller create). Both are **deliberate shipped contract shapes**, identical to the
  certified `Story`/`Offer` "raw success DTO" precedent — not ad-hoc error mapping.
- `ISupportDirectory` returns **nullable DTOs** for "not found" and **throws
  `InvalidOperationException` with a machine-code message** for every other expected business fault;
  `SupportExceptionMapper` then maps by **exact message equality** against a hardcoded 20-code set.

  This is functionally correct but non-canonical on three counts:
  1. classification is by **message text equality** rather than a typed code property;
  2. the domain throws a **framework exception type** carrying a business code in `Message`;
  3. the known-code set is **duplicated** in `SupportExceptionMapper` instead of being owned by the
     code catalogue.

  Canonical repository precedent: `Returns` throws `Tooba.BuildingBlocks.ContractOperationException`
  (typed `Code` property) from Domain/Infrastructure and maps it in
  `Returns.Application.Composition.ReturnsOperation` filtered by `ReturnsErrorCodes.IsKnown(ex.Code)`;
  `Story` throws `SemanticException` directly from Domain and maps in
  `Story.Application.Composition.StoryOperation`. **W1 aligns Support with the Returns/Story
  typed-fault pattern (blocker B5).**

- `SupportErrorCodes` living in `Application.Errors` is why Host's admin adapter needed a
  hand-rolled `SupportAdminAuthorizationCodes` restatement (AMC-R1 workaround). Creating
  `Tooba.Support.Contracts` removes that workaround pressure without breaking the seam.

---

## 12. Logging / OpenTelemetry / correlation findings

| Check | Result |
| --- | --- |
| `ILogger<T>` usage in module | 0 (module has no logging needs) |
| `Console.WriteLine` / `Debug.WriteLine` | 0 |
| second `ActivitySource` / `Meter` | 0 |
| `ActivitySource.StartActivity` direct call | 0 |
| manual `traceparent` parsing | 0 |
| parallel correlation header / `AsyncLocal` | 0 |
| sensitive-data logging (tokens, cookies, secrets, `Authorization`) | 0 |
| cross-module call traced via `IModuleCallTracer` | N/A — Support makes exactly one cross-module **port** call (Notification) and no `ActivitySource` is required by the certified precedent for a Contracts-port call |

`Logging-State = CANONICAL`, `Sensitive-Logging-State = NONE`, `OpenTelemetry-State = CANONICAL`,
`Correlation-Trace-State = CANONICAL`.

---

## 13. File cohesion / splitting plan

No file requires splitting. `Directories/SupportDirectory.cs` (511 LOC) is a single-responsibility
directory for one aggregate and its messages; it stays `WATCH` (below the 800 ceiling).

The only cohesion *smell* is **intra-file type grouping**, which W2 resolves by foldering, not by
splitting behaviour:

| File | Types declared | W2 action |
| --- | --- | --- |
| `Application/Models/SupportDirectoryInputs.cs` | `CreateTicketCommand`, `ReplyTicketCommand`, `AdminTicketPatchCommand`, `AdminTicketListQuery`, `AudienceTicketListQuery` | `Models/` (capability-first) |
| `Application/Models/SupportDtos.cs` | `TicketMessageDto`, `TicketSnapshotDto`, `TicketListRowDto`, `TicketListPageDto` | `Models/` |
| `Application/Ports/ISupportDirectory.cs` | `ISupportDirectory`, `SupportEnumParsing` | split parser to `Composition/` |
| `Application/Errors/SupportExceptionMapper.cs` | `SupportExceptionMapper` | replaced by typed-fault seam in W1 |
| `Endpoints/Customer/SupportCustomerEndpoints.cs` | `CreateTicketBody`, `ReplyTicketBody`, `SupportCustomerEndpoints` | `Models/` + endpoint file |
| `Infrastructure/Adapters/SupportDemoSnapshot.cs` | `SupportDemoIds`, `SupportDemoSnapshotStore`, `SupportDemoPreviewAdapter` | `Adapters/` (+ `Development/` for store) |

---

## 14. Folder-Granularity-State

`TECHNICAL_AXIS_FIRST` — `Application/Commands/<UseCase>/` and `Application/Queries/<UseCase>/` are
the **primary** axis, and **every one of the 17 leaves contains exactly one production `.cs`**
(verified by file count, not by declared type count). Support has a real capability axis
(ticket lifecycle across three audiences) so this is non-canonical per the Structure skill.

Also `OVER_FOLDERED` by the single-file-leaf rule: 17 unjustified single-file request folders.

Other structural divergences:

| Item | Today | Canonical |
| --- | --- | --- |
| Migrations | `Infrastructure/Migrations/` | `Infrastructure/Persistence/Migrations/` (27 of 32 modules; `Content`/`Reviews`/`Wallet` are legacy) |
| Enum folder | `Domain/ValueObjects/SupportEnums.cs` (single file, 5 enums) | `Domain/Enums/` (or one enum per file) |
| Demo snapshot store | `Adapters/SupportDemoSnapshot.cs` | `Development/` (it is a dev-preview store) |
| Seed folder | `Infrastructure/Seeds/` (only module in repo) | `Development/` |
| Composition entry | `DependencyInjection/SupportModule.cs` | `DependencyInjection/` ✅ (matches `Story`/`Returns`/`Settlement`) |

`Solution-Explorer-State = CANONICAL` — `Tooba.slnx:239-244` already groups five projects under
`/Modules/Support/`, and `Tooba.Support.Contracts` will need to be added there in W1.

`Path-Namespace-State = EXACT` (guard-verified for the four existing projects).
`Physical-Copy-State = CLEAN`. `Root-Allowlist-State = ENFORCED` (root files: only
`SupportEndpointModule.cs` in Endpoints; other projects have zero root `.cs`).

**Structure-Handoff-State = `REQUIRED`.**

---

## 15. Behavior-preservation checklist (must not change)

- 17 routes, verbs, and paths exactly as listed in §8
- Success response shapes: `TicketSnapshotDto` / `TicketListPageDto` / `SupportDemoSnapshotDto` JSON
  field names and casing; `201` + raw DTO for customer/seller create
- Status codes: 401 for missing customer session, 403 for seller/admin denial, 503 for
  `support.authorization.unavailable` and `support.demo.not_ready`, 404 for `support.missing`
- All client-observable machine codes (FE `support-api.ts` depends on `support.rejected`,
  `support.reply.rejected`, `support.action.rejected`, `support.patch.rejected`, `support.missing`)
- Authorization semantics: capability ids `support.view` / `support.create` / `support.reply` /
  `support.manage`; fail-closed on `AuthorizationDecisionKind.Unavailable`
- Ticket state transitions: `Open → WaitingFor* → Resolved/Closed`, `CloseByRequester`,
  `ReopenByRequester`, `ApplyAdminPatch`, `MarkWaitingAfterAdminPublicReply`
- Idempotency semantics: unique filtered indexes on `support_tickets.idempotency_key` and
  `ticket_messages.idempotency_key`; `:first` suffix for the create-time first message
- Admin patch clear-assignee sentinel (`Guid.Empty` ⇒ clear)
- Persistence schema `support`, tables `support_tickets` / `ticket_messages` / outbox, all indexes
  and `HasFilter` predicates
- Migration id `20260827120000_InitialSupport` and its Up/Down behavior
- Development seed ids/values and the published `SupportDemoSnapshotStore` snapshot
- Notification contract: `NotificationSemanticTypes.SupportAdminReply`, source event id
  `support.admin-reply:{messageId}`, dedup key `support.ticket.admin_reply`, target routes
- Module registration: `AddSupportEndpointPresentation`, `MapSupportEndpoints`, CQRS assembly
  registration, `SupportModule` DI, `ModuleMigrationRegistry` descriptor

---

## 16. Migration order (W1 → W2 → W3)

**W1 (Migrate)**
1. Create `Tooba.Support.Contracts` (Errors only) + `Tooba.slnx` `/Modules/Support/` entry.
2. Move `SupportErrorCodes` → `Tooba.Support.Contracts.Errors`; add `IsKnown` / `IsHttpReachable`
   sets; keep every existing code string byte-identical.
3. Introduce the typed fault primitive (`ContractOperationException`) in Domain/Infrastructure and
   replace `InvalidOperationException("<code>")`; replace `SupportExceptionMapper` with the canonical
   `Application/Composition/SupportOperation` seam (Story/Returns shape).
4. Add transport-shape FluentValidation for the 9 `VR` requests + `Application/Validation/
   SupportValidationCodes.cs`; re-derive the 17-row matrix; verify DI discovery through
   `AddToobaCqrsFoundation`.
5. Localize every client-observable code bilingually in `SupportErrors.resx` / `.fa.resx`; resolve the
   `support.action.rejected` dead registration honestly.
6. Persian XML documentation pass (32-doc standard) on touched/re-written members.
7. Add `W1` durable guard.

**W2 (Structure)**
8. Capability-first foldering: `Application/Tickets/{Commands,Queries,Models,Ports,Validators}`,
   `Application/Composition/`, `Application/Errors/`; flatten the 17 single-file leaves.
9. `Infrastructure/Persistence/Migrations/` move; `Development/` for seed + bootstrap + demo store;
   `Domain/Enums/`.
10. Path↔namespace exactness for every moved file; manifest allowlists; solution grouping.
11. Add `W2` structure guard.

**W3 (Certify)**
12. Re-verify every gate against current disk; author the module-scoped AMSC certification guard
    (route set-equality, request↔handler↔validator set equality, catalogue uniqueness, path↔namespace,
    root allowlist, no foreign Application/Infrastructure/Domain, no join).
13. Promote the manifest entry (`structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`, remove
    `Support` from `uncertifiedHttpOwningModules`), write `supportAmsc001W3` SoT, add the Master
    Recovery checkpoint, and record the AMSC evidence tree.

---

## 17. Verification plan

| Wave | Focused validation |
| --- | --- |
| W1 | build `Support.Contracts/Domain/Application/Infrastructure/Endpoints/Tests`; `Tooba.Support.Tests` (12 existing); new validator-coverage guard; `HostSupportAmcGuardTests`; `SupportFoundationTests`; `HostModuleEndpointOwnershipTests`; Host build (composition/DI touched) |
| W2 | build affected projects; new structure guard; `.slnx` parse; `SupportArchitectureGuardTests` (allowlists updated honestly, assertions not weakened) |
| W3 | full module guard set + `TmarSourceSizeGuard` for touched files + `TmarDurableGuardTests` + `HostRootFinalCertGuardTests` |

No broad repository-wide test run. No guard may be weakened; baselines may only be widened for a
file that genuinely changed classification.

---

## 18. Certification blockers (to be closed by W1–W3)

| ID | Blocker | Owner wave |
| --- | --- | --- |
| **B1** | `Tooba.Support.Contracts` missing although the module owns stable cross-boundary codes and the only HTTP-owning module without a Contracts project | W1 |
| **B2** | `SupportErrors.resx` localizes 1 of 6 client-observable codes; `support.action.rejected` registered but never emitted | W1 |
| **B3** | 9 declared outcome codes, only 6 catalogued/reachable; code catalogue duplicated in `SupportExceptionMapper` | W1 |
| **B4** | Validator matrix not exhaustive: 0 validators for 9 caller-controlled malformable requests | W1 |
| **B5** | Fault classification by `InvalidOperationException.Message` text equality instead of a typed code (non-canonical vs `Returns`/`Story`) | W1 |
| **B6** | `TECHNICAL_AXIS_FIRST` + 17 single-file request leaf folders; `Infrastructure/Migrations` at wrong depth; `Seeds/` non-canonical | W2 |
| **B7** | No `structureCertified` manifest entry, no `supportAmsc001W0..W3` SoT lineage, no AMSC evidence tree, no Master Recovery AMSC checkpoint | W2/W3 |
| **B8** | No module-scoped AMSC durable guard (only the Host-evacuation `HostSupportAmcGuardTests`) | W3 |

**No architecture decision is required.** Every blocker has a canonical repository precedent and is
behavior-preserving to close.

---

## 19. Disposition

```
Final-Disposition: READY_TO_MIGRATE
Structure-Handoff-State: REQUIRED
Behavior-Preservation-Risk: LOW
Host-Illegal-Authority: ZERO
Cross-Module-Coupling-State: LEGAL_CONTRACTS_ONLY (single edge: Notification.Contracts)
Validator-Coverage-State: GAPS (9 VR / 8 NV, 0 validators)
Localization-State: MISSING_INFRASTRUCTURE_USE
Stable-Error-Code-State: UNREGISTERED_CODES
```
