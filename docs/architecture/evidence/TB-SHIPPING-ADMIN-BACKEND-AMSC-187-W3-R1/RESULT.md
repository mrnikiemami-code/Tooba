# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1 — bounded module-call tracing repair and certification correction

- Task: `TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1` (parent `TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3`)
- Mode: `BOUNDED_MODULE_CALL_TRACING_REPAIR_AND_CERTIFICATION_CORRECTION`
- Skills applied sequentially inside this one task: `tooba-backend-feature`, `tooba-architecture-migrate`
  (authorized repair), `tooba-architecture-structure` (changed surface only), `tooba-architecture-certify`
  (focused corrective verdict, trace gate re-checked independently)
- Expected base / actual base: `2605344c10e6cd497349493357e9dcf3e6d666a4` /
  `2605344c10e6cd497349493357e9dcf3e6d666a4` (`HEAD == origin/main` before edits, no stash, no divergence)
- Code-SHA (repair commit): `3b39c90ffe567cef06a5b9d048cfa92014085187`
- Recovery state: `SAFE` (no reset/rebase/stash/force-push; the one unrelated untracked file
  `docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3/post-result.js` was left untouched)
- Verdict: **COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002** — corrective authority `W3-R1`
- Stop state: `STOP_FOR_ARCHITECT`, `automaticNextImplementationTask = NONE`

## 1. Architect ruling and explicit invalidation (done before the repair)

W3 was promoted at `2605344c` and then rejected:

- Certify section 10 requires *"cross-module calls use `IModuleCallTracer` decoration and trace
  continuity is not broken"*. `ModuleCallTracer`'s contract is explicitly for in-process cross-module
  calls, so a shared MediatR parent span is **not** a substitute for the missing module-call child span.
- A sibling certified module with the same omission is **not** exception authority. No exemption exists.
- W3's wording ("residual / optional hygiene") is withdrawn, not preserved as debt.

Invalidation was applied to the live metadata **before** touching production code, so the record could
never be green while the defect existed:

1. `docs/architecture/tmar-module-structure-manifests.json` — Shipping entry set to
   `structureCertified: false`, `certificationState: "NOT_CERTIFIED"`, with
   `certificationInvalidatedBy` / `certificationInvalidationReason` and a note stating that only the
   certification state was invalidated (physical structure untouched).
2. `docs/architecture/tmar-current-state.json` — Shipping removed from
   `structureLock.certifiedModules`, block state `TRACE_REPAIR_PENDING`, `verdict: NOT_CERTIFIED`.
3. `docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3/certification.md` — original W3
   text preserved verbatim; an append-only section now records the Architect rejection, the withdrawn
   residual claim and the superseding outcome.

After the repair passed, the same two files were promoted once (see §7). No entry is duplicated and no
unrelated module entry, physical root allowlist, W2 structural evidence or Host final-closure checkpoint
was modified.

## 2. Actual outbound edge inventory (verified, not assumed)

Verified from the adapter sources, `ShippingModule.AddServices`, and every direct constructor caller:
the **only** production constructions are the two `AddScoped` registrations in `ShippingModule`; all
other constructions are test fixtures. Bounded edge inventory = **3 executed foreign reads across 2
adapters** (full matrix in `call-site-span-matrix.md`):

| Adapter | Foreign member | Span |
|---|---|---|
| `GeographyDestinationScopeValidator` | `IGeographyLookup.GetByIdsAsync` | `module.Shipping.Geography.ValidateDestinationIdentifiers` |
| `FulfillmentShippingMethodOptionValidator` | `IShippingCatalogReader.ListAsync` | `module.Shipping.Fulfillment.ListShippingCatalog` |
| `FulfillmentShippingMethodOptionValidator` | `IShippingOneLevelCanonicalMethodReader.IsCanonicalMethodBindingAsync` (canonical fallback branch only) | `module.Shipping.Fulfillment.ResolveCanonicalMethodBinding` |

No additional outbound call exists inside this capability, so scope did not need to grow.

