# TB-TMAR-STORY-AMSC-001 — Wave 0 (Analyze)

- **Skill:** `tooba-architecture-analyze` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Story/Tooba.Story.*`
- **Starting HEAD:** `e9301c4edf219d1129d93e6869f346a246522019`
- **Branch:** `main` (`HEAD == origin/main`)
- **Production code changed in this wave:** NONE (analysis only)
- **Host touched in this wave:** NONE

---

## 0. Prior art — this module already carries a closed AMC-001 lineage

Story is **not** an unrecovered module. `docs/architecture/tmar-current-state.json` already records
`storyModuleAmc001 … storyModuleAmc001W6Cert`, and
`docs/architecture/tmar-module-structure-manifests.json` already marks the module
`structureCertified: true` / `lockVersion: "ARCH-COMPLETE-002"` with the note
*"Certified by TB-TMAR-STORY-AMC-001-W6 under COMPLETE_REFERENCE_PATTERN."*

AMSC is therefore a **re-verification and hardening pass**, not an evacuation. The honest question for
W0 is not "what is broken?" but "which AMSC gates are *not yet actually proven* on the shipped
surface?" This analysis answers that with evidence rather than assumption, and it explicitly names the
small set of real defects it found instead of fabricating debt.

---

## 1. Target analyzed

| Project | Production `.cs` | Largest file |
| --- | --- | --- |
| `Tooba.Story.Contracts` | 1 | `Errors/StoryErrorCodes.cs` (15 LOC) |
| `Tooba.Story.Domain` | 7 | `Aggregates/Story.cs` (418 LOC) |
| `Tooba.Story.Application` | 12 | `Stories/Ports/IStoryDirectory.cs` (157 LOC) |
| `Tooba.Story.Infrastructure` | 9 + 5 migrations | `Directory/StoryDirectory.cs` (672 LOC) |
| `Tooba.Story.Endpoints` | 11 + 2 resx | `Admin/StoryAdminEndpoints.cs` (182 LOC) |

Totals: **5 projects, 40 production `.cs`, 2 `.resx`, 5 migration files** (2 migrations + 2 designers +
1 snapshot). Host owns **zero** Story production files: `src/backend/Host/Tooba.Host/Story` is absent,
and the only Host-side Story types are the legitimate platform adapter
`Host/Security/Seller/HostStorySellerAuthorizer.cs` plus composition-root lines in `Program.cs` and
`Composition/ToobaModuleComposition.cs`.

---

## 2. Structured State Fields

| Field | Value |
| --- | --- |
| **Foundation-State** | `FOUNDATION_READY` (module already `structureCertified: true` under ARCH-COMPLETE-002; all five projects present and capability-foldered) |
| **Ownership-State** | `correct` (every responsibility belongs to Story; Host owns no Story business authority) |
| **File-Cohesion-State** | `COHESIVE` (no `MUST_SPLIT`; 672-LOC `StoryDirectory.cs` is single-responsibility persistence for one aggregate) |
| **Oversized/God-File-State** | NONE above the 800-LOC ceiling. `Directory/StoryDirectory.cs` 672 LOC = `WATCH`, single responsibility; `Aggregates/Story.cs` 418 LOC = aggregate + its invariant helpers |
| **Localization-State** | `HARDCODED_TEXT` (see §9 / blocker B1 — the only true quality defect found) |
| **API-Result-Pattern-State** | `CANONICAL` — `ApiResponseFactory.From/Created` everywhere; zero `Results.Json/BadRequest/Problem`; zero local `ProblemDetails` builder |
| **Stable-Error-Code-State** | `CATALOGUED` — 5 `story.*` codes, exactly one `ErrorDescriptor` owner (`StoryErrorCatalogContributor`) |
| **Logging-State** | `CANONICAL` — zero `ILogger`, zero `Console/Debug.WriteLine`, zero second telemetry pipeline in the module |
| **Sensitive-Logging-State** | `NONE` |
| **OpenTelemetry-State** | `CANONICAL` — zero `ActivitySource.StartActivity`, zero second `ActivitySource`/`Meter` |
| **Correlation-Trace-State** | `CANONICAL` — no parallel correlation, no `AsyncLocal`, no manual `traceparent` |
| **CQRS-State** | `COMPLIANT` — 25 real `IRequest<T>` + `IRequestHandler<,>`, all dispatched from Endpoints through `ISender` |
| **Validator-Coverage-State** | `GAPS` (see §8 / blocker B2 — the shipped 25-request matrix is not yet exhaustive by the AMSC provenance rule) |
| **Contracts-Boundary-State** | `CLEAN` — Contracts holds only `Errors/StoryErrorCodes.cs`; no Application-internal type leaked, no mixed `*Contracts.cs` |
| **Cross-Module-Coupling-State** | `NONE` — the module references **no** foreign module at all; only `Tooba.BuildingBlocks`, `Tooba.ModuleContracts`, `Tooba.Persistence` |
| **Cross-Module-Join-State** | `NONE` |
| **Persistence-Ownership-State** | `CORRECT` — one `StoryDbContext`, schema `story`, module-owned migrations |
| **Endpoint-Ownership-State** | `MODULE_OWNED` — 25 routes across `/v1/admin/stories`, `/v1/seller/stories`, `/v1/storefront/stories`; Host route count `ZERO` |
| **Host-Residue-State** | `ALLOWED_SECURITY_ADAPTER` + `ALLOWED_COMPOSITION_ROOT` only |
| **Schema-Migration-State** | `UNCHANGED` (2 migrations, `InitialStory` + `AddStoryReviewOwnership`) |
| **Behavior-Preservation-Risk** | `LOW` (W1/W2 changes are documentation, foldering and one additive localization repair) |
| **Canonical-Reference-Used** | `BuildingBlocks` (`Result`/`ApiResponseFactory`/`SafeErrorMapper`/`IErrorDefinitionCatalog`/`IErrorResourceSet`/`ValidationBehavior`), `Promotion` for validation-code localization semantics, `Settlement`/`Offer`/`Promotion` for the `Contracts/Errors` + `Infrastructure/DependencyInjection` + `Infrastructure/Messaging` structure precedent, `docs/architecture/32-persian-code-documentation-standard.md` |
| **Final-Disposition** | `READY_TO_MIGRATE` |

Structural handoff fields (owned by the Structure skill, recorded here for handoff):

| Field | Value |
| --- | --- |
| **Folder-Granularity-State** | `PROFESSIONAL_SHALLOW` (capability-first `Stories/{Commands,Queries,Models,Ports,Presentation,Validators,Composition}`; no single-file request leaf; no technical-axis-first root) |
| **Solution-Explorer-State** | `CANONICAL` (`/Modules/Story/`, 5 projects, `Tooba.slnx:21-26`) |
| **Path-Namespace-State** | `EXACT` (0 mismatches over 40 production files) |
| **Physical-Copy-State** | `CLEAN` |
| **Root-Allowlist-State** | `ENFORCED` (Contracts/Domain/Application roots empty; Endpoints root `StoryEndpointModule.cs`; Infrastructure root `StoryModule.cs`) |
| **Single-File-Request-Leaf-State** | `ZERO` |
| **Technical-Axis-First-State** | `ZERO` |
| **Structure-Handoff-State** | `REQUIRED` (W2 owns the composition/outbox folder alignment, the `Directory` → `Directories` folder-name alignment and the final root-allowlist verdict) |

---

## 3. Responsibility map

| Responsibility | Location | Classification |
| --- | --- | --- |
| Story aggregate invariants, review workflow, publication window | `Domain/Aggregates/Story.cs`, `StoryItem.cs`, `Rules/StoryRules.cs` | `DOMAIN_RULE` |
| Story status/origin/review enums | `Domain/Enums/*` | `DOMAIN_RULE` |
| Stable tenant key → `Guid` mapping | `Domain/Tenant/StoryTenantIds.cs` | `DOMAIN_RULE` |
| 25 use cases (admin CRUD/status/schedule/review/items, seller draft/submit/items, storefront list) | `Application/Stories/{Commands,Queries}/**` | `APPLICATION_USE_CASE` |
| `StoryPresentationComposer` use-case façade + tenant guard | `Application/Stories/Presentation/StoryPresentationComposer.cs` | `PRESENTATION_COMPOSITION` |
| `StoryOperation` semantic→`Result` mapping | `Application/Stories/Composition/StoryOperation.cs` | `APPLICATION_USE_CASE` (support) |
| `StoryFailureMapper` transport enum parsing | `Application/Stories/StoryFailureMapper.cs` | `APPLICATION_USE_CASE` (support) |
| Directory/grid ports | `Application/Stories/Ports/*` | `APPLICATION_USE_CASE` (port) |
| 15 FluentValidation validators + codes | `Application/Stories/Validators/StoryValidators.cs` | `APPLICATION_USE_CASE` (transport shape) |
| Application-internal DTOs/snapshots/commands | `Application/Stories/Models/StoryModels.cs` | `APPLICATION_USE_CASE` (model) |
| Stable `story.*` error codes | `Contracts/Errors/StoryErrorCodes.cs` | `CONTRACT` |
| 25 HTTP routes | `Endpoints/{Admin,Seller,Storefront}/**` | `HTTP_ENDPOINT` |
| Admin/seller authorization seams | `Endpoints/Admin/IStoryAdminAuthorizer.cs`, `Endpoints/Seller/IStorySellerAuthorizer.cs` | `AUTHORIZATION_ADAPTER` (seam) |
| Error catalog contributor | `Endpoints/Errors/StoryErrorCatalogContributor.cs` | `HTTP_ENDPOINT` (presentation) |
| HTTP bodies + body→command mapping | `Endpoints/Models/StoryHttpModels.cs` | `HTTP_ENDPOINT` |
| Error resource set + resx | `Endpoints/Resources/*` | `HTTP_ENDPOINT` (localization) |
| `StoryDbContext`, migrations | `Infrastructure/Persistence/**` | `PERSISTENCE` |
| `StoryDirectory` (aggregate persistence) | `Infrastructure/Directory/StoryDirectory.cs` | `PERSISTENCE` |
| Admin grid adapter + query engine + policies | `Infrastructure/{Adapters,Grid}/**` | `PERSISTENCE` (read model) |
| Development seed | `Infrastructure/Development/StoryDevelopmentSeed.cs` | `DEVELOPMENT_SEED` |
| Module composition + outbox registration | `Infrastructure/StoryModule.cs` | `HOST_COMPOSITION_ROOT` (module-owned) |

**No `MUST_SPLIT` file.** `StoryModule.cs` is the one file that declares two top-level types
(`StoryModule` + `StoryOutboxRegistration`, 57 LOC); that is the exact shape of the certified
`Offer`/`Settlement`/`Payment`/`Promotion`/`Returns` modules, and W2 splits it into the canonical
`DependencyInjection/` + `Messaging/` (or `Outbox/`) pair as a *structural* alignment, not because it
is a cohesion violation.

---

## 4. Ownership map

Every responsibility maps to **Story**. Nothing in the module belongs to Host, Catalog, Media, Party,
Content or PageComposition. Verified foreign consumers of Story (all legal, all Contracts-only, and in
practice none exist):

| Consumer | Reference | Purpose |
| --- | --- | --- |
| `Host/Program.cs` | `Tooba.Story.Endpoints` (mapper + presentation + CQRS assembly + seller authorizer) | composition only |
| `Host/Composition/ToobaModuleComposition.cs` | `new StoryModule()` | composition only |
| `Host/Development/*` | `Tooba.Story.Infrastructure(.Development/.Persistence)` | development bootstrap only |
| `Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs` | `Tooba.Story.Infrastructure.Persistence` | migration registry |
| `Host/Security/Seller/HostStorySellerAuthorizer.cs` | `IStorySellerAuthorizer` | platform security adapter |

No module consumes `Tooba.Story.Contracts` today, which is consistent with `StoryErrorCodes` being
Story-owned boundary semantics.

---

## 5. Current illegal dependencies

**None.** Verified absence of:

- `Tooba.Story.* → foreign .Application / .Infrastructure / .Domain / .Endpoints`;
- foreign `DbContext` / `DbSet` reach-through;
- cross-module EF/SQL join (the only joins are `story.stories` ⋈ `story.story_items`, same schema);
- `TypeForwardedTo`;
- namespace alias hiding placement — **one** alias exists and is *intra-module* and *precedent-legal*:
  `StoryDirectory.cs:10` and `StoryDbContext.cs:8` use `using StoryEntity = Tooba.Story.Domain.Aggregates.Story;`
  to disambiguate the aggregate from the namespace segment `Tooba.Story.*`. It is not a folder-debt
  workaround and does not point at a foreign module. Recorded, not a violation.
- Story → Host dependency;
- Host-owned Story business policy.

Project edges (complete inventory):

| From | To | Kind |
| --- | --- | --- |
| `Tooba.Story.Domain` | `Tooba.BuildingBlocks`, `Tooba.Story.Contracts` | foundation + own module |
| `Tooba.Story.Application` | `Tooba.Story.Domain`, `Tooba.Story.Contracts`, `Tooba.BuildingBlocks` | own module + foundation |
| `Tooba.Story.Infrastructure` | `Tooba.Story.{Application,Contracts,Domain}`, `Tooba.ModuleContracts`, `Tooba.Persistence` | own module + foundation |
| `Tooba.Story.Endpoints` | `Tooba.Story.{Application,Contracts}`, `Tooba.BuildingBlocks` (+ `Microsoft.AspNetCore.App`) | own module + foundation |

Note: `Tooba.Story.Endpoints` deliberately has **no** `Tooba.Story.Domain` and **no**
`Tooba.Story.Infrastructure` edge — enforced today by
`StoryModuleAmcW6CertGuardTests.Story_endpoints_do_not_reference_Infrastructure_or_DbContext`. This is
the AMSC `Contracts-Boundary-State = CLEAN` and `Cross-Module-Coupling-State = NONE` proof, and it is
already durable.

---

## 6. Cross-module join inventory

`NONE`. `StoryDbContext` owns schema `story` with exactly two entity tables (`stories`, `story_items`)
plus the platform outbox table mapped through `OutboxMessageMapping.Map(modelBuilder, Schema)`. No
foreign schema, table, `DbSet`, navigation or raw SQL.

---

## 7. MUST_SPLIT decisions

`NONE` (see §3). The module's cohesion is already correct; W2 performs *structural alignment* moves
(composition entry, outbox registration, `Directory`→`Directories`, `StoryOperation` foldering), not
responsibility splits.

---

## 8. CQRS / MediatR and the validator matrix

**CQRS is compliant.** 25 endpoint-reachable requests, all real `IRequest<Result<T>>`, all with real
`IRequestHandler<,>`, all dispatched from Endpoints through `ISender`. Registered once through
`AddToobaCqrsFoundation(typeof(GetPublicStoriesQuery).Assembly)` in `Host/Program.cs:189`. No endpoint
touches `StoryDbContext` or `IStoryDirectory`.

**Validator coverage has a real gap.** The closed `W5` matrix claims
`15 VALIDATOR_REQUIRED + 10 NO_VALIDATOR_REQUIRED`. Re-deriving the matrix independently from shipped
routes and `ISender.Send` call sites gives the same 25 requests, but two `NO_VALIDATOR_REQUIRED`
classifications do not survive the AMSC provenance rule ("optional does not mean trusted"):

| Route | Request | Caller-controlled input | W5 claim | AMSC finding |
| --- | --- | --- | --- | --- |
| `GET /v1/storefront/stories?locale&market` | `GetPublicStoriesQuery` | `locale`, `market` query strings | `NO_VALIDATOR_REQUIRED` | **`VALIDATOR_REQUIRED`** — unbounded caller text that is compared with `StoryRules.MatchesLocale`/`MatchesMarket` and reaches the DB filter; sibling module Promotion validates exactly this shape (`PromotionValidationCodes.LocaleInvalid`) |
| `GET /v1/admin/stories?reviewStatus` | `ListAdminStoriesQuery` | `reviewStatus` query string | `VALIDATOR_REQUIRED` (present) | valid, but the validator duplicates `StoryFailureMapper.RequireReviewStatus`; one authoritative transport parse is preferable |
| `POST /v1/admin/stories/query?reviewStatus` | `QueryAdminStoryGridQuery` | `reviewStatus` query string | `VALIDATOR_REQUIRED` (present) | same duplication |

All other 23 rows are confirmed correct: `market`/`locale` are the only malformable caller inputs that
were left unclassified, and the remaining `NO_VALIDATOR_REQUIRED` rows (`Guid` route ids, actor ids
derived from the authorization seam, `Submit/Remove/Approve/Disable` with no body) carry no malformable
transport shape. Full matrix is carried forward to W1/W3.

---

## 9. Localization findings

`Localization-State = HARDCODED_TEXT`. One real defect, everything else canonical.

**Canonical (verified):**

- `Contracts/Errors/StoryErrorCodes.cs` declares 5 machine-stable codes (`story.missing`,
  `story.cta.rejected`, `story.mutation.rejected`, `story.tenant.missing`, `story.reviewStatus.invalid`).
- `Endpoints/Errors/StoryErrorCatalogContributor.cs` is the single `ErrorDescriptor` owner for those 5
  codes; `Endpoints/Resources/StoryErrorResourceSet.Owns` claims the `story.` prefix; both
  `StoryErrors.resx` and `StoryErrors.fa.resx` carry the 5 keys. `ErrorCatalogUniqueCodeGuardTests`
  keeps descriptor ownership unique.
- Zero `exception.Message` / `ex.Message` classification anywhere in the module
  (`StoryOperation` maps typed `SemanticException` only; `StoryFailureMapper` was reduced to a pure
  transport enum parser in AMC W1).
- Zero `Accept-Language` parsing; locale is a plain query/body value.
- Persian strings in production code exist **only** in `Infrastructure/Development/StoryDevelopmentSeed.cs`
  (seed display values `"موبایل"`, `"بازی"`, `"پیش‌نویس فروشنده"`, `"در انتظار بازبینی"`). Seed data is
  **not** user-facing API text and is not a localization violation.

**Defect (blocker B1):** `Application/Stories/Validators/StoryValidators.cs` declares 8 stable
validation machine codes:

```text
story.title.required            story.ids.required        story.itemIds.required
story.mediaType.required        story.grid.request.required
story.reviewStatus.invalid      story.rejectionReason.required
story.schedule.range.invalid
```

Every one of them travels inside the canonical foundation `validation.failed` envelope
(`SafeErrorMapper.MapValidation` → `MappedSafeError.ValidationErrors`), so the API contract is stable
and *no* endpoint response changes. But only **one** of the 8 (`story.reviewStatus.invalid`) exists in
the `.resx` pair. The other 7 have no localization resource, so a client that renders
`validationErrors` keys shows raw machine codes. The certified sibling **Promotion** resolves exactly
this by shipping its own validation keys in `PromotionErrors.resx` / `.fa.resx`
(`promotion.definition.id_required`, `promotion.translation.locale_required`, …), and Story's own
`story.reviewStatus.invalid` already follows that pattern. W1 closes the gap additively.

---

## 10. API result / error mapping findings

`CANONICAL` by construction:

- every endpoint injects `ApiResponseFactory` and returns `api.From(result)` or
  `api.Created(location, result)`; success shape is the shipped `ApiResponse` envelope;
- zero `Results.Json(...)`, `Results.BadRequest(...)`, `Results.Problem(...)`;
- zero local `ProblemDetails` builder, zero local error mapper;
- zero `catch`-and-map in endpoints; the only `catch` is `StoryHttpErrors.ResolveTenantId`, which
  converts a typed `SemanticException` into `Result.Failure` — that is the canonical typed-failure
  path, not message parsing;
- unknown exceptions propagate to the Host global boundary (`IExceptionPresentationService`).

`ErrorDefinitionCatalog` fail-fast duplicate detection is untouched; no suppression, overwrite,
`DistinctBy` or first/last-wins behaviour exists in the module.

---

## 11. Logging / sensitive-data findings

- `Logging-State = CANONICAL`: the module contains zero `ILogger`, zero `Console.WriteLine`, zero
  `Debug.WriteLine`, zero custom logger, zero second telemetry pipeline. Story is a thin
  directory/aggregate module whose observability is supplied by the canonical CQRS
  `LoggingBehavior`/`TracingBehavior` pipeline.
- `Sensitive-Logging-State = NONE`: no token, secret, `Authorization` header, cookie, credential or
  payment payload is logged or forwarded to telemetry anywhere in the module.

---

## 12. OpenTelemetry / correlation findings

- `OpenTelemetry-State = CANONICAL`; `Correlation-Trace-State = CANONICAL`.
- Zero direct `ActivitySource.StartActivity`, zero second `ActivitySource`/`Meter`, zero manual
  `traceparent` parsing, zero competing correlation header, zero `AsyncLocal` correlation, zero
  `Guid.NewGuid()`-as-correlation.
- Story issues no synchronous cross-module call, so no `IModuleCallTracer` decoration is required on
  this surface.

---

## 13. File size / cohesion audit

| File | LOC | Verdict | Action |
| --- | --- | --- | --- |
| `Infrastructure/Directory/StoryDirectory.cs` | 672 | `OVERSIZED_ONLY` / `WATCH` (single responsibility: aggregate persistence + mapping) | none (no `tmar-source-size-baseline.json` entry; below the 800 ceiling) |
| `Domain/Aggregates/Story.cs` | 418 | `COHESIVE` (aggregate root + its invariants) | none |
| `Infrastructure/Grid/AdminStoryGridQueryEngine.cs` | 218 | `COHESIVE` | none |
| `Endpoints/Admin/StoryAdminEndpoints.cs` | 182 | `COHESIVE` (one audience, transport only) | none |
| everything else | ≤ 163 | `COHESIVE` | none |

No `CRITICAL_GOD_FILE`, no `MULTI_RESPONSIBILITY_COHESION_VIOLATION`, no cosmetic split required. No
baseline change is needed in any wave.

---

## 14. Exact target paths / namespaces

Capability-first and shallow today:

```text
Tooba.Story.Contracts/
    Errors/StoryErrorCodes.cs                        namespace Tooba.Story.Contracts.Errors
Tooba.Story.Domain/
    Aggregates/{Story,StoryItem}.cs                  namespace Tooba.Story.Domain.Aggregates
    Enums/{StoryOrigin,StoryReviewStatus,StoryStatus}.cs
    Rules/StoryRules.cs
    Tenant/StoryTenantIds.cs
Tooba.Story.Application/
    Stories/Commands/{Admin,Seller}/*.cs             namespace Tooba.Story.Application.Stories.Commands.*
    Stories/Queries/{Admin,Seller,Storefront}/*.cs
    Stories/Models/StoryModels.cs
    Stories/Ports/*.cs
    Stories/Presentation/StoryPresentationComposer.cs
    Stories/Validators/StoryValidators.cs
    Stories/Composition/StoryOperation.cs
    Stories/StoryFailureMapper.cs
Tooba.Story.Endpoints/
    StoryEndpointModule.cs                           (root allowlist)
    Admin/{IStoryAdminAuthorizer,StoryAdminEndpoints}.cs
    Seller/{IStorySellerAuthorizer,StorySellerEndpoints}.cs
    Storefront/StoryStorefrontEndpoints.cs
    Errors/{StoryErrorCatalogContributor,StoryHttpErrors}.cs
    Models/StoryHttpModels.cs
    Resources/{StoryErrorResources.cs,StoryErrors.resx,StoryErrors.fa.resx}
Tooba.Story.Infrastructure/
    StoryModule.cs                                   (root allowlist, today)
    Adapters/AdminStoryGridAdapter.cs
    Development/StoryDevelopmentSeed.cs
    Directory/StoryDirectory.cs
    Grid/{AdminStoryGridQueryEngine,StoryAdminGridPolicies}.cs
    Persistence/StoryDbContext.cs
    Persistence/Migrations/*                         (5 files)
```

**W2 structural decisions handed off** (Analyze does not decide them; it records the options and the
evidence):

1. **Composition entry placement.** Repository standard *permits* the module composition entry at the
   Infrastructure root; the newest certified majority (`Settlement`, `Offer`, `Payment`, `Promotion`,
   `Returns`, `Notification`, `Fulfillment`, `Inventory`, `Pricing`, `CustomerProfile`, `AddressBook`,
   `AccessControl`) places `*Module.cs` under `Infrastructure/DependencyInjection/`. Story is currently
   in the root minority together with `Identity`, `Media`, `Content`, `Catalog`, `Order`, `Party`,
   `Localization`, `PageComposition`, `ProductQnA`, `BulkInquiry`, `OperatorProfile`, `ProductWorkspace`.
   W2 must decide and, if it moves the file, update the allowlist, the manifest and the guards
   atomically.
2. **Outbox registration placement.** `StoryOutboxRegistration` shares `StoryModule.cs`. The canonical
   precedent is a separate file under `Infrastructure/{Outbox,Messaging}/`
   (`OfferOutboxRegistration`, `SettlementOutboxRegistration`, `PaymentOutboxRegistration`, …). W2 owns
   the decision and the blast radius (the type is referenced only from inside `StoryModule.AddServices`).
3. **`Directory/` → `Directories/`.** 20 certified modules use `Infrastructure/Directories/`; Story is
   the **only** module using the singular `Directory/`. Renaming the folder is cosmetic on disk but
   requires the namespace, the manifest and several Host guards (`HostGridAmcR2GuardTests`,
   `HostGridAmcR5GuardTests`, `HostGridAmcR5R1GuardTests`, `AdminDbNativeGridQueryTests`) to move
   together. W2 owns the decision.
4. **`Stories/Composition/StoryOperation.cs` and `Stories/StoryFailureMapper.cs`.** The certified
   majority places the module operation mapper at `Application/Composition/<Module>Operation.cs`
   (`SettlementOperation`, `PaymentOperation`, `PromotionOperation`, `ReturnsOperation`,
   `NotificationOperation`, `InventoryOperation`, …). `StoryFailureMapper` sits directly under
   `Stories/` (no technical axis), which no other certified module does. W2 owns both decisions.
5. **`Stories/` as a single capability.** Story has exactly one capability, so the `Stories/` wrapper is
   defensible (cf. `Settlement/Payouts`, `Returns/ReturnRequests`, `Notification/Customer|Seller`).
   W2 must state explicitly whether it keeps `Stories/` or flattens to the module root; this analysis
   records that the module is single-capability and that `Stories/` is not over-foldering.

---

## 15. Behavior-preservation checklist

Must remain exactly equivalent across W1–W3:

- **Routes (25).** `/v1/admin/stories` (GET, POST, PUT `/{id}`, PUT `/reorder`), `/v1/admin/stories/query`
  (POST), `/v1/admin/stories/{id}` (GET), `/v1/admin/stories/{id}/{enable|disable|schedule|approve|reject}`
  (POST), `/v1/admin/stories/{id}/items` (POST), `/v1/admin/stories/{id}/items/{itemId}` (PUT, DELETE),
  `/v1/admin/stories/{id}/items/reorder` (PUT); `/v1/seller/stories` (GET, POST), `/v1/seller/stories/{id}`
  (GET, PUT), `/v1/seller/stories/{id}/submit` (POST), `/v1/seller/stories/{id}/items` (POST),
  `/v1/seller/stories/{id}/items/{itemId}` (PUT, DELETE), `/v1/seller/stories/{id}/items/reorder` (PUT);
  `/v1/storefront/stories` (GET).
- **HTTP methods, status codes, response envelope** (`ApiResponse`), `Created` location headers
  (`/v1/admin/stories/{storyId}`, `/v1/seller/stories/{storyId}`).
- **Stable error codes** — the 5 `story.*` codes and their HTTP classifications/statuses.
- **Validation machine codes** — the 8 `story.*` validation codes and the foundation
  `validation.failed` envelope; W1 adds **resources for existing keys only**, never new codes, never
  renamed keys.
- **Domain rules** — `StoryRules` CTA scheme rejection (`javascript:`/`data:`/`vbscript:`), allowed CTA
  types, allowed media types, max lengths, locale/market matching, review state machine
  (Draft→Submitted→Approved/Rejected), `IsPublicationEligible`, `IsPubliclyVisible`, `VersionToken`
  optimistic concurrency.
- **Authorization semantics** — admin via `IAdminPanelAccess`, seller via `ISellerPanelAccess`; actor
  and `sellerPartyId` derived by the seam, never from the request body.
- **Tenant scoping** — `StoryPresentationComposer.RequireTenantId` → `StoryTenantIds.FromTenantKey`
  (deterministic SHA-256 mapping, `store-alpha`/`store-beta` pins) and the fail-closed
  `story.tenant.missing` path.
- **Persistence/schema** — schema `story`, tables `stories`/`story_items`, columns, indexes,
  `HasConversion<int>` enums, `VersionToken` concurrency token, outbox table, both migration IDs and
  their Up/Down.
- **DI** — `StoryModule.Name == "Story"`, its position in `ToobaModuleComposition.Modules`, scoped
  lifetimes (`IStoryDirectory`, `IAdminStoryGridPort`, `StoryPresentationComposer`), outbox
  registration, `AddModuleSchemaMigrator<StoryDbContext>("Story", ModuleSchemaMigrationOrder.Story)`.
- **Development seed values** (including the four Persian display strings) — unchanged.
- **Telemetry/correlation** — unchanged (none emitted by the module).
- **Localization keys and semantics** — preserved; W1 only adds missing resources for already-declared
  codes.

---

## 16. W1 migration order

Story has **no ownership, coupling, cohesion, CQRS, API-result, logging, telemetry or schema debt**.
W1 therefore performs two bounded, behavior-preserving repairs:

1. **Persian documentation compliance** (same class of repair AMSC performed for StoreContext).
   `docs/architecture/32-persian-code-documentation-standard.md` (Architect-accepted, `COMPLETE`)
   requires strong professional Persian XML on Tooba-owned public members. 18 Story production files
   currently carry **English** XML summaries — including `Contracts/Errors/StoryErrorCodes.cs` (6),
   `Infrastructure/StoryModule.cs` (8), `Infrastructure/Directory/StoryDirectory.cs` (25),
   `Application/Stories/Composition/StoryOperation.cs` (6), `Application/Stories/StoryFailureMapper.cs` (3),
   `Endpoints/Errors/StoryHttpErrors.cs`, `Endpoints/Errors/StoryErrorCatalogContributor.cs`,
   `Endpoints/Resources/StoryErrorResources.cs`, `Endpoints/Seller/IStorySellerAuthorizer.cs`,
   `Endpoints/Admin/IStoryAdminAuthorizer.cs`, `Application/Stories/Validators/StoryValidators.cs`,
   `Application/Stories/Ports/IAdminStoryGridPort.cs`, the three query files,
   `Infrastructure/Adapters/AdminStoryGridAdapter.cs`, `Infrastructure/Grid/StoryAdminGridPolicies.cs`,
   `Infrastructure/Persistence/StoryDbContext.cs`. W1 rewrites them in professional Persian, keeps
   technical identifiers in English, preserves every invariant sentence's meaning, and changes **no**
   code, signature, registration, namespace or behaviour.
2. **Validation-code localization repair (blocker B1).** W1 adds the 7 missing
   `story.*` validation keys to `Endpoints/Resources/StoryErrors.resx` **and** `StoryErrors.fa.resx`,
   following the Promotion precedent. This is additive: no code change, no key rename, no new
   machine code, no HTTP contract change. `StoryErrorResourceSet.Owns("story.")` already claims the
   prefix, so the resources are reachable with zero wiring change.

Nothing else is authorized in W1. Any ownership/coupling "fix" would be a fabricated defect.

---

## 17. Verification plan

| Wave | Focused validation |
| --- | --- |
| W0 | none (analysis only); `git status` free of production modifications |
| W1 | `dotnet build src/backend/Tooba.slnx` (0 errors; CS1591 stays enforced); `StoryModuleAmcW1GuardTests`; `StoryModuleAmcW3StructureGuardTests`; `StoryModuleAmcW4ResultGuardTests`; `StoryModuleAmcW5ValidatorGuardTests`; `StoryModuleAmcW6CertGuardTests`; `ErrorCatalogUniqueCodeGuardTests`; `StoryFoundationTests` |
| W2 | new `StoryModuleAmsc001W2StructureGuardTests`; `TmarCompleteReferenceStructureGateTests`; the four Host grid/migration guards that pin Story paths; `.slnx` `/Modules/Story/` grouping check |
| W3 | new `StoryModuleAmsc001W3CertGuardTests`; all W1/W2 guards; `ErrorCatalogUniqueCodeGuardTests`; `HostStoryAmcGuardTests` |

Bounded rule: one deterministic repair per failing focused check, then stop and report.

---

## 18. Certification blockers

Blockers that must be closed before Story can claim a current `TB-TMAR-STORY-AMSC-001` certification:

1. **B1 — validation-code localization gap.** 7 of 8 declared `story.*` validation codes have no
   `.resx` resource while the certified Promotion precedent resolves exactly this. → **W1**
   (additive resources).
2. **B2 — validator matrix not exhaustive under the AMSC provenance rule.**
   `GetPublicStoriesQuery` (`locale`, `market`) is classified `NO_VALIDATOR_REQUIRED` although both are
   unbounded caller-controlled strings that reach a DB filter. → **W1** (add the canonical
   transport-shape validator and re-derive the 25-row matrix).
3. **B3 — documentation-standard gap.** 18 production files carry English XML against the
   Architect-accepted Persian documentation standard. → **W1**.
4. **B4 — structural divergence from the certified majority.**
   `Infrastructure/StoryModule.cs` at the root (instead of `DependencyInjection/`), the outbox
   registration sharing the composition file (instead of `Messaging/`/`Outbox/`),
   `Infrastructure/Directory/` singular (instead of `Directories/`),
   `Stories/Composition/StoryOperation.cs` (instead of `Application/Composition/`), and
   `Stories/StoryFailureMapper.cs` directly under the capability folder. → **W2**.
5. **B5 — manifest/structure justifications and AMSC lineage.**
   The Story manifest entry has `rootAllowlistJustification` prose but **no** `certificationNote`
   recording the AMSC lineage, and there is no `storyAmsc001W0..W3` SoT record, no AMSC evidence tree
   and no Master Recovery AMSC checkpoint for Story. → **W0→W3**.
6. **B6 — no Story-scoped AMSC durable guard.** Current protections are the AMC-era guards
   (`StoryModuleAmcW1/W3/W4/W5/W6`) plus shared Host guards. → **W2/W3** add module-scoped AMSC guards.

Non-blocking observations (recorded, no action required):

- `HostDevelopmentMigrationSeamGuardTests:39` pins `"Story/Tooba.Story.Infrastructure/StoryModule.cs"`
  as one of 28 module composition roots. If W2 moves the composition entry, this guard must be updated
  in the same change.
- `HostGridAmcR2GuardTests:26,34`, `HostGridAmcR5GuardTests:66`, `HostGridAmcR5R1GuardTests:72` and
  `AdminDbNativeGridQueryTests:140` pin the exact Story Infrastructure paths
  (`Grid/AdminStoryGridQueryEngine.cs`, `Grid/StoryAdminGridPolicies.cs`, `StoryModule.cs`).
- `StoryModuleAmcW3StructureGuardTests:53` and `StoryModuleAmcW6CertGuardTests:57` assert the
  Infrastructure root file set is exactly `["StoryModule.cs"]`; the manifest mirrors this in
  `Tooba.Story.Infrastructure.rootAllowlist`. Any W2 move must update both guards, the manifest and
  `HostDevelopmentMigrationSeamGuardTests` atomically.
- `TmarSourceSizeAndInfraAppTests` fails in this environment for a **pre-existing, unrelated** reason
  (the stale git-ignored `.tmp-baseline` working clone); it is not Story debt and no wave touches it.
- The historical AMC evidence under `docs/evidence/TB-TMAR-STORY-AMC-001-*` is preserved unrewritten;
  the AMSC lineage is recorded **additively** under `docs/architecture/evidence/`.

---

## 19. Disposition

`READY_TO_MIGRATE`

`Structure-Handoff-State = REQUIRED` (W1 documents + localizes; W2 owns the composition/outbox/folder
alignment decisions in §14 and the final structural verdict; W3 certifies and locks the module under
`ARCH-COMPLETE-002`).
