# TB-TMAR-USERPREFERENCE-AMSC-001-W0 — Analyze (`tooba-architecture-analyze`)

```text
TASK:          TB-TMAR-USERPREFERENCE-AMSC-001-W0
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-analyze (V2)
TARGET:        src/backend/Modules/UserPreference/Tooba.UserPreference.*
STARTING HEAD: e3f9f160
LOCK VERSION:  ARCH-COMPLETE-002
STATE:         ANALYZE_COMPLETE
VERDICT:       READY_TO_MIGRATE
```

Analysis-only wave. **No production code changed in W0.** Evidence + SoT only.

---

## 0. Applicability Gate (MANDATORY BEFORE TARGET SHAPE)

**Classification: `HTTP_OWNING`.**

Evidence (route ownership is module-owned, not Host-owned):

| Probe | Result |
| --- | --- |
| Module route mapper | `UserPreferenceEndpointModule.MapUserPreferenceModuleEndpoints` maps **3 route groups** |
| Route group 1 | `/v1/customer/preferences` → `GET /`, `PUT /` (customer locale) |
| Route group 2 | `/v1/admin/operator/preferences` → `GET /`, `PUT /` (admin operator locale) |
| Route group 3 | `/v1/admin/ui-preferences` → `GET /{key}`, `PUT /{key}` (admin UI preference) |
| Module-owned route count | **6** |
| Host-owned UserPreference route count | **0** |
| Host `MapUserPreferenceModuleEndpoints()` call | present at `Program.cs:425` (composition only) |
| Frontend consumers | `app/customer-panel/customer-preferences-api.ts`, `app/admin/operator-settings-api.ts`, `app/admin/saved-view-store.ts` — all hit the three module route groups |

`Tooba.UserPreference.Endpoints` is **not** ceremony: it owns real, frontend-consumed HTTP
routes. This is the certified `HTTP_OWNING` shape, so the endpoint/CQRS/validator gates apply
in full for W1/W2/W3.

Consequences for the target plan:

```text
Endpoints          = APPLICABLE (module-owned, 3 route groups / 6 routes)
CQRS               = APPLICABLE (MediatR via AddToobaCqrsFoundation)
Validator Matrix   = APPLICABLE (4 endpoint-reachable requests)
```

---

## 1. Target analyzed

Full production inventory (33 `.cs` production files + 5 migration artifacts, 5 projects):

```text
Tooba.UserPreference.Contracts/            (3 .cs)
  Errors/UserPreferenceErrorCodes.cs
  Errors/UserPreferenceErrorCatalogContributor.cs
  Errors/UserPreferenceErrorResourceSet.cs
  Resources/UserPreferenceErrors.resx          (+ .fa.resx)
Tooba.UserPreference.Domain/               (2 .cs)
  Aggregates/UserPreference.cs, UiPreference.cs
Tooba.UserPreference.Application/          (11 .cs)
  Composition/UserPreferenceOperation.cs
  LocalePreferences/Commands/UpsertUserPreferenceCommand.cs
  LocalePreferences/Queries/GetUserPreferenceQuery.cs
  LocalePreferences/Validators/UpsertUserPreferenceCommandValidator.cs
  UiPreferences/Commands/UpsertUiPreferenceCommand.cs
  UiPreferences/Queries/GetUiPreferenceQuery.cs
  UiPreferences/Validators/UpsertUiPreferenceCommandValidator.cs
  UiPreferences/Validators/GetUiPreferenceQueryValidator.cs
  Models/UserPreferenceModels.cs
  Ports/IUserPreferenceDirectory.cs
  Ports/IUiPreferenceDirectory.cs
Tooba.UserPreference.Endpoints/            (6 .cs)
  UserPreferenceEndpointModule.cs
  Admin/IUserPreferenceAdminAuthorizer.cs
  Admin/UserPreferenceAdminEndpoints.cs
  Admin/UiPreferenceAdminEndpoints.cs
  Customer/UserPreferenceCustomerActorResolver.cs
  Customer/UserPreferenceCustomerEndpoints.cs
Tooba.UserPreference.Infrastructure/       (11 .cs + 5 migration artifacts)
  UserPreferenceModule.cs
  Development/UserPreferenceDevelopmentSeed.cs
  Directories/UserPreferenceDirectory.cs, UiPreferenceDirectory.cs
  Persistence/UserPreferenceDbContext.cs
  Persistence/UserPreferenceOutboxRegistration.cs
  Persistence/Migrations/{20260827215300_InitialUserPreference,20260828020000_AddUiPreferences}(.Designer).cs,
  Persistence/Migrations/UserPreferenceDbContextModelSnapshot.cs
```

Machine-verified path↔namespace scan (`inspect.cjs`, all 5 projects):

```text
projects = Tooba.UserPreference.Application, Tooba.UserPreference.Contracts, Tooba.UserPreference.Domain,
           Tooba.UserPreference.Endpoints, Tooba.UserPreference.Infrastructure
per-project .cs = {Application:11, Contracts:3, Domain:2, Endpoints:6, Infrastructure:11}
total .cs = 33 mismatches = 0
```

