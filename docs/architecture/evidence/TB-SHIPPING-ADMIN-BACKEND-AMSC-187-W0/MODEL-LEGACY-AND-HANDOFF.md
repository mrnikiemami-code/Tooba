# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0 — Target model, legacy transition and implementation handoff

Inspected commit: `68d1aeb3525e8b2d43c719578b433874d26e09d4`.
Product authority: `docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md` §1–§15 (owner decisions dated 2026-10-21/22), `SHIPPING-STOREFRONT-UX-REFERENCE.md`, `docs/architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md`, `docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md` (dated ledger).
UX evidence (not implementation): `src/frontend/app/admin/shipping-services/design-preview/**`.

## 1. Field-by-field mapping (approved editor → current persisted reality)

`KEEP` = already persisted by the right owner; `EXTEND` = same owner, additive column/contract; `GAP` = nothing persisted anywhere; `CONFLICT` = existing persisted concept contradicts the approved target.

| Approved editor field | Current persisted owner / type | Decision |
|---|---|---|
| Method identity / code | Fulfillment `ShippingMethod.MethodCode`, `ShippingService.Code` + `ShippingServiceOption.Code` | `KEEP` |
| Method name / description, localized | Fulfillment `ShippingServiceTranslation`, `ShippingServiceOptionTranslation`, `ShippingOneLevelMethodTranslationSnapshot` (per `LanguageId`) | `KEEP` |
| Method activation | Fulfillment `ShippingMethod.IsActive` + one-level activation route | `KEEP` |
| **Shipping type above method name** (ordinary / heavy / extra-heavy / grocery) | **nothing** — no `ShippingType`/taxonomy entity in Shipping or Fulfillment | `GAP` |
| **Rule owner** (marketplace vs seller, owner-scoped panel) | **nothing** — no owner column on any Shipping table and none on the Fulfillment shipping catalog | `GAP` |
| Prepaid vs collect on delivery | Shipping `ShippingPackagingPolicy.FreightPayerMode` (`Prepaid|CollectOnDelivery`) — modelled on the **packaging** policy, not the method | `CONFLICT` (placement) → `EXTEND` method-level |
| Calculation mode manual / carrier / free | Shipping `ShippingMethodFinancialMode` proves **only** `FixedAmount`; free / rule-based / provider-quote have no column, rule or engine | `GAP` |
| Region group (one geography selection) | Shipping `ShippingDestinationScope` (Country/AdminArea/City/ServiceArea) per rate/coverage/packaging row — **per row**, no group entity | `GAP` (group) / `KEEP` (scope value) |
| Multiple alternative AND-condition rows | Shipping `ShippingRatePolicy` with `Min/MaxWeightGrams`, `Min/MaxItemCount` (half-open) | `KEEP`, `EXTEND` with merchandise-amount tier |
| Group merchandise amount condition | **nothing** (`CartSubtotalExclusive` exists only as an **input** to the operational quote evaluator, and only as a free-above-threshold comparison) | `GAP` |
| Freight amount + currency | Shipping `ChargeAmount` + `CurrencyCode` (3-letter, upper-cased, CHECKed) | `KEEP` |
| Packaging amount | Shipping `ShippingPackagingPolicy.FeeAmount` + `CurrencyCode` (separate aggregate) | `KEEP` |
| Delivery duration | Shipping `PreparationDays` + `LeadDays` (deliberately separate) | `KEEP`; the preview's single `minDays/maxDays` must be mapped onto them, not merged |
| Weekly working/closed + start/end per weekday | Shipping `ShippingOperatingSchedule.Windows` (`Weekday` 1..7, half-open `TimeOnly`, optional `Capacity`) | `EXTEND` — see §4 |
| Method-level timezone | Shipping `ShippingOperatingSchedule.TimeZoneId` (validated via `TimeZoneInfo.FindSystemTimeZoneById`) | `KEEP` |
| Order cutoff (hours before the day's window) | **nothing** — no cutoff column on schedule or method | `GAP` |
| Content languages used by the editor | Localization language registry via `/v1/admin/languages` + `ILanguageLookup` | `KEEP` — see §3 |
| Store currency | `StoreCommerceContext.DefaultCurrency` (store default only) and per-row `CurrencyCode` | `KEEP` as defaults; no new currency engine |

**Consequence:** of the approved editor, only identity, translations, single-amount freight, packaging amount, preparation/lead days, destination scope values and per-weekday windows are persisted today. Owner scope, shipping-type taxonomy, region-group container, merchandise-amount tier, explicit calculation modes, cutoff and marketplace seller scoping are all unmodelled.

## 2. Edition / store / owner provenance (source-proven)

### 2.1 What is proven

- `ToobaEdition` = `Unset | Marketplace | SingleStore` is a **process-level** platform value from configuration, never from the request (`src/backend/BuildingBlocks/Tooba.BuildingBlocks/CommerceContext.cs:8-23`).
- The Host resolves it once per request in `TenantResolutionMiddleware`:
  - `Marketplace` → `Tenant: null`, one `MarketplaceConnectionReference`, `_registry.DeploymentStoreCommerce`.
  - `SingleStore` → `Host -> TenantRecord` allowlist lookup; `Disabled`/`Suspended`/unknown → identical fail-closed 404 (`TenantResolutionMiddleware.cs:99-156`).
  - `Unset` → `platform.edition.unconfigured`.
- The assigned store commerce context is `StoreCommerceContext(Market, DefaultCurrency, SalesChannel)` — **three strings and no owner** (`Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs:18-21`). The contract's own XML doc says consumers must fail closed when it is `null`.
- Merchant/seller identity is **not** available from `StoreCommerceContext`, `ICurrentCommerceContext` or `ICurrentTenant`. The only canonical seller-scope seam is `IPlatformAccessSeams.ISellerPanelAccess.RequireAuthorizedAsync(request) -> (ActorUserId, SellerPartyId)` (`src/backend/BuildingBlocks/Tooba.BuildingBlocks/Security/IPlatformAccessSeams.cs:16-27`), whose implementation derives edition from `ICurrentEdition.Current.Edition` and fails closed when unresolved (`Host/Tooba.Host/Security/Seller/SellerPanelAccess.cs:91-107`).
- The Shipping admin gate does **not** use that seam. `ShippingAdminAuthorizer` uses `IAdminPanelAccess` (admin panel only), returns only `ActorUserId`, and hardcodes `Edition = ToobaEdition.SingleStore` plus `TenantId = tenant.Current?.TenantId.Value ?? "unknown"` in its `AuthorizationCallContext` (`Tooba.Shipping.Endpoints/Admin/ShippingAdminAuthorizer.cs:35-58`).
  - The hardcoded edition replicates an existing admin-authorizer pattern (same shape in `HostOrderAdminAuthorizer`, `HostSupportAdminAuthorizer`, `HostWalletAdminAuthorizer`, `AdminPanelAccess`), while `HostAdminPanelAccess` uses `Marketplace` and `SellerPanelAccess` uses the resolved edition. Reported as a **pre-existing platform pattern**, not as a Shipping invention.
- No endpoint, handler, query, projection or database filter anywhere in Shipping scopes work by seller/merchant. `(service_code, option_code[, scope])` is the entire identity at every layer.

### 2.2 Resulting single-store / marketplace matrix (current reality)

| Capability | Single-store (today) | Marketplace (today) | Required by owner |
|---|---|---|---|
| Admin rate/coverage/packaging/schedule authoring | Allowed (`fulfillment.manage` via admin panel) | Allowed, **but** the authorizer asserts `Edition=SingleStore` and `TenantId="unknown"` | Mode must come from authoritative edition/context |
| Visible owner selector | Absent | **Absent** | Single-store: absent + server rejects marketplace-only operations. Marketplace: present for authorized sellers |
| Seller-authored shipping rules | Not possible (no route, no owner column) | Not possible (same) | Marketplace preserves independently authorized seller scopes |
| Forged owner / foreign-owner rejection | Trivially "safe" because no owner input exists | Same | Must reject forged/missing/foreign owner explicitly |
| Cross-store visibility | Prevented by per-tenant database connection (SingleStore) | Marketplace = one shared database → **no per-seller isolation exists** | Must be enforced by owner scope, not only by connection routing |
| Marketplace-only operations rejected in single-store | n/a (no such operation exists) | n/a | Explicitly required |

**Single-Store-Marketplace-Authority-State = `UNPROVEN_GAP`.** The current model is "store-global policy, admin-only, no owner". That accidentally satisfies *hiding* the owner selector in a single store but fails the server-side requirement for marketplace seller scoping, and it satisfies "no forged owner" only because owner input does not exist. `StoreCommerceContext` cannot supply the "actual store's canonical merchant ownership"; that mapping is `UNKNOWN` in the repository and must be an Architect decision (§7 D1).

### 2.3 Cache-scope observation

No Shipping cache exists (`ShippingModule.AddServices` registers no `IMemoryCache`/`IDistributedCache` consumer and no key factory). The only cross-request state is EF tracking per scope. Therefore no cache key requires edition/tenant/store/locale scope **today**; if a later slice introduces one, the observed contract requires at least edition + tenant/store + language. No new cache is proposed in W0.

## 3. Database-backed language authority (proven behaviour)

| Question | Proven answer | Evidence |
|---|---|---|
| What does `ILanguageLookup.ListAsync` return? | **All** rows in the Localization database, ordered by `SortOrder` then `Code`. **No** `IsActive` filter, no store filter, no per-store enablement. | `LanguageDirectory.ListAsync` (`_db.Languages.AsNoTracking().OrderBy(SortOrder).ThenBy(Code)`), `LanguageLookupBridge.ListAsync` |
| Does it expose active/default/direction? | `LanguageLookupSnapshot(LanguageId, Code, Culture, UrlPrefix, IsDefault)` — **no** `IsActive`, **no** `Direction`, **no** `DisplayName`, **no** `SortOrder` | `Localization.Contracts/Ports/LanguageLookupContracts.cs:5-11` |
| So is "known languages" == "active/store-enabled"? | **No.** The set includes inactive languages. The seam therefore cannot back an "enabled content languages" claim without an extension. | same |
| Is there a per-store language enablement table? | **No.** Language rows are global to the Localization database. In SingleStore that database is the store's database (so "store languages" ≈ that DB's rows); in Marketplace the same table is marketplace-global, so a seller cannot have its own language set. | `LocalizationDbContext` + `Language` aggregate |
| Is "exactly one active default" enforced? | Yes, on the write path: at least one active and exactly one active default after save (`LanguageDirectory.SaveWithInvariantChecksAsync`). | `LanguageDirectory.cs:247-262` |
| What does the live Admin read today? | `GET /v1/admin/languages` (admin-authorised, returns full admin rows incl. `active`, `direction`, `sortOrder`, `calendarDisplay`), mapped by `mapSupportedLocale`; the shipping method language editor consumes it and keys drafts/saves by real `languageId`. | `LocaleAdminEndpoints.cs:15-22`, `language-api.ts:18`, `shipping-method-language-model.ts:8-16` |
| Is there any Shipping-owned language table or fa/en hardcode in the backend? | **No.** No Shipping language storage; `ShippingServiceTranslation.LanguageId` is a Fulfillment FK to the Localization language id (by identifier, no cross-schema FK). | Shipping/Fulfillment persistence |

