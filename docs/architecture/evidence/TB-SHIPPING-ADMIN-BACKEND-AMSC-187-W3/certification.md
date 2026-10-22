# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3 — Shipping architecture certification (ARCH-COMPLETE-002)

- Task: `TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3` (parent `TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1`)
- Skill: `tooba-architecture-certify` (+ `tooba-architecture-structure` gate re-read)
- Mode: `BOUNDED_AMSC_CERTIFY_EXISTING_SHIPPING_BASELINE`
- Expected base / actual base: `8fa3b7842985575ccb611eb7411be765caee00b1` / `8fa3b7842985575ccb611eb7411be765caee00b1`
- Production code: **UNCHANGED** (zero `src/backend/**` production edits; test + metadata + evidence only)
- Verdict: **COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED**
- Stop state: `STOP_FOR_ARCHITECT`, `automaticNextImplementationTask = NONE`

Lineage: W0 analyze → W1 canonical errors/edition guard `7e6b1479` → W2 structure `59ce3625` (evidence `0d9936f0`) → W2-R1 approved-shadow compatibility `6c54c821` (evidence `8fa3b784`) → W3 certify (this commit).

## 1. Applicability and endpoint ownership

`HTTP_OWNING` — proven from shipped routes, not project names.

- Composition entry: `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/ShippingEndpointModule.cs` (`MapShippingEndpoints`) maps exactly one `/v1/admin` group and five capability mappers (`ShippingRateCoverageEndpoints`, `ShippingMethodFinancialStatusEndpoints`, `ShippingOperatingScheduleEndpoints`, `ShippingPackagingPolicyEndpoints`, `ShippingOperationalQuoteEndpoints`).
- Shipped routes: **16** (all module-owned; Host-owned Shipping route count = 0). Guard: `ShippingModuleStructureGuardTests.Endpoints_composition_entry_lives_at_the_root_and_nowhere_else`, `ShippingAdminEndpointOwnershipTests`.
- Every route is authorized through the module authorizer seam with existing platform permissions (`fulfillment.view` / `fulfillment.manage`); no `AllowAnonymous`, no invented permission id.

## 2. Structure gate (handoff from W2, re-verified current)

| Gate | State | Source |
| --- | --- | --- |
| Structure-State | `READY_FOR_CERTIFY` | W2 `59ce3625` / evidence `0d9936f0` |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | W2 + this wave's guards |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Shipping/`, 6 projects once each) | `Solution_membership_is_canonical_and_unique` |
| Path-Namespace-State | `EXACT` | `Every_production_namespace_equals_its_path_derived_namespace` |
| Physical-Copy-State | `CLEAN` | `No_stale_or_duplicate_production_copy_exists` |
| Root-Allowlist-State | `ENFORCED` | `Root_allowlists_and_forbidden_paths_match_the_real_physical_roots` |
| per-use-case single-file leaves | zero | `Physical_tree_is_capability_first_without_single_file_request_leaves` |

Current disk state was re-checked by running the W2 guards in this wave; they are green against the promoted manifest.

## 3. Independent route / input / validator set equality (hard gate)

Independently re-derived from source (not adopted from W0/W2): every `group.Map*` call site → handler body → `sender.Send(new <Request>(...))` → authoritative `IRequest`, plus the complete `IRequestHandler<,>` and `AbstractValidator<>` universe of `Tooba.Shipping.Application`.

