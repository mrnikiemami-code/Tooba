# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0 — RESULT

**Task:** `TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0`
**Parent:** `TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2`
**Program:** `TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY`
**Mode:** `BOUNDED_AMSC_ANALYZE_CONTRACTS_AND_LEGACY_MAPPING_ONLY`
**Skill:** `tooba-architecture-analyze` (Analyze only; Migrate/Structure/Certify not executed)
**Expected base:** `68d1aeb3525e8b2d43c719578b433874d26e09d4`
**Base-SHA / inspected commit:** `68d1aeb3525e8b2d43c719578b433874d26e09d4` (`HEAD == origin/main`, fast-forward only from `fefc38ce9`)
**Code-SHA:** `UNCHANGED`
**Status:** `PASS`
**Final-Disposition:** `NEEDS_ARCHITECT_DECISION`

Supporting evidence in this directory: `SOURCE-MAP.md`, `INPUT-VALIDATION-MATRIX.md`, `MODEL-LEGACY-AND-HANDOFF.md`, `task-receipt.json`.

---

## 1. What was done

Bounded AMSC Analyze of the Shipping Admin authoring capability and its direct authoritative dependencies: `Tooba.Shipping`, the Fulfillment shipping catalog/identity capability it consumes, Geography destination identity, Localization language authority, StoreContext/platform edition context, and the Host composition/authorization boundaries. Product authority read: `SHIPPING-ADMIN-RULES-DECISION.md` §1–§15, `SHIPPING-STOREFRONT-UX-REFERENCE.md`, the fee-allocation decision, the dated architect recovery ledger, and the architecture skills/standards.

No production code, schema, test, configuration, manifest, certification state or runtime data was modified. No container, database, seed, package install or browser session was started. No test or build was executed (documentation-only analysis).

## 2. Required state fields