## 3. Repair (before → after)

`GeographyDestinationScopeValidator` (before): constructor `(IGeographyLookup geography)`; the raw
`await geography.GetByIdsAsync(...)` returned straight into the null-check with no span.

After: constructor `(IGeographyLookup geography, IModuleCallTracer moduleCallTracer)`; one
`using var moduleCall = moduleCallTracer.Begin("Shipping", "Geography", "ValidateDestinationIdentifiers")`
opened **before** the await and closed by `Dispose`, with `SetOk()` on an accepted scope,
`SetBusinessFailure(ShippingErrorCodes.DestinationScopeInvalid)` when Geography returns `null`, and
`SetError(exception); throw;` for anything thrown.

`FulfillmentShippingMethodOptionValidator` (before): constructor `(IShippingCatalogReader catalog,
IShippingOneLevelCanonicalMethodReader? canonicalMethods = null)`; two untraced foreign awaits.

After: constructor `(IShippingCatalogReader catalog, IModuleCallTracer moduleCallTracer,
IShippingOneLevelCanonicalMethodReader? canonicalMethods = null)`; a single private
`CallFulfillmentAsync<T>(operation, call)` helper opens one span per **executed** foreign read
(`ListShippingCatalog` always after the local empty-code rejection; `ResolveCanonicalMethodBinding`
only inside the existing `canonicalMethods is not null && legacy option absent` branch, which was
restructured into `if (canonicalMethods is not null) { var binding = await ...; if (binding) return ... }`
without changing order or semantics). `SetOk()` for `null`/`false` results (a completed lookup), and
`SetError(exception); throw;` for dependency throws.

Explicitly **not** done: no new `ActivitySource`/`Meter`/correlation id/header/`AsyncLocal`/framework,
no `StartActivity` in Application/Endpoints, no global decoration of shared Geography/Fulfillment
Contracts registrations, no Host edit, no foreign project reference, no new logging, no optional or
no-op tracer, no payload/identifier/coordinate/token/exception-message tag.

## 4. Real listener / correlation / behaviour / DI proof

`src/backend/Modules/Shipping/Tooba.Shipping.Tests/Observability/ShippingModuleCallTracingTests.cs`
(16 tests) uses the **real** `ModuleCallTracer` over the real `ToobaTelemetry.ActivitySource` with a real
`ActivityListener` (collection parallelization disabled; sink filtered to `module.Shipping.*` so
unrelated activities cannot falsify counts), never a spy and never a "null listener" shortcut.

Proved:

- span identity: name, `tooba.module.source=Shipping`, `tooba.module.target=Geography|Fulfillment`,
  `tooba.module`, `tooba.operation`, `tooba.request_kind=module_call`;
- `tooba.correlation_id` equals the ambient `CorrelationIdContext` scope value;
- correct parent (`ParentSpanId` == incoming span) and shared `TraceId`;
- closed exactly once per executed read (`Dispose` idempotent) and `Activity.Current` restored to the
  incoming parent after the call;
- Geography accepted, `null` rejection (business-failure tags, `Ok` status), exact forwarded
  `DestinationIdentifiers` and the very same cancellation token;
- Fulfillment service-level success (1 span, canonical branch not entered), legacy-option success
  (1 span, no canonical span), canonical `true` (2 distinct spans, exact forwarded codes), canonical
  `false` (2 `Ok` spans, Shipping rejection code unchanged), canonical reader absent (1 span, no
  fabricated call), unknown service (1 `Ok` span);
- local empty/whitespace service code ⇒ **zero** foreign spans and zero foreign calls;
- dependency throws ⇒ `exception.type` tag only and the **same** exception instance rethrown; caller
  cancellation stays `OperationCanceledException` (never converted into a business/unavailable result);
- exclusion: no exported tag value contains the sentinel exception message, service code, option code,
  or destination ids; no span events (no logging added);