**Lawful read seam for the target (recommended, minimal):** reuse `GET /v1/admin/languages` for the *admin authoring* UI (already proven, already authorised, already carries every field the editor needs) and extend the **Contracts-level** `LanguageLookupSnapshot` only if a non-HTTP consumer genuinely needs it. Required additions for a literal "one/two/many configured content languages" semantics are `IsActive` (and, if the consumer renders labels, `Direction`); both already exist on the domain aggregate, so this is an additive Contracts change with a single owner (Localization), not a new resolver.

**Explicitly rejected by this analysis:** a Shipping language table, a duplicate language resolver, a fixed `fa/en` list, reading `LocalizationDbContext` from Shipping, or inferring direction/culture from a country or browser header. The preview's `PREVIEW_CONTENT_LANGUAGE_IDS = ["fa","en"]` is fixture data and must not become an API.

**`Database-Language-Authority-State = `EXISTING_SEAM_PROVEN / ENABLEMENT_SEMANTICS_GAP`.**

## 4. Legacy transition (additive / change / keep), with exact conflicts

### 4.1 Conflict register (each item is source-proven)

| # | Existing behaviour | Approved target | Classification |
|---|---|---|---|
| C1 | Coverage mode `Unrestricted` = "no admin-defined restriction" (absence of a `method_coverages` row is also unrestricted) | "one non-overlapping multi-region selection per region group; unmatched input unavailable" | `CHANGE` — absence must stop meaning "unrestricted" for a method that has a region group; `Unrestricted` remains the explicit mode and the safe legacy default |
| C2 | Geography is stored **per policy row** (`rate_policies`, `packaging_policies`, `coverage_grants` each carry their own scope) | Geography chosen **once** per region group and reused by packaging/schedule | `CHANGE` — needs a region-group container; no duplicated geography |
| C3 | `FreeAboveThreshold` is a per-row free-shipping threshold | "Free base method vs Promotion offer remain separate" | `KEEP` as a legacy row attribute; do **not** promote it to a free mode. A zero/absent amount must not become a "free" state |
| C4 | `PreparationDays` + `LeadDays` per rate row | Delivery duration shown per row; preparation/dispatch/customer window remain separate | `KEEP` both fields; the editor must not collapse them into one number |
| C5 | Schedules are per `(service_code, option_code)` with **many** windows per weekday, capacity, timezone; no closed-day flag; no cutoff | Independent per-weekday working/closed + hours; shared across method regions; cutoff mapped, not guessed | `EXTEND` — add explicit closed-day semantics and cutoff **additively**; never delete multi-window rows |
| C6 | `ShippingMethodFinancialMode` proves only `FixedAmount`; free/COD/rule-based/provider-quote are explicitly *not* provable | Prepaid vs collect independent of manual/carrier/free | `EXTEND` — a new explicit calculation-mode contract, gated so `FixedAmount` keeps today's behaviour |
| C7 | `FreightPayerMode` lives on the packaging policy | Payer timing is a **method** fact | `CHANGE` (placement) — move/duplicate to method level with a documented precedence; do not silently change existing row meaning |
| C8 | Rate/coverage uniqueness is `(service, option, exact scope, tier)` with `NULLS NOT DISTINCT` | One region group with multiple alternative rows | `EXTEND` — keep the existing unique index as-is; add the group key, do not weaken the existing invariant |
| C9 | `ShippingAdminAuthorizer` hardcodes `Edition = SingleStore` and `TenantId = "unknown"` in the authorization call context | Mode is authoritative edition/context | `CHANGE` — derive from `ICurrentEdition` (fail-closed), matching `SellerPanelAccess`'s existing pattern |
| C10 | No owner column anywhere on Shipping tables or the Fulfillment shipping catalog | Owner-scoped rules for marketplace; single-store canonical merchant owner | `ADDITIVE` with a **data-migration decision required** (who owns existing rows?) — see D1 |
| C11 | No shipping-type taxonomy | "Shipping type above method name" | `ADDITIVE` (new taxonomy owned by Fulfillment, referenced by Shipping policy) — see D2, and see the reconciliation note in §6 |
| C12 | `RatePolicies` / `PackagingPolicies` / `Schedules` never physically delete | Historical snapshots in Order must stay readable | `KEEP` — no destructive collapse of distinct legacy rates/schedules/owners |

### 4.2 Explicit non-decisions (do not guess)

- Legacy rows whose owner is ambiguous must **not** be auto-assigned to the platform owner or to any seller. Keep them readable; mark them editable only after an explicit Architect decision.
- `Tooba:ShippingOwnedRateAuthority:Enabled` and `Tooba:ShippingOperationalShadow` defaults stay **OFF**; the one-level catalog gate stays **OFF**. No flag flips in this program without a separate authorised task.
- Packaging payer / customer charge stays out of the customer payable (TB-177 BLOCKED business decision); financial allocation stays `PendingAllocation`.
- Old multi-window schedules stay supported. "The preview edits one interval per day" is a UI choice, not a migration licence.

## 5. Smallest coherent save/read operation (design only)

**Read (no schema change):** one query per tab, already implemented — `GetShippingRatesAndCoverageQuery`, `GetShippingOperatingSchedulesQuery`, `GetShippingPackagingPoliciesQuery`, `GetShippingMethodFinancialStatusQuery`. A grouped editor read can be composed in the module's **Application** layer from these four existing ports; no new HTTP route per tab is required *if* the client is allowed four reads (it already performs them today).

**Write (the real gap):** a single "save method authoring" operation that covers *method metadata + region groups + rows* must be **one Application command with one transaction inside the Shipping module** (`ShippingDbContext.SaveChanges` under a single `ExecuteUpdate`/unit of work), because all of that data is Shipping-owned. It must **not**:
- span Fulfillment identity writes (method code/translations/activation stay on their existing Fulfillment routes, invoked separately by the client);
- use a shared `DbContext`, a shared transaction, a cross-module FK or a cross-schema join;
- be presented to the client as "N browser writes are atomic".

Failure/retry/idempotency:
- optimistic concurrency via a row `Version`/`UpdatedAt` precondition (additive), returning a stable `shipping.*.concurrency` code and 409-class semantics through `ApiResponseFactory`;
- the operation is idempotent for a repeated identical payload (upsert by natural key, not append);
- partial failure must not leave a half-written region group: one `SaveChanges` per command.

If the owner later requires method-identity + policy in one user action, that is an **orchestration** concern: the client performs two authorised calls, or Shipping exposes a command that calls Fulfillment **Contracts** and reports a composite result. Shipping must not open a Fulfillment transaction.

## 6. Reconciliation of superseded statements (documentation truth)

- `SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md` states "NO 'Types of Service' tab: the former child types become individual manageable methods". The owner's 2026-10-22 "shipping type above method name" introduces a **shipping-type taxonomy** (e.g. ordinary/heavy/extra-heavy/grocery) that is **not** the removed provider child-service-type tree. These are different concepts and the older sentence must not be read as forbidding the new field. Exact taxonomy values, whether they are store-configurable, and whether they are translated are `UNKNOWN` → decision D2.
- Recovery-ledger and older planning statements that the Admin flat method / per-method schedule / packaging policy are "NOT IMPLEMENTED" are stale; the sources in `SOURCE-MAP.md` show them implemented and routed. This document uses the dated ledger plus the inspected commit.
- The `ShippingModule` / `ShippingDbContext` XML remarks still say "not wired to Host / nothing writes / no endpoint". That is **stale documentation drift**: `Program.cs:479` maps the routes and `ToobaModuleComposition.cs:98` composes the module. Reported as a finding; not repaired in W0 (documentation-only task).
- `ShippingDbContext` "NOT WIRED" remark is likewise stale (migrations are applied only by the explicit `Tooba.MigrationRunner` CLI, which is the real, still-true constraint).

## 7. Unresolved Architect decisions (blocking implementation)

- **D1 — Canonical merchant/owner identity for single-store, and marketplace seller scope.** No owner exists in `StoreCommerceContext` or in any Shipping/Fulfillment shipping table. Options: (a) add an owner column to Shipping policy tables with `Platform|Seller` + owner id, resolving the single-store owner from a new lawful platform seam; (b) keep single-store policy platform-global and add seller scope only for marketplace, with the single-store "owner" being an implicit platform scope; (c) defer owner scoping to a later slice and ship only single-store. **Existing-row ownership policy must also be decided** (who owns the rows that exist today).
- **D2 — Shipping-type taxonomy.** Owning module (Fulfillment vs Shipping), value set, configurability, translation requirement, and whether it changes pricing/coverage semantics or is purely classificatory.
- **D3 — Region-group persistence shape.** A new `shipping.region_groups` (owner-scoped, one geography selection) referenced by rate rows, vs reusing the existing per-row scope with a group id. Must not duplicate geography, must not break the existing unique index, and must define what happens to legacy rows with no group.
- **D4 — Explicit calculation modes + payment timing.** Where the mode enum lives, how `Free` interacts with `FreeAboveThreshold` and Promotion (must stay separate), and how `Carrier` behaves with no carrier integration.
- **D5 — Closed-day vs absent-window semantics and cutoff.** Whether "closed" is an explicit row flag or an absent-window convention, and where `cutoffHours` is stored (method vs schedule vs both).
- **D6 — Language enablement semantics.** Whether `ILanguageLookup` must expose `IsActive` (additive Contracts change) or whether the Admin HTTP registry remains the only consumer, and whether marketplace needs per-seller language enablement (currently impossible).
- **D7 — Migration execution.** Any new Shipping migration is applied only by the explicit `Tooba.MigrationRunner` CLI; who/when applies it, and whether the shared `:5432/tooba_alpha` may be touched, is an operator decision.

## 8. First implementation slice (smallest safe, independently reviewable)

**Recommended slice S1 — "owner/mode-neutral read+write hardening of the existing per-method policy editor", no new business semantics.**

- Owner: `Tooba.Shipping` (Application + Endpoints + Infrastructure), with **no** Fulfillment or Geography production change.
- Files/paths (candidate, exact namespace = path-derived):
  - `Tooba.Shipping.Application/Rates/Commands/ShippingRateCoverageCommands.cs` (existing) — group the existing per-row commands behind one "replace region rows for one method" command **only if** D3 is decided; otherwise keep as-is.
  - `Tooba.Shipping.Application/Validation/ShippingAdminValidators.cs` + the three duplicated `*ValidationCodes` classes — extract one shared `ShippingValidationCodes` and one shared body-reading seam in `Tooba.Shipping.Endpoints`.
  - `Tooba.Shipping.Endpoints/Admin/ShippingAdminAuthorizer.cs` — replace the hardcoded `Edition = SingleStore` with `ICurrentEdition.Current.Edition` (fail-closed, mirroring `SellerPanelAccess`).
  - `Tooba.Shipping.Contracts/Errors/ShippingErrorCodes.cs` (+ new `Tooba.Shipping.Endpoints/Errors/ShippingErrorCatalogContributor.cs` and `Tooba.Shipping.Endpoints/Resources/ShippingErrors.resx` + `.fa.resx`) — register the 44 existing stable codes as canonical `ErrorDescriptor`s with a `LocalizationKey`.
  - `Tooba.Shipping.Infrastructure/DependencyInjection/ShippingModule.cs` — remove stale remarks, register the new contributor.
- Allowed delta: additive only (error catalog registration + localisation resources + edition derivation + de-duplication). No schema change, no route change, no response-shape change.
- Why it is independent: it fixes proven canonical-mechanism gaps (`Stable-Error-Code-State = UNREGISTERED_CODES`, `Localization-State = MISSING_INFRASTRUCTURE_USE`, duplicated codes/body reader) that are prerequisites for any later owner-scoped work, changes no observable payload, and leaves all default-OFF gates untouched. It produces no dead code and no temporary illegal coupling.
- Acceptance (source-backed): all 44 `ShippingErrorCodes` resolve to exactly one canonical descriptor; `ErrorDefinitionCatalog` fail-fast duplicate detection still passes; every shipping admin failure renders a localised message for configured languages; edition in the authorization call context equals `ICurrentEdition.Current.Edition`; the 16 routes keep identical verb/path/status/body.
- Focused checks (proposed, **not run** in W0): `Tooba.Shipping.Tests` admin + composition guard tests, plus the Host `ErrorCatalogUniqueCodeGuardTests` and `TmarSourceSizeGuard`.

**Dependency-ordered remaining slices (planning only, not executable envelopes):**
1. **S2** — Error/localisation canonicalisation for Shipping (could be merged into S1).
2. **S3** — D1/D3 decision implementation: owner scope + region group + additive migration + backfill policy for ambiguous legacy rows.
3. **S4** — D4/D5: explicit calculation/payment modes + cutoff/closed-day additively on top of the existing schedule.
4. **S5** — D2: shipping-type taxonomy (Fulfillment-owned) + Admin ordering "type above method name".
5. **S6** — D6: language enablement exposure (Contracts additive) + multi-language acceptance matrix (1, 2, 3+ languages).
6. **S7** — Structure + Certify for `Tooba.Shipping` (the module has **no** manifest entry today; see `RESULT.md`).

## 9. Analysis state summary (per skill)

| Field | Value |
|---|---|
| Foundation-State (`Tooba.Shipping`) | `FOUNDATION_PARTIAL` — all six projects exist and obey layering; **no** entry in `tmar-module-structure-manifests.json`, no AMSC wave |
| Foundation-State (`Tooba.Fulfillment`) | `FOUNDATION_READY` (`structureCertified: true`) |
| Ownership-State | `MUST_SPLIT` for admin identity vs policy (already split Fulfillment/Shipping); `UNKNOWN_OWNER` for shipping-type taxonomy |
| File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (bounded: 4 duplicated body readers, 3 duplicated code classes) |
| Oversized/God-File-State | No file over the size baseline in the bounded surface; largest is `ShippingRateCoverageDirectory.cs` at 503 LOC |
| Localization-State | `MISSING_INFRASTRUCTURE_USE` (no resx, no `IErrorResourceSet`, no `IErrorCatalogContributor`) |
| API-Result-Pattern-State | `CANONICAL` |
| Stable-Error-Code-State | `UNREGISTERED_CODES` (44 declared, 0 registered) |
| Logging-State | `CANONICAL` (no console/debug/file logging in the bounded surface) |
| Sensitive-Logging-State | `NONE` found |
| OpenTelemetry-State | `CANONICAL` (no second pipeline; `Activity.Current` tags set by Host middleware) |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `COMPLIANT` |
| Validator-Coverage-State | `EXHAUSTIVE` (16/16) |
| Contracts-Boundary-State | `CLEAN` (Contracts→Contracts edge only) |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | No shipping business residue in Host; only composition, middleware, access adapters, migration CLI (permitted platform seams) |
| Schema-Migration-State | `UNCHANGED` (no migration generated or applied) |
| Behavior-Preservation-Risk | `LOW` for S1; `HIGH` for any owner-scope slice (data migration + semantics) |
| Structure-Handoff-State | `REQUIRED` — the module must be structured/certified before any Structure PASS is claimed |