| Field | Value |
|---|---|
| **Analyzed-Surface-And-Applicability** | `Tooba.Shipping` = `HTTP_OWNING` (16 shipped admin routes, `Program.cs:479`). `Tooba.Fulfillment` shipping capability = `HTTP_OWNING` (certified). `Tooba.Geography` = `HTTP_OWNING` (admin reads). `Tooba.Localization` = `HTTP_OWNING` registry + `INTERNAL_ONLY` lookup seam. `Tooba.StoreContext` / BuildingBlocks edition-tenant = `INTERNAL_ONLY`. Shipping operational/quote/rate/packaging read seams = `INTERNAL_ONLY`. No `MIXED_OR_UNKNOWN`. Detail: `SOURCE-MAP.md §2`. |
| **Canonical-Owner-And-Dependency-Map** | **Fulfillment** owns shipping method identity, codes, catalog rows, translations, activation and one-level canonical binding. **Shipping** owns rate/coverage/packaging/operating-schedule *policy* and its own `shipping` schema. **Geography** owns destination identity/hierarchy. **Localization** owns the language registry and default/direction/culture. **Platform (BuildingBlocks + Host control plane)** owns `ToobaEdition`, tenant resolution and the store commerce context. **AccessControl via platform seams** owns authorization. **Order/Payment** own customer promise, money and allocation. Dependency graph is Contracts-only: `Shipping.Contracts -> Geography.Contracts`; `Shipping.Infrastructure -> Fulfillment.Contracts + Geography.Contracts`. Detail: `SOURCE-MAP.md §1, §4`. |
| **Single-Store-Marketplace-Authority** | `UNPROVEN_GAP`. Edition is a real process-level platform value (`ToobaEdition`) resolved once by Host middleware; `StoreCommerceContext` carries **only** `Market/DefaultCurrency/SalesChannel` and **no** merchant/owner identity. No Shipping table and no Fulfillment shipping catalog row has an owner/seller/tenant column. Seller identity is only reachable through `ISellerPanelAccess` (`(ActorUserId, SellerPartyId)`), which the shipping admin gate does not use. `ShippingAdminAuthorizer` hardcodes `Edition = SingleStore` and `TenantId = "unknown"` in its authorization call context, replicating an existing pre-refactor admin pattern rather than the canonical `ICurrentEdition` derivation used by `SellerPanelAccess`. Marketplace therefore has **no** seller-scoped shipping authoring and **no** per-seller isolation in its single shared database. Detail + matrix: `MODEL-LEGACY-AND-HANDOFF.md §2`. |
| **Database-Language-Authority** | Proved: `ILanguageLookup.ListAsync` returns **all** language rows (no `IsActive` filter, no store filter) and `LanguageLookupSnapshot` exposes only `LanguageId, Code, Culture, UrlPrefix, IsDefault` — so it does **not** carry active/store-enabled semantics. There is **no** per-store enablement table: in SingleStore the Localization DB *is* the store DB; in Marketplace the table is marketplace-global. The live Admin already uses the lawful, authorised `GET /v1/admin/languages` route and keys translations by real `languageId`. No Shipping language table, duplicate resolver or fa/en hardcode exists in the backend. Detail: `MODEL-LEGACY-AND-HANDOFF.md §3`. |
| **Target-Model-And-Legacy-Transition** | Only identity/translations/single-amount freight/packaging amount/preparation+lead days/destination scope values/per-weekday windows are persisted today. **Unmodelled:** owner scope, shipping-type taxonomy, region-group container, merchandise-amount condition tier, explicit calculation modes (free/carrier/rule-based/provider-quote), cutoff, closed-day flag. 12 explicit legacy conflicts registered (C1–C12) with `KEEP`/`EXTEND`/`CHANGE`/`ADDITIVE` decisions; no destructive collapse of distinct legacy rates, schedules or owners is proposed. Detail: `MODEL-LEGACY-AND-HANDOFF.md §1, §4`. |
| **Contracts-Boundary-State** | `CLEAN`. Contracts-only by project reference; no foreign Application/Infrastructure/Domain edge; the only cross-module edge in Contracts is `Shipping.Contracts -> Geography.Contracts` (reuse of `DestinationIdentifiers`), which is legal but makes Geography's contract assembly a transitive dependency. |
| **Cross-Module-Coupling-State** | `LEGAL_CONTRACTS_ONLY`. Fulfillment seam: `IShippingCatalogReader`, `IShippingOneLevelCanonicalMethodReader`. Geography seam: `IGeographyLookup.GetByIdsAsync`. |
| **Cross-Module-Join-State** | `NONE`. No cross-schema join, foreign `DbSet`, `FromSql` or navigation. |
| **Validator-Coverage-State** | `EXHAUSTIVE` for the bounded surface: 16 shipped routes → 16 distinct MediatR requests → 16 validators, 0 `NO_VALIDATOR_REQUIRED`, no orphan/duplicate. Set-equality proof and per-input provenance: `INPUT-VALIDATION-MATRIX.md`. |
| **Canonical-API-Error-Localization-State** | API result mapping `CANONICAL` (`ApiResponseFactory.From` everywhere; no `Results.Json`, no ad-hoc mapper, no `ex.Message` parsing). **But** stable-error-code state is `UNREGISTERED_CODES` (44 `ShippingErrorCodes`, **0** `ErrorDescriptor`s, no `IErrorCatalogContributor`, **no resx / `IErrorResourceSet`**) → `Localization-State = MISSING_INFRASTRUCTURE_USE`. Transport validation codes are declared three times in three static classes with identical strings, and the body-reading seam is duplicated verbatim in four endpoint files. |
| **Foundation-State** | `Tooba.Shipping = FOUNDATION_PARTIAL` — all six projects exist with correct layering, but the module has **no** entry in `tmar-module-structure-manifests.json` and no AMSC wave in `tmar-current-state.json`. `Tooba.Fulfillment = FOUNDATION_READY` (`structureCertified: true`). |
| **Structure-Handoff-State** | `REQUIRED`. Physical layout is capability-first and shallow on inspection (`Application/{Rates,Packaging,Scheduling,Operations,Validation}`, `Endpoints/Admin`, `Infrastructure/{Rates,Packaging,Scheduling,Operations,Persistence,DependencyInjection}`), but Structure PASS and certification are **not** claimed in W0; `Tooba.Shipping` needs its own Structure/Certify bounded tasks. |
| **Behavior-Preservation-Risk** | `LOW` for the recommended first slice (additive catalog/localisation/edition-derivation/de-duplication); `HIGH` for any owner-scope or region-group slice (data migration + semantics). |
| **Focused-Checks-Actually-Run** | `NONE`. Documentation-only task; no test/build/HTTP/DB/browser command was executed. Proposed (not run) checks are listed in `MODEL-LEGACY-AND-HANDOFF.md §8`. |
| **Production-Changes** | `NONE` |
| **Schema-And-Runtime-Writes** | `NONE` |
| **automaticNextImplementationTask** | `NONE` |

## 3. Responsibility-to-owner table (single owner per concern)

| Concern | Proved owner | Evidence |
|---|---|---|
| Shipping method identity/code | Fulfillment | `ShippingMethod`, `ShippingService`, `ShippingServiceOption`, one-level catalog |
| Method names/descriptions per language | Fulfillment | `ShippingServiceTranslation`, `ShippingOneLevelMethodTranslationSnapshot` |
| Method activation | Fulfillment | one-level activation route + `ShippingMethod.IsActive` |
| Shipping type taxonomy | **UNOWNED (GAP)** | no type entity anywhere |
| Rate amount/currency/thresholds/lead times | Shipping | `ShippingRatePolicy` |
| Coverage restriction mode + grants | Shipping | `ShippingMethodCoverage`, `ShippingCoverageGrant` |
| Packaging fee + freight payer mode | Shipping | `ShippingPackagingPolicy` |
| Weekly operating windows + timezone + capacity | Shipping | `ShippingOperatingSchedule`, `ShippingOperatingWindow` |
| Destination identity/hierarchy/validity | Geography | `DestinationIdentifiers`, `IGeographyLookup` |
| Known/active/default language + direction/culture | Localization | `Language`, `LanguageDirectory`, `/v1/admin/languages` |
| Edition / tenant / store commerce context | Platform (BuildingBlocks + Host) | `ToobaEdition`, `ICurrentCommerceContext`, `TenantResolutionMiddleware` |
| Admin/seller authorization | AccessControl via platform seams | `IAdminPanelAccess`, `ISellerPanelAccess`, `IAuthorizationService` |
| Customer money, shipping charge allocation, promise snapshot | Order/Payment | fee-allocation decision; Order snapshots |