- DI: both adapters resolve from the real `ShippingModule` registration over
  `AddToobaObservabilityFoundation()` (exactly one `IModuleCallTracer` = `ModuleCallTracer` singleton,
  both adapters `Scoped`, no duplicate/cycle/`ValidateScopes` failure), and a validator resolved through
  that container really emits the module-call span. A reflection guard forbids making the tracer
  optional/defaulted again.

## 5. Changed surface and structural proof

`tooba-architecture-structure`, changed surface only: no new production file, no folder/namespace/
capability/folder-granularity/solution-grouping change. Both edits are inline in existing capability
files (`Tooba.Shipping.Infrastructure/Rates/`), path↔namespace exact, root allowlists untouched, no
wrapper introduced, no root widening, W2 structural evidence not re-run for the whole repository. The
only new file is a test-only `Observability/` folder in `Tooba.Shipping.Tests` (test project is outside
the production folder/root guards). `ShippingModuleStructureGuardTests` still passes every physical/
capability/namespace/root/solution guard; only its lifecycle/authority assertions were updated to pin
the corrective provenance (W3-R1 authority, W3-R1 evidence path, rejection/history text, `IModuleCallTracer`
mentioned in `certificationCorrection`). No assertion accepts "any state", no skip, no false pointer.

## 6. Reused vs re-executed certification evidence

Reused **by exact source identity** — `git diff 2605344c..3b39c90` proves these trees are bit-identical,
so every W3 prerequisite recorded against them remains valid without re-running:

| W3 prerequisite | Reuse proof | Status |
|---|---|---|
| 16 shipped `/v1/admin` routes / 16 requests / 16 handlers / 16 validators + 6 nested (set equality) | `ShippingRequestClassificationTests` re-ran green; route/endpoint/handler/validator files unchanged | RE-RAN (cheap, in-suite) |
| 44 declared = 44 registered = 44 `.resx` keys (neutral + `fa`) | `git diff` empty on Contracts/Errors + Localization resources | REUSED |
| Canonical `ApiResponseFactory` result mapping, legacy HTTP 400 compatibility | `git diff` empty on Endpoints + Presentation | REUSED |
| Authorization / permissions per route | `git diff` empty on Endpoints | REUSED |
| Contracts-only cross-module boundaries; Order shadow boundary (TB-079/080) | `git diff` empty on `Geography.Contracts`, `Fulfillment.Contracts`, Order observer files | REUSED |
| Schema/migrations (13 migration files, snapshot) | `git diff` empty on `Tooba.Shipping.Infrastructure/Persistence`; SHA-256 prefixes recorded below | REUSED |
| Host composition / final-closure checkpoints | `git diff` empty on `src/backend/Host` | REUSED |
| W2 physical structure (roots, foldering, namespaces, solution grouping) | `git diff 2605344c` touches only two `Rates` files; guards re-ran green | REUSED + guarded |
| Module-call tracing on outbound reads (**the rejected gate**) | previously absent | **REPAIRED AND EXECUTED NOW** |

Migration SHA-256 prefixes (unchanged, matching W3): `D1D45877D9FD 20261014144742_InitialShipping.cs`,
`3B2B70BE93DA …Designer.cs`, `625E87E3C070 20261015030159_OptionLevelRatePolicy.cs`, `B1C18673F49C …`,
`037D65E71229 20261015045130_OperatingSchedule.cs`, `3C5B530793AE …`, `F8EFEB918237
20261015063838_WeightAndCountRateTiers.cs`, `27EF33E7E074 …`, `9B841CC28D3E
20261015071836_ShippingPackagingPolicy.cs`, `DCB731A93F30 …`, `245AF200F70B
20261016211322_OptionScopedCoverageRestriction.cs`, `214B32D817D6 …`, `D88D724E34E3
ShippingDbContextModelSnapshot.cs` (13 files total, zero diff).

## 7. Corrective certification metadata