Repository context read: `AGENTS.md`, `TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`,
`TMAR-architecture-locks.md`, `tmar-current-state.json`, `tmar-module-structure-manifests.json`,
plus the four AMSC skills and the certified `Tax` (latest `INTERNAL_ONLY`) and `Pricing`/`Wishlist`
(`HTTP_OWNING`) surfaces.

---

## 2. Responsibility map

| Responsibility | Owner | Current home | Verdict |
| --- | --- | --- | --- |
| Locale preference invariant (`fa`/`en` only) | UserPreference | `Domain/Aggregates/UserPreference.cs` | correct |
| Keyed UI preference invariant | UserPreference | `Domain/Aggregates/UiPreference.cs` | correct |
| Locale read/write use cases | UserPreference | `Application/LocalePreferences/*` | correct |
| Keyed UI preference read/write use cases | UserPreference | `Application/UiPreferences/*` | correct |
| Application port + snapshots/write inputs | UserPreference | `Application/Ports`, `Application/Models` | correct |
| HTTP boundary (3 route groups, 6 routes) | UserPreference | `Endpoints/{Customer,Admin}` | correct |
| Customer actor resolution seam | UserPreference | `Endpoints/Customer/UserPreferenceCustomerActorResolver.cs` | correct (consumes `Order.Contracts.Fulfillment.StorefrontGuestActor`) |
| Admin authorization port | UserPreference | `Endpoints/Admin/IUserPreferenceAdminAuthorizer.cs` | correct (Host supplies the adapter) |
| Persistence (own `user_preference` schema) | UserPreference | `Infrastructure/Persistence/*` | correct |
| Outbox translation | UserPreference | `Infrastructure/Persistence/UserPreferenceOutboxRegistration.cs` | correct (no external event this version) |
| Module DI composition | UserPreference | `Infrastructure/UserPreferenceModule.cs` | correct |
| Stable error-code home + catalog + resource set | UserPreference | `Contracts/Errors/*` | correct home, **defective semantics (see §9)** |
| Typed-fault → `Result` seam | UserPreference | `Application/Composition/UserPreferenceOperation.cs` | present, **incomplete (see §10)** |

`MUST_SPLIT` files: **none.** Every file is single-responsibility and within the `ARCH-SIZE-001`
ceiling (largest production file: `Domain/Aggregates/UiPreference.cs` at 77 LOC).

---

## 3. Ownership map

```text
UserPreference OWNS:  per-actor locale preference state, per-actor keyed UI preference state (grid
                      saved views), its own HTTP boundary (6 routes), its own user_preference schema
                      and outbox, and the stable error-code vocabulary for its own outcomes.
UserPreference DOES NOT: own identity/session business rules (Identity/Foundation), the guest actor
                      vocabulary (Order.Contracts.Fulfillment), admin authorization policy (Host
                      platform adapter + AccessControl), customer profile (CustomerProfile),
                      operator profile (OperatorProfile), catalog/grid rendering (frontend).
```

No file contains responsibilities owned by two modules. Ownership is `correct`.

---

## 4. Current illegal dependencies

**ZERO.**

| Check | Result |
| --- | --- |
| Foreign `.Application` / `.Infrastructure` / `.Domain` project reference in any UserPreference project | **0** |
| Foreign `DbContext` / `DbSet` / foreign schema read | **0** |
| Cross-module EF/SQL join | **0** |
| Foreign FK (Party/Order/Catalog) in the `user_preference` schema | **0** — `user_preferences.OwnerUserId` is the PK, `ui_preferences` keys on `PreferenceId` with a unique `(ActorUserId, Key)` index; no navigation crosses a module boundary |
| `Tooba.UserPreference.*` project referencing `Tooba.Host` | **0** |
| `TypeForwardedTo` / namespace alias workaround | **0** |
| `ex.Message` classification / `PlatformHttpException` catch-and-map in Endpoints | **0** (repaired by AMC-001 W4-R1) |

The only cross-module edge is a **legal Contracts-only** one:

```text
Tooba.UserPreference.Endpoints.csproj -> ..\..\Order\Tooba.Order.Contracts\Tooba.Order.Contracts.csproj
used by: Customer/UserPreferenceCustomerActorResolver.cs -> StorefrontGuestActor.ActorId
```

`Cross-Module-Coupling-State = NONE` beyond that single approved Contracts seam.

**Inbound**: `Host` touches UserPreference only at the composition root (`Program.cs` using/map call,
presentation registration, module-assembly registration for CQRS, `IUserPreferenceAdminAuthorizer`
adapter binding, `Tooba.Host.csproj` references) plus the Host-admin authorizer implementation
`Host/Admin/Access/Authorizers/HostUserPreferenceAdminAuthorizer.cs`. `Host/Preferences` is
CLOSED_HOST_ZERO (certified AMC-001 W4).