## 4. Honest findings requiring Architect attention

1. **No owner concept exists** in Shipping or the shipping catalog; marketplace seller-scoped rules cannot be implemented without a schema/ownership decision (D1).
2. **`StoreCommerceContext` cannot supply the single-store canonical merchant owner** — the contract has three strings and no owner. No lawful single-store merchant identity seam was found in the repository; this is `UNKNOWN`, not merely unimplemented.
3. **`ShippingAdminAuthorizer` hardcodes `Edition = SingleStore` and `TenantId = "unknown"`**, so the authorization call context is not derived from the authoritative edition. The same pattern exists in other pre-existing admin authorizers; the canonical derivation exists in `SellerPanelAccess`.
4. **44 stable shipping error codes are unregistered and unlocalised** — no contributor, no resx. Every shipping failure currently falls back to generic presentation.
5. **`ILanguageLookup` is not an "enabled languages" source** — it returns inactive languages too and omits `IsActive`/`Direction`. Reusing it as-is would let the editor offer a language the store has disabled.
6. **Stale module XML documentation** still claims Shipping is not composed, writes nothing and has no endpoint; the code proves otherwise. Documentation drift only.
7. **Shipping migrations are applied only by the explicit `Tooba.MigrationRunner` CLI**, never at Host startup. Any future migration needs an operator step.
8. **Shipping type taxonomy vs the older "no Types of Service" lock** must be explicitly reconciled: the approved field is a shipping-class taxonomy, not a resurrection of provider child-service-types.
9. Duplicated body-reader (4 files) and duplicated validation-code constants (3 classes) are cohesion debt requiring one owner during Migrate.

## 5. First implementation slice (recommended, not executed)

**S1 — canonicalisation hardening with zero observable behaviour change**, owned by `Tooba.Shipping`:

- register the 44 existing stable `ShippingErrorCodes` as exactly one canonical `ErrorDescriptor` each (new `Tooba.Shipping.Endpoints/Errors/ShippingErrorCatalogContributor.cs`), with neutral + configured-locale `.resx` under `Tooba.Shipping.Endpoints/Resources/` and an `IErrorResourceSet`;
- derive the authorization call-context edition from `ICurrentEdition.Current.Edition` (fail-closed), replacing the hardcoded `SingleStore`;
- extract one shared body-reading seam and one shared `ShippingValidationCodes` owner;
- remove the stale module XML remarks.

Allowed delta: additive only — no route, no schema, no response shape, no flag change; all default-OFF gates untouched. It is independently reviewable/committable, introduces no dead code and no temporary illegal coupling, and it is a prerequisite for every later owner-scoped slice.

Remaining dependency-ordered slices (planning only): S3 owner scope + region group + migration/backfill, S4 calculation/payment modes + cutoff/closed-day, S5 shipping-type taxonomy, S6 language enablement + 1/2/3+ language acceptance matrix, S7 Structure + Certify for `Tooba.Shipping`. Detail: `MODEL-LEGACY-AND-HANDOFF.md §8`.

## 6. Unresolved decisions (blocking implementation, no guessing)

- **D1** Canonical merchant/owner identity for single-store + marketplace seller scope + ownership of existing rows.
- **D2** Shipping-type taxonomy: owner, value set, configurability, translation, whether it affects pricing/coverage.
- **D3** Region-group persistence shape and legacy rows without a group.
- **D4** Explicit calculation modes and payment timing vs `FreeAboveThreshold` vs Promotion vs carrier integration.
- **D5** Closed-day vs absent-window semantics, and where the cutoff lives.
- **D6** Language enablement semantics (`ILanguageLookup` extension vs HTTP-only) and whether marketplace needs per-seller enablement.
- **D7** Migration execution authority against the shared database.

## 7. Scope compliance

- Writes limited to `docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0.task.md` and `docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0/`.
- No production/schema/test/manifest/SoT-certification edit; no container/server/DB/seed/package/browser work; no broad test/build suite; no test/repair loop.
- Preserved shared listeners/apps (`:5088`, `:3000`), running services, unrelated work and all three pre-existing stashes. No reset, rebase, stash or force-push.
- No next task scheduled, no implementation started, TB-185 remains paused, Storefront/checkout/payment/deployment and production flag activation remain out of scope.
- `Production-Functional-Acceptance: PENDING` — the visual baseline acceptance of TB-186-R2 is **not** acceptance of production behaviour or permission to deploy.