| # | Route (verb + path) | Request | Handler | Validator | Inputs |
| --- | --- | --- | --- | --- | --- |
| 1 | GET `/v1/admin/shipping-services/{serviceCode}/rates` | `GetShippingRatesAndCoverageQuery` | `GetShippingRatesAndCoverageHandler` | `GetShippingRatesAndCoverageQueryValidator` | route `serviceCode` |
| 2 | POST `/v1/admin/shipping-services/{serviceCode}/rates` | `CreateShippingRatePolicyCommand` | `CreateShippingRatePolicyHandler` | `CreateShippingRatePolicyCommandValidator` (+ nested) | route + body |
| 3 | PUT `/v1/admin/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}` | `UpdateShippingRatePolicyCommand` | `UpdateShippingRatePolicyHandler` | `UpdateShippingRatePolicyCommandValidator` (+ nested) | route id + route code + body |
| 4 | PATCH `/v1/admin/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}/active` | `SetShippingRatePolicyActiveCommand` | `SetShippingRatePolicyActiveHandler` | `SetShippingRatePolicyActiveCommandValidator` | route id + code + body flag |
| 5 | POST `/v1/admin/shipping-services/{serviceCode}/coverage-grants` | `CreateShippingCoverageGrantCommand` | `CreateShippingCoverageGrantHandler` | `CreateShippingCoverageGrantCommandValidator` (+ nested) | route + body |
| 6 | PATCH `/v1/admin/shipping-services/{serviceCode}/coverage-grants/{coverageGrantId:guid}/active` | `SetShippingCoverageGrantActiveCommand` | `SetShippingCoverageGrantActiveHandler` | `SetShippingCoverageGrantActiveCommandValidator` | route id + code + body flag |
| 7 | PUT `/v1/admin/shipping-services/{serviceCode}/method-coverage` | `SetShippingMethodCoverageCommand` | `SetShippingMethodCoverageHandler` | `SetShippingMethodCoverageCommandValidator` (+ nested) | route + body |
| 8 | GET `/v1/admin/shipping-services/{serviceCode}/financial-status` | `GetShippingMethodFinancialStatusQuery` | `GetShippingMethodFinancialStatusHandler` | `GetShippingMethodFinancialStatusQueryValidator` | route code + **optional query `optionCode` (caller-controlled)** |
| 9 | GET `/v1/admin/shipping-services/{serviceCode}/operating-schedules` | `GetShippingOperatingSchedulesQuery` | `GetShippingOperatingSchedulesHandler` | `GetShippingOperatingSchedulesQueryValidator` | route code |
| 10 | PUT `/v1/admin/shipping-services/{serviceCode}/operating-schedules` | `UpsertShippingOperatingScheduleCommand` | `UpsertShippingOperatingScheduleHandler` | `UpsertShippingOperatingScheduleCommandValidator` (+ nested) | route + body |
| 11 | PATCH `/v1/admin/shipping-services/{serviceCode}/operating-schedules/{scheduleId:guid}/active` | `SetShippingOperatingScheduleActiveCommand` | `SetShippingOperatingScheduleActiveHandler` | `SetShippingOperatingScheduleActiveCommandValidator` | route id + code + body flag |
| 12 | POST `/v1/admin/shipping-services/{serviceCode}/operational-quote` | `EvaluateShippingOperationalQuoteQuery` | `EvaluateShippingOperationalQuoteHandler` | `EvaluateShippingOperationalQuoteQueryValidator` (+ nested) | route + body |
| 13 | GET `/v1/admin/shipping-services/{serviceCode}/packaging-policies` | `GetShippingPackagingPoliciesQuery` | `GetShippingPackagingPoliciesHandler` | `GetShippingPackagingPoliciesQueryValidator` | route code |
| 14 | POST `/v1/admin/shipping-services/{serviceCode}/packaging-policies` | `CreateShippingPackagingPolicyCommand` | `CreateShippingPackagingPolicyHandler` | `CreateShippingPackagingPolicyCommandValidator` (+ nested) | route + body |
| 15 | PUT `/v1/admin/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}` | `UpdateShippingPackagingPolicyCommand` | `UpdateShippingPackagingPolicyHandler` | `UpdateShippingPackagingPolicyCommandValidator` (+ nested) | route id + code + body |
| 16 | PATCH `/v1/admin/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}/active` | `SetShippingPackagingPolicyActiveCommand` | `SetShippingPackagingPolicyActiveHandler` | `SetShippingPackagingPolicyActiveCommandValidator` | route id + code + body flag |

Set equality: shipped routes 16 = classified rows 16 = endpoint-reachable requests 16 = Application `IRequest` declarations 16 = handlers 16 = request validators 16. `NO_VALIDATOR_REQUIRED` rows: **0** (no exemption to justify). Nested transport validators: **6** (`ShippingRatePolicyWriteRequestValidator`, `ShippingCoverageGrantWriteRequestValidator`, `ShippingMethodCoverageWriteRequestValidator`, `ShippingPackagingPolicyWriteRequestValidator`, `ShippingOperatingScheduleWriteRequestValidator`, `ShippingOperationalQuoteEvaluationRequestValidator`), each proven wired by a `SetValidator(new …)` call in its parent. Declared validator universe = 22.

No input is server-derived or inherited: the only optional input (`optionCode` on route 8) is caller-controlled and bounded when present, so optionality is not treated as safety. Durable source: `docs/architecture/shipping/shipping-request-classification.json`, enforced by `ShippingRequestClassificationTests` (route/request/handler/validator set equality plus negative detection of a new unclassified route/request).

Validator execution: `AddToobaCqrsFoundation(Tooba.Shipping.Application) → AddValidatorsFromAssembly` + `ValidationBehavior<,>` in the MediatR pipeline; the guard resolves every `IValidator<T>` through the real DI and asserts the resolved type equals the classified validator. Representative paths: omitted/empty `serviceCode` → `shipping.validation.service_code_required`; valid `serviceCode` passes; present-but-invalid nested rate envelope → declared `shipping.validation.scope_required` (see §5 for the absent-envelope wire note).