- `docs/architecture/tmar-module-structure-manifests.json` — the **single** Shipping entry is
  `structureCertified: true`, `lockVersion: "ARCH-COMPLETE-002"`,
  `certificationState: "COMPLETE_REFERENCE_PATTERN"`, `certificationAuthorityTask:
  "TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1"`, `certificationHistory:
  "…W3_PROMOTED_THEN_ARCHITECT_REJECTED_AND_SUPERSEDED_BY_W3-R1"`, `evidence:
  ".../TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1/RESULT.md"`, plus `certificationCorrection` naming the
  rejected gate and the exact repaired call sites. `structureAuthorityTask` stays
  `TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2` / `READY_FOR_CERTIFY`.
- `docs/architecture/tmar-current-state.json` — Shipping present in `structureLock.certifiedModules`
  exactly once (33 entries, no duplicate); the `shippingArchComplete002Structure` block is `CERTIFIED`
  / `COMPLETE_REFERENCE_PATTERN`, records `w3HistoricalVerdict`,
  `w3RejectionReason`, `w3EvidenceRetained`, the tracer edge list, the listener proof state, the new
  guard list and `evidence: .../W3-R1/RESULT.md`. `guardsWeakened: NONE`, `baselinesWidened: NONE`.
- W3's historical report is retained, append-only annotated as rejected/superseded.

## 8. Focused validation commands and counts

All commands ran in the repository root, each well under the 180 s cap, on a task-owned isolated
artifacts path (`.tmp-w3r1-artifacts`, removed afterwards; shared `bin`/`obj`, running Host and any
running process untouched; no browser/DB/container/package-install/migration/schema command).

1. `dotnet test src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj --artifacts-path .tmp-w3r1-artifacts --filter "FullyQualifiedName~ShippingModuleCallTracingTests"`
   → **15 passed / 0 failed** (first run, before the mandatory-tracer guard was added).
2. Same project, full module suite after the invalidation edit → **405 passed, 1 failed** (only the
   expected `Manifest_entry_is_certified_…` lifecycle assertion, because metadata was deliberately
   invalidated at that moment).
3. `--filter "FullyQualifiedName~Both_adapters_require_the_canonical_tracer"` → 1 failed on
   `ParameterInfo.DefaultValue`/`DBNull` assumption; deterministic in-scope fix to
   `HasDefaultValue` (no weakening of the requirement).
4. Full Shipping module suite, final → **407 passed, 0 failed, 0 skipped** (391 W3 baseline + 16 new
   tracing tests). W2 structure guards, distribution/classification guards and compatibility guards all
   green inside that run.
5. `dotnet build` proof of the affected projects is implicit in the test builds (Infrastructure,
   Application, Contracts, Endpoints compile through the test project); no production project outside
   Shipping was rebuilt or touched.
6. `git diff --name-only 2605344c -- <Persistence|Geography.Contracts|Fulfillment.Contracts|Host>` →
   empty (reuse proof).

No full-solution/Order/architecture-wide suite was run, per the task's bounds.

## 9. Remaining blockers and pending product work

- `Exact-Remaining-Blockers: NONE` for the ARCH-COMPLETE-002 certification of this baseline.
- D1–D7 product requirements (owner-scoped single admin control panel, marketplace seller authoring,
  region groups with multiple condition/price rows, configured-language UI, payment/carrier/free modes,
  cutoff, shipping-type taxonomy) remain **unimplemented** and are not certified by this verdict.
- Integrated Admin runtime acceptance, browser E2E, deployment and customer-facing acceptance remain
  pending and are not implied by architecture certification.
- In-process tests are not production traffic; the OpenTelemetry exporter pipeline itself was not
  exercised (no collector/DB/container allowed), only the real `ActivitySource` + listener contract.

## 10. Isolation, cleanliness and elapsed

- Task-owned artifacts `.tmp-w3r1-artifacts` removed; no generated file added to git.
- Scoped commit only: 2 production files, 4 Shipping test files (1 new), 2 metadata files, 1 appended
  W3 evidence file, task receipt + this evidence. Unrelated untracked
  `docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3/post-result.js` preserved untouched.
- Elapsed (hard budget 45 min): see Bridge result `Elapsed-Minutes`.