---

## 5. Cross-module join inventory

**NONE.** Verified by reading `UserPreferenceDbContext`, its `OnModelCreating` (both entity
configurations are inline and `HasDefaultSchema("user_preference")`), and both migrations
(`20260827215300_InitialUserPreference`, `20260828020000_AddUiPreferences`). No `HasOne/WithMany`,
no `HasForeignKey` pointing outside `user_preference`, no raw SQL, no `FromSql`.

---

## 6. Contracts-only replacement map

Already satisfied. The Contracts surface is the module boundary:

| Contract | Consumer | Purpose |
| --- | --- | --- |
| `UserPreferenceErrorCodes` (`Tooba.UserPreference.Contracts.Errors`) | module internals + composed error catalog | module-owned stable code identity |
| `UserPreferenceErrorCatalogContributor` | Host composition (`UserPreferenceModule.AddServices`) | descriptor registration |
| `UserPreferenceErrorResourceSet` | Host composition | localized text ownership for the `preference.` / `ui_preference.` keyspace |

No new cross-module contract is required. W1 adds only the module-owned declared-code guard
(`KnownCodes` + `IsKnown`) — a legal module-boundary semantic per the Analyze
"semantic contract ownership gate".

---

## 7. CQRS / MediatR gaps

**COMPLIANT.** All 4 endpoint-reachable requests are real `IRequest<Result<...>>` with real
`IRequestHandler<,>` implementations, dispatched from thin endpoints through `ISender`:

| Request | Kind | Handler | Dispatch |
| --- | --- | --- | --- |
| `UpsertUserPreferenceCommand` | Command | `UpsertUserPreferenceCommandHandler` | `ISender.Send` (customer PUT + admin PUT) |
| `GetUserPreferenceQuery` | Query | `GetUserPreferenceQueryHandler` | `ISender.Send` (customer GET + admin GET) |
| `UpsertUiPreferenceCommand` | Command | `UpsertUiPreferenceCommandHandler` | `ISender.Send` (admin UI PUT) |
| `GetUiPreferenceQuery` | Query | `GetUiPreferenceQueryHandler` | `ISender.Send` (admin UI GET) |

No endpoint business logic, no direct Directory/DbContext call from an endpoint, no custom
dispatcher, no Host bypass. MediatR registration goes through `AddToobaCqrsFoundation`
(`Program.cs:191` registers the `Tooba.UserPreference.Application` assembly).

---

## 8. Validation classification matrix

Set-equality gate: the reachable request set is derived from the shipped route mappings
(`UserPreferenceCustomerEndpoints.Map`, `UserPreferenceAdminEndpoints.Map`,
`UiPreferenceAdminEndpoints.Map`) and their `ISender.Send` call sites. Four distinct request types
are reachable, each with exactly one classification.

| # | Route + verb | Request | Caller-controlled inputs & provenance | Transport shape owner | Validator | Classification |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `PUT /v1/customer/preferences` | `UpsertUserPreferenceCommand(Guid ActorUserId, string Locale)` | `ActorUserId` = **server-derived** (`ICurrentAuthenticatedUser` / dev header / `StorefrontGuestActor`); `Locale` = body (`UserPreferenceWriteRequest.Locale`) | transport | `UpsertUserPreferenceCommandValidator` (NotEmpty on `ActorUserId`, `Locale`) | `VALIDATOR_REQUIRED` |
| 2 | `GET /v1/customer/preferences` | `GetUserPreferenceQuery(Guid ActorUserId)` | `ActorUserId` = **server-derived**; no other input | — | — | `NO_VALIDATOR_REQUIRED` (no caller-controlled transport shape) |
| 3 | `PUT /v1/admin/ui-preferences/{key}` | `UpsertUiPreferenceCommand(Guid ActorUserId, string Key, string JsonPayload)` | `ActorUserId` = server-derived (admin authorizer); `Key` = route; `JsonPayload` = body (`JsonElement.GetRawText()` after an endpoint `Undefined/Null` guard) | transport | `UpsertUiPreferenceCommandValidator` (NotEmpty on `ActorUserId`, `Key`, `JsonPayload`) | `VALIDATOR_REQUIRED` |
| 4 | `GET /v1/admin/ui-preferences/{key}` | `GetUiPreferenceQuery(Guid ActorUserId, string Key)` | `ActorUserId` = server-derived; `Key` = route | transport | `GetUiPreferenceQueryValidator` (NotEmpty on `Key`) | `VALIDATOR_REQUIRED` |
| — | `PUT /v1/admin/operator/preferences` | `UpsertUserPreferenceCommand` | same type as #1 | transport | same validator | (shares row 1) |
| — | `GET /v1/admin/operator/preferences` | `GetUserPreferenceQuery` | same type as #2 | — | — | (shares row 2) |

`Validator-Coverage-State = EXHAUSTIVE` — 4 of 4 requests classified, 3 `VALIDATOR_REQUIRED` with a
present validator, 1 `NO_VALIDATOR_REQUIRED` with a proven server-derived-only provenance.