## 4. Canonical results, errors, languages, DI

- Success/failure mapping: every handler returns `api.From(await sender.Send(...))`; **zero** `Results.Json` / `Results.BadRequest` / `Results.Problem` / local mapper in the module. Unreadable or absent JSON body is converted by the endpoint body reader into the declared `shipping.validation.body_invalid` validation envelope before dispatch (canonical `ApiResponseFactory.FromException`).
- W1 compatibility decision preserved: the pre-existing semantic Shipping failures intentionally keep legacy HTTP 400 / `Business` / `Warning` with localized titles; this was not re-inferred from code-name suffixes. `ShippingErrorCatalogAndResourcesTests.Every_registered_descriptor_keeps_the_legacy_http_contract` stays green.
- Stable codes: `Contracts/Errors/ShippingErrorCodes.cs` declares **44** codes; the module catalog contributor registers **44** descriptors (name set equal, no duplicate descriptor, no first/last-wins or `DistinctBy` suppression, composed catalog constructible). Guard: `ShippingErrorCatalogAndResourcesTests.Declared_codes_and_registered_descriptors_are_set_equal`, `Composed_catalog_has_one_owner_and_suppresses_nothing`.
- Error/non-error separation: `shipping.validation.*` are FluentValidation field codes and the remaining `shipping.*` literals in Contracts are diagnostic reason codes; neither is a competing descriptor owner. No undeclared semantic emitter exists (44 used = 44 declared).
- Localization: single `IErrorResourceSet` (`ShippingErrorResourceSet`) with neutral + `fa` `.resx`, 44 keys each, canonical locale resolver and neutral fallback (`Uncovered_locale_falls_back_to_the_neutral_shipping_title`); no hard-coded user-facing text in Domain/Application/Endpoints/Infrastructure; no raw `Accept-Language` parsing and no two-language hard limit.
- DI/authorization: module registration (`ShippingModule`) owns directories/lookups/adapters (`AddScoped`), the endpoints project registers exactly one catalog contributor and one resource set, Host binds `IShippingAdminAuthorizer → ShippingAdminAuthorizer`; no startup schema migrator; the authorizer consumes the platform admin-panel/authorization seams and denies with Foundation codes (no owner/mode header authority invented).

## 5. Boundaries, persistence, Host, observability

- Project/composition edges: Contracts → BuildingBlocks + `Geography.Contracts`; Infrastructure → own Contracts/Application/Domain + `Geography.Contracts` + `Fulfillment.Contracts` + BuildingBlocks/ModuleContracts/Persistence. Foreign **Application/Infrastructure/Domain** edges: **zero**. Cross-module joins / foreign DbSets / schema SQL: **zero**.
- Approved read-only Order shadow boundary (TB-079/TB-080) re-verified with the W2-R1 guards: exactly three reviewed Order files consume the operational contracts and may not import Shipping internals; negative tests fail on any unexpected fourth consumer or implementation leak. No production consumer edit or cutover in this wave.
- Persistence: Shipping owns its `ShippingDbContext` and schema. Migration artifacts unchanged — zero diff under `Tooba.Shipping.Infrastructure` and stable hashes:

```
20261014144742_InitialShipping.cs                  D1D45877D9FD77FF
20261014144742_InitialShipping.Designer.cs         3B2B70BE93DAAC01
20261015030159_OptionLevelRatePolicy.cs            625E87E3C0707823
20261015030159_OptionLevelRatePolicy.Designer.cs   B1C18673F49C0ED2
20261015045130_OperatingSchedule.cs                037D65E71229E885
20261015045130_OperatingSchedule.Designer.cs       3C5B530793AE293E
20261015063838_WeightAndCountRateTiers.cs          F8EFEB91823743BD
20261015063838_WeightAndCountRateTiers.Designer.cs  27EF33E7E07491D7
20261015071836_ShippingPackagingPolicy.cs          9B841CC28D3EAECC
20261015071836_ShippingPackagingPolicy.Designer.cs DCB731A93F304A74
20261016211322_OptionScopedCoverageRestriction.cs  245AF200F70B183B
20261016211322_OptionScopedCoverageRestriction.Designer.cs 214B32D817D64D68
ShippingDbContextModelSnapshot.cs                  D88D724E34E364E2
```

