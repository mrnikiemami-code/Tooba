# TB-TMAR-PAGECOMPOSITION-AMSC-001-W3 — Certify (tooba-architecture-certify)

## Scope

`src/backend/Modules/PageComposition/Tooba.PageComposition.*` — ARCH-COMPLETE-002 certification of the touched surface under the AMSC pipeline id, starting head `361a1837` (W2), parent `TB-TMAR-PAGECOMPOSITION-AMSC-001-W2`. Zero production change, zero schema change.

## Structure gate (mandatory precondition)

Source: `TB-TMAR-PAGECOMPOSITION-AMSC-001-W2` (commit `361a1837`, verdict `READY_FOR_CERTIFY`, current, same surface — no intervening structural commit). All gates hold: `PROFESSIONAL_SHALLOW` / `CANONICAL` / `EXACT` / `CLEAN` / `ENFORCED`. Re-checked as defense in depth by the new durable W3 guard.

## Certification checks

1. **Physical tree audit** — 30 production files across 5 projects; roots match manifest allowlists exactly (Application/Domain/Contracts empty; Infrastructure = module composition only; Endpoints = endpoint composition only). No root dump.
2. **Semantic Contracts + folder granularity** — Contracts carries only module-owned stable error codes + catalog contributor + resource set + bilingual resx (correct: the only cross-boundary semantics of this leaf module are its error codes; no foreign consumer exists). Application carries all CQRS requests/handlers/validators/models/ports; `AddHomeSectionCommand`/`UpdateHomeSectionCommand` in Models are nested input payloads carried by the authoritative MediatR requests — no duplicate request shape. Zero single-file use-case request leaves (machine-scanned, 0).
3. **File cohesion / no god-file** — largest `PageCompositionDirectory.cs` 233 LOC single persistence seam; `SectionCatalog.cs` 186 LOC cohesive domain catalog; `PageDefinition.cs` 166 LOC single aggregate; `PageCompositionAdminEndpoints.cs` 165 LOC transport-only over 7 routes. No ARCH-SIZE-001 entry required; no multi-responsibility file; no artificial over-split.
4. **Endpoint ownership** — 8 module-owned routes via `MapPageCompositionModuleEndpoints` (7 Admin under `/v1/admin/page-composition/home` + 1 Storefront `GET /v1/storefront/home/composition`); Host HTTP ownership ZERO; no duplicate mapping; admin authorizer module-owned in Endpoints.
5. **CQRS/MediatR** — 8 endpoint-reachable requests, all real `IRequest<Result<T>>` + real `IRequestHandler<,>`; thin endpoints dispatch through `ISender` only; zero endpoint→Directory/DbContext direct calls; MediatR 12.5.0 via `AddToobaCqrsFoundation` (assembly swept in `Program.cs`).
6. **Validator coverage** — `EXHAUSTIVE_8_OF_8_REQUIRED_PRESENT` (6 Admin validators + 2 Storefront validators; all stable `page-composition.*` codes via `WithErrorCode`; 0 NO_VALIDATOR_REQUIRED); discovered through `AddValidatorsFromAssembly` in the CQRS foundation.
7. **Localization** — canonical: `PageCompositionErrorResourceSet` owns the `page-composition.` keyspace; bilingual resx pair (8 keys × 2 cultures); descriptors carry `LocalizationKey = code` with `SafeTitleFallback`; no hard-coded user-facing API text in production code; no `Accept-Language` parsing (optional `locale` query param flows into module locale normalization only).
8. **API result/error mapping** — canonical `ApiResponseFactory.From`/`Created` only; zero raw `Results.Json/BadRequest/Problem`; zero `ex.Message` classification (guard-enforced); unknown exceptions propagate to the global boundary; the W1 dual-mechanism seam maps known typed faults by declared stable code only.
9. **Logging/sensitive data** — module has zero log call sites (nothing to weaken); no Console/Debug writers; no second telemetry pipeline; no secrets in any DTO/log path.
10. **OpenTelemetry/correlation** — canonical; no custom correlation, no direct `StartActivity`, no traceparent parsing; no outbound cross-module calls (so no `IModuleCallTracer` requirement).
11. **Cross-module boundary** — `NONE_SELF_CONTAINED`: zero foreign project references in csproj and zero foreign `using` in source, in both directions (no foreign consumer of PageComposition.Contracts exists — leaf composition module). Stronger than contracts-only: fully self-contained.
12. **Persistence ownership** — own `PageCompositionDbContext`, schema `page_composition`, own Outbox registration, own migrations; no foreign DbSet/schema/transaction; `ARCH-DATA-001` intact.
13. **Host authority** — `ALLOWED_COMPOSITION_ROOT_ONLY` (Program.cs: usings, `AddPageCompositionEndpointPresentation`, CQRS assembly sweep, `MapPageCompositionModuleEndpoints`; `ToobaModuleComposition`: module registration) + allowed dev-seed/migration invocation (2 Development bootstrappers). Zero ILLEGAL categories. Pinned by `HostPageCompositionAmcGuardTests`.
14. **Persistence/migration safety** — 0 migration files touched across W0–W3; schema unchanged; `ModuleSchemaMigrationOrder.PageComposition` untouched.
15. **Durable guards** — new W3 cert guard (5 facts) + W2 structure guard + W1 migrate guard + legacy `PageCompositionModuleAmcW2StructureGuardTests`/`PageCompositionModuleAmcW4CertGuardTests` + `HostPageCompositionAmcGuardTests` all green. No guard weakened.

## Manifest promotion

`tmar-module-structure-manifests.json` PageComposition entry: `structureCertified: true` and `lockVersion: ARCH-COMPLETE-002` were already true (AMC-001 W4); this wave rewrites the stale `certificationNote` to name the AMSC-001 lineage as the current certification authority (the AMC-001 W4 note is preserved verbatim in SoT history `pageCompositionAmc001`). Exactly one certified PageComposition entry exists.

## SoT record

`pageCompositionAmsc001W3` block appended: verdict `COMPLETE_REFERENCE_PATTERN`, full certified state, accepted lineage W0 `652036cc` → W1 `6a28c921` → W2 `361a1837` → W3 `PENDING_THIS_COMMIT` (real SHA recorded by the R1 recovery wave), stop gate `USER_REVIEW_PAGECOMPOSITION_AMSC_001_W3`, `automaticNextImplementationTask = NONE`.

## Focused validation

- Builds: PageComposition Endpoints chain 0 errors; Host.Tests 0 errors (one compile fix inside the new W2 guard itself: missing `using System.Text.Json;` plus leaf-scan heuristic corrected to key on use-case-named FOLDER names per the structure skill — shared technical axes legitimately contain request-named files).
- Family: **22/22 PASS** under the PageComposition family filter (`PageCompositionModuleAmsc001W3CertGuardTests` 5 facts + `PageCompositionModuleAmsc001W2StructureGuardTests` 5 facts + `PageCompositionModuleAmsc001W1MigrateGuardTests` 2 facts + legacy `PageCompositionModuleAmcW2StructureGuardTests` 2 facts + `PageCompositionModuleAmcW4CertGuardTests` 7 facts + `HostPageCompositionAmcGuardTests` 1 fact).
- `TmarCompleteReferenceStructureGateTests`: single disclosed pre-existing Catalog Contracts namespace debt red — Catalog-owned, out of PageComposition scope, unchanged.