Discovery is via `AddToobaCqrsFoundation` assembly scanning (validators live in the
`Tooba.UserPreference.Application` assembly that `Program.cs:191` already registers).

**W0 finding (not a coverage gap, a robustness gap):** row 3's `JsonPayload` empty/whitespace case is
covered by both the endpoint `Undefined/Null` guard (`ui_preference.json_required`) and the validator
(`ui_preference.validation.json_required`); rows 1–4 are exhaustive. No new validator is required by
W1.

---

## 9. Localization findings

**Current state: `CANONICAL_RUNTIME_CORRECT_LOGICAL_NAME_IMPLICIT`.**

The *shape* is canonical — 9 `ErrorDescriptor`s registered once by
`UserPreferenceErrorCatalogContributor`, `LocalizationKey = code`, and a single
`UserPreferenceErrorResourceSet` owning the `preference.` / `ui_preference.` keyspace. EN **and** FA
both resolve at runtime. One hardening gap remains.

### 9.1 Defect A — the embedded-resource logical names are implicit, not locked

`Tooba.UserPreference.Contracts.csproj` has **no** `EmbeddedResource` item and **no**
`LogicalName`, unlike every certified sibling (`Tax`, `Pricing`, `Wishlist`, …):

```text
# Tooba.UserPreference.Contracts.csproj — actual
<ItemGroup>
  <ProjectReference Include="..\..\..\BuildingBlocks\Tooba.BuildingBlocks\Tooba.BuildingBlocks.csproj" />
</ItemGroup>
```

Machine proof (reflection over the **actually built** assemblies — this supersedes the W0 draft
claim that the Persian resource was dead):

```text
DLL: Tooba.UserPreference.Contracts.dll
GetManifestResourceNames() -> [ "Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.resources" ]

DLL: fa/Tooba.UserPreference.Contracts.resources.dll   (culture = fa)
GetManifestResourceNames() -> [ "Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.fa.resources" ]
```

Both names happen to equal the exact names
`UserPreferenceErrorResources.Manager = new ResourceManager("Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors", ...)`
resolves, so EN/FA localization **works today**. The real, narrower defect is that this equality is
an *implicit* side effect of the SDK convention (`EmbeddedResourceUseDependentUponConvention` +
default culture suffixing) rather than an explicit, locked contract. Certified siblings pin it:

Canonical comparison (`Tax`):

```xml
<EmbeddedResource Update="Resources\TaxErrors.resx">
  <LogicalName>Tooba.Tax.Contracts.Resources.TaxErrors.resources</LogicalName>
</EmbeddedResource>
<EmbeddedResource Update="Resources\TaxErrors.fa.resx">
  <LogicalName>Tooba.Tax.Contracts.Resources.TaxErrors.fa.resources</LogicalName>
</EmbeddedResource>
```

W1 adds the same explicit pair so the logical-name contract becomes a locked, durable invariant
(and so a future change to `EmbeddedResourceUseDependentUponConvention` cannot silently break
Persian). This is behavior-preserving: the resolved names are byte-identical to the current ones.

### 9.2 Defect B — the shared cross-cutting session code is misclassified by the module guard

`UserPreferenceErrorCodes` declares `SessionRequired = "customer.session.required"` as a *module*
code, while the descriptor and both-culture resources for that code are owned by
`FoundationErrorCatalogContributor` (`Tooba.BuildingBlocks.Presentation.Errors.FoundationErrorCodes.CustomerSessionRequired`,
`Classification.Forbidden`, HTTP 401). The contributor correctly does **not** re-register it, but the
code constant sitting inside the module's declared set is exactly the ambiguity the canonical
declared-code guard must resolve. Certified siblings keep the constant (so the module can *consume*
the code) while explicitly excluding it from the module-owned declared set:

- `CustomerProfileErrorCodes` — declares `SessionRequired` with a doc-comment stating the descriptor
  and both-culture resources are Foundation-owned;
- `AddressBookModuleAmsc001W3CertGuardTests` — excludes `SessionRequired` from the locally-owned
  constant set;
- `SettlementModuleAmsc001W1MigrateGuardTests` / `ReturnsModuleAmsc001W1MigrateGuardTests` /
  `SupportModuleAmsc001W1MigrateGuardTests` — assert
  `Assert.False(XErrorCodes.IsKnown("customer.session.required"))`.

`UserPreference` must therefore be explicit: `IsKnown("customer.session.required") == false` (it is
not a module use-case fault) while the constant stays for the endpoint's `api.FromFailure` path.

### 9.3 Other localization findings