- Host authority: `ALLOWED_COMPOSITION_ROOT` only — `Program.cs` module DI, one `AddShippingEndpointPresentation()`, one `MapShippingEndpoints()`, the authorizer binding, the Application assembly registration for CQRS/validators; `ToobaModuleComposition` module list; `ModuleMigrationRegistry` descriptor. No Shipping business/endpoint/persistence authority in Host. Host final closure preserved; no new Host folder or file this wave.
- Logging: zero `Console`/`Debug` writers, zero second pipeline, zero sensitive-data logging, zero ad-hoc logger usage in the module (no log call site to repair and none added).
- Correlation/trace: foundation `TracingBehavior<,>` decorates all 16 requests; `MediatRTraceNaming` resolves module `Shipping` and activity `mediatr.Shipping.<Request>` for every classified request (guarded). No parallel correlation id, no manual `traceparent` parsing, no second `ActivitySource`, no direct `StartActivity` in Application/Endpoints.
- Residual (non-blocking, recorded honestly): the two Contracts read adapters (`GeographyDestinationScopeValidator`, `FulfillmentShippingMethodOptionValidator`) are **not** wrapped with `IModuleCallTracer`. They are in-process Contracts-only reads on the already-traced MediatR span/correlation context, with no competing or lost propagation; the same shape exists in the certified Fulfillment module. This is residual observability debt, not a continuity regression, and it is **not** claimed as decoration.

## 6. Durable guards and promotion

Added (Shipping-scoped, test-only): `Certification/ShippingRequestClassificationTests.cs` — 9 tests covering route/request/handler/validator set equality, ISender dispatch from the real handler body, the complete Application request universe, nested-validator wiring, real DI validator discovery + `ValidationBehavior` presence, representative valid/invalid/omitted inputs, trace naming, and negative detection of an unclassified route/request.

Modified: `Composition/ShippingModuleStructureGuardTests.cs` — only the W2 lifecycle-stage assertion was replaced by `Manifest_entry_is_certified_with_the_shipping_certification_lineage` (certified state, `lockVersion`, `COMPLETE_REFERENCE_PATTERN`, authority task, evidence path, W2 structure lineage, exactly one entry). All root/dependency/namespace/solution/cohesion assertions are unchanged and green.

Promoted: the single Shipping manifest entry → `structureCertified: true`, `certificationState: COMPLETE_REFERENCE_PATTERN`, `certificationAuthorityTask: …W3`, evidence path; SoT block `shippingArchComplete002Structure` added and `Shipping` added once to `structureLock.certifiedModules`. No unrelated module state rewritten.

## 7. Focused validation

`dotnet test src/backend/Modules/Shipping/Tooba.Shipping.Tests --artifacts-path .tmp-w3-artifacts`

- Guard subset (structure + certification): 15/15 passed.
- Final whole-module Shipping suite: **391 passed, 0 failed, 0 skipped** (382 pre-existing + 9 new).
- Isolated task-owned artifacts (`.tmp-w3-artifacts`, removed after the run); shared `bin`/`obj` and running apps untouched; no browser/DB/container/package install.

## 8. Known limitations (not certifiable facts)

- D1–D7 product requirements (owner-scoped single admin control panel, marketplace seller authoring, region groups with multiple condition/price rows, configured-language UI, payment/carrier/free modes, cutoff, shipping-type taxonomy) remain **unimplemented**; this verdict is architecture only.
- The `Absent-envelope` note: `RuleFor(x => x.Request).NotNull()` carries the FluentValidation default code, but that path is unreachable over HTTP because the endpoint body reader converts absent/unreadable JSON into `shipping.validation.body_invalid` before dispatch. No wire contract depends on the default code.
- In-process DI/tests are not deployment, browser E2E or integrated customer acceptance proof.

## Verdict

`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` — **STRUCTURE_CERTIFIED** for the existing Shipping backend baseline. No whole-product acceptance, release conclusion or feature activation follows. `STOP_FOR_ARCHITECT`.

---

## ARCHITECT REJECTION — SUPERSEDED BY TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1

Appended by TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1. Everything above is preserved verbatim as the
historical W3 output and is **not** re-written; the following ruling overrides its verdict and its
section 6 "residual" classification.

- The Architect rejected the W3 verdict. Section 10 of the Certify skill requires *"cross-module calls
  use IModuleCallTracer decoration and trace continuity is not broken"*. ModuleCallTracer's own
  contract is explicitly for in-process cross-module calls, so a shared MediatR parent span does
  **not** supply the missing module-call child span.
- A similar omission in a sibling certified module is **not** exception authority. There is no
  approved exemption for this edge.
- Consequently the sentence *"Residual (non-blocking, recorded honestly) … residual observability
  debt, not a continuity regression"* is **withdrawn**. It is a blocking certification violation, not
  residual debt.
- Outcome: W3's promotion is superseded. structureCertified = true in
  docs/architecture/tmar-module-structure-manifests.json and Shipping's membership in
  structureLock.certifiedModules now cite TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1 as the
  corrective authority; this file remains the historical (rejected) report.
- Corrective evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1/RESULT.md.
- Product requirements D1–D7, integrated Admin runtime acceptance and release acceptance remain
  pending irrespective of the repair.