- Zero hard-coded Persian user-facing strings in Domain/Application/Endpoints/Infrastructure.
- Zero `exception.Message` / `ex.Message` used as a localized or user-facing contract.
- Zero `Accept-Language` parsing in endpoints.
- Zero duplicate/parallel localization mechanism.
- **The `preference.` / `ui_preference.` keyspace is exclusively UserPreference's** — verified by a
  repository-wide scan of every other module's `.cs`/`.resx` for those prefixes: **0** matches. So
  the resource set's `Owns()` prefix test is collision-free today, but it is prefix-based rather
  than exact-code-based; W1 makes it derive from the declared-code set (the certified
  `TaxErrorResourceSet` shape) so a future foreign `preference.*` code cannot be silently claimed.
- **No test asserts the UserPreference error text** (verified by scan); therefore switching the
  resource set to code-set ownership and making the logical names explicit changes **no** asserted
  behavior and **no** resolved string.

---

## 10. API result / error mapping findings

**CANONICAL, with one seam gap.**

- Zero `Results.Json` / `Results.BadRequest` / `Results.Problem` / local `ProblemDetails` builder in
  `Tooba.UserPreference.Endpoints` (guarded today by
  `UserPreferenceModuleAmcW4CertGuardTests.UserPreference_endpoints_have_zero_catch_and_map_for_platform_or_semantic`).
- Zero `catch (PlatformHttpException` / `FromPlatformException(` / `catch (SemanticException` in
  Endpoints (repaired by AMC-001 W4-R1).
- Every endpoint returns `api.From(result)` / `api.FromFailure(...)`.
- Endpoints reference exactly one module-owned stable code directly
  (`UserPreferenceErrorCodes.SessionRequired`, two sites in the customer GET/PUT) for the
  actor-unavailable branch — the canonical `SemanticError` → `api.FromFailure` path.

**The gap** is the typed-fault seam:

```csharp
// Application/Composition/UserPreferenceOperation.cs — actual (incomplete)
public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
{
    try { return Result.Success(await action()); }
    catch (SemanticException ex) { return Result.Failure<T>(ex.Error); }
}
```

`UserPreferenceOperation` maps only `SemanticException`. It has:

- no value-less `ExecuteAsync(Func<Task>)` overload;
- no `ContractOperationException` handling — so the Infrastructure directory/outbox layer, which is
  the layer that raises contract-boundary faults, has **no** typed mapping into `Result`.

The Infrastructure surface still carries a raw fault:

| File | Line | Fault |
| --- | --- | --- |
| `Infrastructure/Persistence/UserPreferenceOutboxRegistration.cs` | 23 | `throw new InvalidOperationException("UserPreference integration event is not registered.")` — English user-facing prose |

Per the certified `Inventory`/`Tax` precedent, that prose fault becomes the stable code
`user_preference.outbox.unmapped_event_type` (`ErrorClassification.Platform`, HTTP 500), and the
directory/outbox faults become `ContractOperationException` classified by `IsKnown`.

---

## 11. Logging / sensitive-data findings

**CANONICAL.** Zero `ILogger<T>`, zero `Console.WriteLine`, zero `Debug.WriteLine`, zero hand-rolled
writer, zero `ObservabilityLogScope` misuse in the UserPreference production surface. Nothing to
repair and nothing to add. `Sensitive-Logging-State = NONE`.

---

## 12. OpenTelemetry / correlation findings

**CANONICAL.** Zero `StartActivity`, zero manual `traceparent` parsing, zero competing correlation ID,
zero custom `AsyncLocal`/header/middleware. UserPreference issues no outbound cross-module call (it is
the *callee*), so no `IModuleCallTracer` decoration is required.
`OpenTelemetry-State = CANONICAL`, `Correlation-Trace-State = CANONICAL`.

---

## 13. File cohesion / splitting audit

| File | LOC | Classification |
| --- | --- | --- |
| `Domain/Aggregates/UiPreference.cs` | 77 | COHESIVE |
| `Infrastructure/Directories/UiPreferenceDirectory.cs` | 75 | COHESIVE |
| `Domain/Aggregates/UserPreference.cs` | 74 | COHESIVE |
| `Infrastructure/Persistence/UserPreferenceDbContext.cs` | 68 | COHESIVE |
| `Infrastructure/Directories/UserPreferenceDirectory.cs` | 64 | COHESIVE |
| `Endpoints/Customer/UserPreferenceCustomerEndpoints.cs` | 63 | COHESIVE |
| `Endpoints/Admin/UiPreferenceAdminEndpoints.cs` | 61 | COHESIVE |
| all remaining production files | ≤ 49 | COHESIVE |

No god-file. No file over the 800 LOC `ARCH-SIZE-001` ceiling. No `*Contracts.cs` Application dump.
No duplicate CQRS request shape. No cosmetic split required. `File-Cohesion-State = COHESIVE`.

Note: `Endpoints/Admin/UserPreferenceAdminEndpoints.cs` and
`Endpoints/Customer/UserPreferenceCustomerEndpoints.cs` each declare a private endpoint method pair
plus nothing else; `UiPreferenceWriteRequest` / `UserPreferenceWriteRequest` are endpoint transport
models co-located with their endpoint file — this mirrors the certified
`Support`/`CustomerProfile` endpoint-transport precedent and is cohesive, not a dump.

---

## 14. Target foldering (exact paths / namespaces)

`Structure-Handoff-State = REQUIRED` (W1 adds one Contracts declaration member, one descriptor and
one bilingual resource key; the physical tree itself is already `PROFESSIONAL_SHALLOW`).

```text
src/backend/Modules/UserPreference/
  Tooba.UserPreference.Contracts/                        namespace Tooba.UserPreference.Contracts
    Errors/UserPreferenceErrorCodes.cs                   -> Tooba.UserPreference.Contracts.Errors
    Errors/UserPreferenceErrorCatalogContributor.cs      -> Tooba.UserPreference.Contracts.Errors
    Errors/UserPreferenceErrorResourceSet.cs             -> Tooba.UserPreference.Contracts.Errors
    Resources/UserPreferenceErrors.resx                  (W1: explicit LogicalName)
    Resources/UserPreferenceErrors.fa.resx               (W1: explicit LogicalName)
  Tooba.UserPreference.Domain/                           namespace Tooba.UserPreference.Domain
    Aggregates/UserPreference.cs, UiPreference.cs        -> Tooba.UserPreference.Domain.Aggregates
  Tooba.UserPreference.Application/                      namespace Tooba.UserPreference.Application
    Composition/UserPreferenceOperation.cs               -> .Composition (W1: expand seam)
    LocalePreferences/{Commands,Queries,Validators}
    UiPreferences/{Commands,Queries,Validators}
    Models/UserPreferenceModels.cs
    Ports/IUserPreferenceDirectory.cs, IUiPreferenceDirectory.cs
  Tooba.UserPreference.Endpoints/                        namespace Tooba.UserPreference.Endpoints
    UserPreferenceEndpointModule.cs
    Admin/IUserPreferenceAdminAuthorizer.cs, UserPreferenceAdminEndpoints.cs, UiPreferenceAdminEndpoints.cs
    Customer/UserPreferenceCustomerActorResolver.cs, UserPreferenceCustomerEndpoints.cs
  Tooba.UserPreference.Infrastructure/                   namespace Tooba.UserPreference.Infrastructure
    UserPreferenceModule.cs
    Development/UserPreferenceDevelopmentSeed.cs
    Directories/UserPreferenceDirectory.cs, UiPreferenceDirectory.cs
    Persistence/UserPreferenceDbContext.cs
    Persistence/UserPreferenceOutboxRegistration.cs
    Persistence/Migrations/*
```

Solution Explorer (`src/backend/Tooba.slnx` lines 14-20) — canonical `/Modules/UserPreference/`
folder, **5 projects** matching disk exactly:

```text
/Modules/UserPreference/
  Tooba.UserPreference.Domain
  Tooba.UserPreference.Contracts
  Tooba.UserPreference.Application
  Tooba.UserPreference.Infrastructure
  Tooba.UserPreference.Endpoints
```

No project is added, removed, renamed or re-pathed. No solution-folder change is required.

---

## 15. Behavior-preservation baseline

Must remain byte-identical in observable behavior:

- **Routes**: the 6 routes, verbs, group prefixes and route templates
  (`/v1/customer/preferences`, `/v1/admin/operator/preferences`, `/v1/admin/ui-preferences/{key}`).
- **Response shapes**: `PreferenceLocaleResponse(Locale, CreatedAt?, UpdatedAt?)`,
  `UiPreferenceResponse(Key, Json?, UpdatedAt?)`, the `{}`/default-locale empty-row semantics
  (`UserPreferenceShapes.DefaultLocale = "fa"`), and the `"{}"` default for a missing UI row.
- **Status codes**: the 9 existing descriptor statuses (all `Validation` 400 today) plus the new
  `Platform` 500 for the outbox code; `customer.session.required` stays Foundation-owned 401.
- **Stable error codes**: the 9 existing code string values keep their exact wire identity.
- **Domain invariants**: `fa`/`en` only (`NormalizeLocale`), locale max length 8, UI key max length
  128, non-empty JSON payload, `Guid.Empty` actor rejection, `UiPreference.PreferenceId` new-guid on
  create.
- **Authorization semantics**: `IUserPreferenceAdminAuthorizer.RequireAuthorizedAsync` (Host adapter)
  and the customer actor resolution order (`ICurrentAuthenticatedUser` → dev header in
  Development/Testing → `StorefrontGuestActor.ActorId`).
- **Persistence semantics / schema**: the `user_preference` schema, `user_preferences` (PK
  `OwnerUserId`, `Locale` max 8 required) and `ui_preferences` (PK `PreferenceId`, unique
  `(ActorUserId, Key)`, required `JsonPayload`), the two migration ids
  `20260827215300_InitialUserPreference` and `20260828020000_AddUiPreferences` and their Up/Down
  semantics. **No migration, designer or snapshot file is touched.**
- **Outbox**: `Translate(...) => null` (no external event this version) and
  `ResolveEventClrType(...) => null` stay; only the unreachable `GetEventTypeName` fault type changes.
- **DI lifetimes**: outbox registration + catalog contributor + resource set singleton; both
  directories scoped; DbContext scoped with `OutboxSaveChangesInterceptor` — unchanged.
- **Localization keys**: the 9 existing keys and their EN/FA semantics are preserved; one key is added.

`Behavior-Preservation-Risk = LOW`.

---

## 16. Foundation / certified-module state

**`FOUNDATION_READY`.**

- All 5 projects exist and are grouped under `/Modules/UserPreference/` in `Tooba.slnx`.
- `tmar-module-structure-manifests.json` already contains a certified `UserPreference` entry
  (`structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`) and `UserPreference` is present
  exactly once in `structureLock.certifiedModules` (31 members).
- Path↔namespace is already `EXACT` (0 mismatches / 33 files); `Physical-Copy-State = CLEAN`;
  `Folder-Granularity-State = PROFESSIONAL_SHALLOW`; `Solution-Explorer-State = CANONICAL`.
- Prior AMSC lineage exists only as the **AMC-001** lineage
  (`userPreferenceAmc001` → `userPreferenceAmc001W4R1`, `COMPLETE_REFERENCE_PATTERN`,
  implementation commit `f2c3c2f2`, W4-R1 repair `26444f2f`). There is **no** AMSC-001 lineage.

Because the module is already certified, W1/W2/W3 must **extend** the existing structure and
manifest entry — never create a parallel structure, and never weaken an existing guard.

---

## 17. Migration order (W1 → W2 → W3)

**W1 — Migrate** (`tooba-architecture-migrate`)
1. `Contracts/Errors/UserPreferenceErrorCodes.cs` — add `KnownCodes` (ordinal `HashSet`) +
   `public static bool IsKnown(string?)`, seeded from the module-owned declared constants and
   **deliberately excluding** `SessionRequired` (Foundation-owned descriptor); add
   `OutboxUnmappedEventType = "user_preference.outbox.unmapped_event_type"`; keep every existing
   constant value byte-identical.
2. `Contracts/Errors/UserPreferenceErrorCatalogContributor.cs` — add the 10th descriptor
   (`OutboxUnmappedEventType`, `ErrorClassification.Platform`, HTTP 500); keep the explicit comment
   that `customer.session.required` stays Foundation-owned.
3. `Contracts/Errors/UserPreferenceErrorResourceSet.cs` — make `Owns` derive from the declared-code
   set (exact membership) instead of the two `StartsWith` prefixes.
4. `Contracts/Resources/UserPreferenceErrors.resx` / `.fa.resx` — add the
   `user_preference.outbox.unmapped_event_type` key (EN + FA).
5. `Contracts/Tooba.UserPreference.Contracts.csproj` — add the explicit `EmbeddedResource` +
   `LogicalName` pair for both `.resx` files (canonical `Tax`/`Pricing` shape) so the EN/FA
   logical-name contract is locked rather than implicit. Behavior-preserving (resolved names are
   byte-identical to the currently built ones).
6. `Application/Composition/UserPreferenceOperation.cs` — expand to the dual-mechanism certified seam:
   value + value-less overloads, `ContractOperationException ex when UserPreferenceErrorCodes.IsKnown(ex.Code)`
   → `Result.Failure(new SemanticError(ex.Code))`, plus the existing `SemanticException` mapping;
   unknown codes and unknown exceptions propagate untouched.
7. `Infrastructure/Persistence/UserPreferenceOutboxRegistration.cs` — replace the English prose
   `InvalidOperationException` with `throw new ContractOperationException(UserPreferenceErrorCodes.OutboxUnmappedEventType);`.
8. Add `UserPreferenceModuleAmsc001W1MigrateGuardTests`.

**W2 — Structure** (`tooba-architecture-structure`)
1. Machine-verify the physical tree (path↔namespace exact, root allowlists, `PROFESSIONAL_SHALLOW`
   capability-first, no stale/duplicate copy, no single-file request leaf, canonical `.slnx` grouping)
   and record it as the W2 structure authority for the touched surface.
2. Add `UserPreferenceModuleAmsc001W2StructureGuardTests` (durable structure guard: root allowlists
   and forbidden lists, capability-first trees, no technical-axis-first request tree, `.slnx`
   grouping, exact path↔namespace).
3. Record `structureAuthorityTask = TB-TMAR-USERPREFERENCE-AMSC-001-W2` on the manifest entry
   (structure authority moved from AMC-001 W4 to AMSC-001 W2 for the touched surface).
4. Update the pre-existing structure guards that read the manifest if and only if their intent is
   preserved (no assertion relaxed).

**W3 — Certify** (`tooba-architecture-certify`)
1. `UserPreferenceModuleAmsc001W3CertGuardTests` — durable cert lock (manifest certification + wave
   lineage, HTTP_OWNING route ownership, CQRS/validator set equality, single stable-code owner with
   declared-code guard + bilingual resource set equality + composed-catalog uniqueness, canonical
   typed seam, Contracts-only boundary, schema/migration set, Host closure, evidence tree).
2. Refresh the manifest `certificationNote` to record the AMSC-001 W0→W3 lineage and the now-locked
   explicit localization surface (staying a single certified `modules[]` entry — no pre-cert duplicate).
3. Record the W0/W1/W2/W3 lineage in `tmar-current-state.json`.
4. Master Recovery checkpoint in `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`.

---

## 18. Verification plan

Per wave: focused build of the touched UserPreference projects + `Tooba.Host.Tests`; focused run of
`UserPreferenceModuleAmcW*` guards, `HostPreferencesAmcGuardTests`, `ErrorCatalogUniqueCodeGuardTests`,
`TmarCompleteReferenceStructureGateTests`, `SettingsFoundationTests`, and the new AMSC guard.
No repository-wide sweep. No guard weakened. W2 and W3 each record the failing-test-id set against
the previous head to prove `NEW = 0`.

## 19. Certification blockers

**None blocking.** W0 records the findings to be closed by W1/W2/W3:

1. `UserPreferenceOperation` maps only `SemanticException` — missing value-less overload and missing
   `ContractOperationException` classification (§10).
2. Raw English-prose fault in `UserPreferenceOutboxRegistration.GetEventTypeName` — unregistered
   stable code (§10).
3. No declared-code guard (`KnownCodes` / `IsKnown`) on the Contracts error home, and the
   Foundation-owned `customer.session.required` constant is not explicitly excluded from the
   module-owned set (§9.2).
4. The embedded-resource logical names are implicit because the Contracts csproj lacks the
   canonical `EmbeddedResource`/`LogicalName` pair — EN/FA both resolve today only via SDK
   convention, so the bilingual contract is unlocked (§9.1).
5. `UserPreferenceErrorResourceSet.Owns` is prefix-based rather than declared-code-set-based (§9.3).

---

## 20. Structured state fields

```text
Foundation-State            : FOUNDATION_READY
Ownership-State             : correct
File-Cohesion-State         : COHESIVE
Oversized/God-File-State    : NONE (largest production file 77 LOC, ceiling 800)
Localization-State          : CANONICAL_RUNTIME_CORRECT_LOGICAL_NAME_IMPLICIT (EN+FA both resolve; names unlocked)
API-Result-Pattern-State    : CANONICAL (zero raw Results.*, zero catch-and-map, zero ex.Message classification)
Stable-Error-Code-State     : PARTIAL (9 declared+registered; 1 raw prose fault unregistered; no declared-code guard)
Typed-Fault-Seam-State      : INCOMPLETE (SemanticException only; no value-less overload; no ContractOperationException)
Logging-State               : CANONICAL (zero call sites)
Sensitive-Logging-State     : NONE
OpenTelemetry-State         : CANONICAL
Correlation-Trace-State     : CANONICAL
CQRS-State                  : COMPLIANT (4/4 real IRequest + IRequestHandler + ISender)
Validator-Coverage-State    : EXHAUSTIVE (3 VALIDATOR_REQUIRED + 1 NO_VALIDATOR_REQUIRED, set equality holds)
Contracts-Boundary-State    : CLEAN
Cross-Module-Coupling-State : NONE (one legal Contracts edge: Order.Contracts.Fulfillment guest actor)
Cross-Module-Join-State     : NONE
Persistence-Ownership-State : CORRECT (own user_preference schema, own outbox)
Endpoint-Ownership-State    : MODULE_OWNED (6 routes, Host route count 0)
Host-Residue-State          : ALLOWED_COMPOSITION_ROOT + ALLOWED_SECURITY_ADAPTER (HostUserPreferenceAdminAuthorizer)
Schema-Migration-State      : UNCHANGED
Behavior-Preservation-Risk  : LOW
Canonical-Reference-Used    : Tax + Inventory + Pricing (declared-code guard, dual typed-fault seam,
                              bilingual LogicalName resource pair, resource-set-by-code-set),
                              CustomerProfile/AddressBook (Foundation-owned session constant exclusion),
                              BuildingBlocks Result/SemanticError/ContractOperationException/
                              IErrorCatalogContributor/IErrorResourceSet/IErrorMessageLocalizer
Structure-Handoff-State     : REQUIRED
HTTP-Applicability          : HTTP_OWNING
Final-Disposition           : READY_TO_MIGRATE
```

## 21. Destination-integrity check

No Host folder is a destination. `src/backend/Host/Tooba.Host/**` receives **no** new production
file; the only Host-adjacent surface W1 touches is the already-certified
`HostUserPreferenceAdminAuthorizer` **binding** in `Program.cs` (unchanged) and the certified
`Host/Preferences` CLOSED_HOST_ZERO state (untouched). `HOST_ROOT_FINAL_CERTIFIED` /
`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` are preserved. No closed folder is used as a sink.
The `UserPreference` manifest entry is extended in place; no pre-cert duplicate is created.
