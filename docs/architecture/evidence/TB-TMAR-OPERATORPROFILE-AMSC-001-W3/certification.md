# TB-TMAR-OPERATORPROFILE-AMSC-001-W3 — Certification (tooba-architecture-certify)

## Verdict

`COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

Structure gate source: `TB-TMAR-OPERATORPROFILE-AMSC-001-W2` (commit `14b16690`, `Structure-State = READY_FOR_CERTIFY`, current, same surface, durable guard green).

## Touched-surface recheck

W1 touched `OperatorProfileErrorCodes.cs`, `OperatorProfileOperation.cs`, `UpsertOperatorProfileCommand.cs`, `GetOperatorProfileQuery.cs` — re-read post-change: cohesive single responsibilities, correct capability folders, exact path↔namespace, no hard-coded user-facing text, no foreign leakage, no parallel mechanism, no duplicate request shape, no schema/behavior change. PASS.

## 1. Physical tree / root allowlists

25 production files across 5 projects; all roots match manifest allowlists exactly (Application/Domain/Contracts root empty; Infrastructure root `OperatorProfileModule.cs`; Endpoints root `OperatorProfileEndpointModule.cs`); zero forbidden resurrection (incl. retired `OperatorProfileContracts.cs`); zero empty ceremonial folders.

## 2. Path↔namespace / alias / copies

`EXACT` across all production projects (EF `Persistence/Migrations` artifacts follow the repo-wide EF exemption); zero `TypeForwardedTo`; zero alias workaround; one authoritative physical home per responsibility.

## 3. File cohesion

Largest file 136 LOC (`Domain/Aggregates/OperatorProfile.cs`, single aggregate). No file above any ceiling; no multi-responsibility god-file; no cosmetic over-split; request+handler co-location in one file per use case is the module's accepted single-file axis layout (zero per-use-case leaf folders — files live directly on `Admin/{Commands,Queries,Validators}` axes).

## 4. Endpoint ownership — HTTP_OWNING, MODULE_ENDPOINTS

Route count: **2**, both module-owned: `GET /v1/admin/operator/profile`, `PUT /v1/admin/operator/profile` (mapped via `MapOperatorProfileModuleEndpoints`). Host route count ZERO; Host `OperatorProfile` folder absent (HOST_ZERO preserved by `HostOperatorProfileAmcGuardTests`). No duplicate mapping.

## 5–6. CQRS + validator matrix (exhaustive)

| Request | Kind | Handler | Validator | Classification |
|---|---|---|---|---|
| `GetOperatorProfileQuery` | `IRequest<Result<OperatorProfileAdminResponse>>` | `GetOperatorProfileQueryHandler` | `GetOperatorProfileQueryValidator` (ActorUserId transport shape) | VALIDATOR_REQUIRED_PRESENT |
| `UpsertOperatorProfileCommand` | `IRequest<Result<OperatorProfileAdminResponse>>` | `UpsertOperatorProfileCommandHandler` | `UpsertOperatorProfileCommandValidator` (ActorUserId/DisplayName/optional parts transport shapes, codes `operator.profile.validation.*`) | VALIDATOR_REQUIRED_PRESENT |

2/2 required present, 0 `NO_VALIDATOR_REQUIRED` → `EXHAUSTIVE`. MediatR 12.5.0 via `AddToobaCqrsFoundation` (module assembly registered in `Program.cs`); endpoints are `ISender` + `ApiResponseFactory` only; no endpoint persistence/directory call; no custom dispatcher; no Host bypass.

## 7. Localization

`OperatorProfileErrorResourceSet` (`IErrorResourceSet`) owns the `operator.profile.` keyspace; bilingual pair `OperatorProfileErrors.resx` / `.fa.resx` carries all 6 keys × 2 cultures; messages resolve via the canonical `IErrorMessageLocalizer` inside `ApiResponseFactory`; no hard-coded Persian/English API text in production code; no `ex.Message` contract; no endpoint-level `Accept-Language` parsing.

## 8. API result / error mapping

All endpoints use `ApiResponseFactory.From(Result<T>)`; zero `Results.Json/BadRequest/Problem`; classification by typed code only (dual-mechanism seam with `IsKnown` filter); unknown codes/exceptions propagate untouched to the global boundary; each of the 6 module codes resolves to exactly one `ErrorDescriptor` (single `OperatorProfileErrorCatalogContributor`, no duplicate ownership, no suppression); success shape = raw DTO (shipped contract).

## 9–10. Logging / observability / correlation

`ILogger<T>` structured templates only (`operator.profile.upsert.succeeded`, `{OperatorProfileUpsertEvent}` code-carrying failure field, `operator.profile.get.*`); zero sensitive data; no second ActivitySource/Meter/correlation provider; no direct `StartActivity`; no manual `traceparent`; ProblemDetails trace/correlation via canonical `ProblemDetailsContextProvider`. Zero outbound cross-module calls exist → `IModuleCallTracer` decoration not applicable (recorded, not a gap).

## 11–12. Boundaries + persistence

Zero foreign `*.Application|Infrastructure|Domain|Endpoints` project edges in all 5 projects (regex-proven + csproj-audited). Foreign consumers (`Order.Application`, `AccessControl.Application`, `Catalog.Endpoints`, `ProductWorkspace.Endpoints`) reach only `Contracts.Ports.IActorDisplayLookup` (registered as `ActorDisplayLookupAdapter` by the module). Zero cross-module join; single `OperatorProfileDbContext` on own `operator_profile` schema (+ own Outbox table); module-owned migration `20260827215300_InitialOperatorProfile` unchanged; 0 migration files touched in all waves.

## 13. Host authority

`ALLOWED_COMPOSITION_ROOT` (Program.cs usings/CQRS registration/authorizer DI/endpoint map ×4) + `ALLOWED_SECURITY_ADAPTER` (thin `HostOperatorProfileAdminAuthorizer`) + allowed dev-seed invocation (`SettingsFoundationDevelopmentSeedHost`) + migration descriptor. All ILLEGAL categories ZERO. Host final closure `PRESERVED`; closed-folder regression `NONE` (no destination outside the active module surface was touched).

## 14. Migration safety

Schema, migration id, Up/Down, snapshot, tables/columns/indexes/constraints: unchanged across W0→W3.

## 15–16. Durable guards + manifest

Durable guards (all green): `OperatorProfileModuleAmsc001W3CertGuardTests` (new, 5 facts), `OperatorProfileModuleAmsc001W2StructureGuardTests` (5), `OperatorProfileModuleAmsc001W1MigrateGuardTests` (3), `OperatorProfileModuleAmcW4CertGuardTests` (4), `HostOperatorProfileAmcGuardTests` (3). Manifest: exactly one certified OperatorProfile entry, `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`, certification note promoted to the AMSC-001 W3 lineage.

## 17. Recovery SoT

`operatorProfileAmsc001W3` block records the full verdict; `operatorProfileAmsc001W0/W1/W2` lineage blocks preserved; global Host root checkpoint untouched. Prior `operatorProfileAmc001` (AMC W4) block retained as historical superseded lineage (not rewritten).

## 18. Focused validation

- `dotnet build` Tooba.OperatorProfile.Endpoints = 0 errors / 0 warnings; `dotnet build` Tooba.Host = 0 errors.
- `--filter OperatorProfileModuleAmsc001W3` = 5/5 PASS; OperatorProfile + structure-gate family PASS (Catalog Contracts namespace debt remains the single disclosed pre-existing red — Catalog-owned, module-local scope, not weakened or repaired here).

## Residual non-blocking debt

- No dedicated `Tooba.OperatorProfile.Tests` project (module has no behavior tests of its own; module-local durable guards live in `Tooba.Host.Tests` per repo pattern). Non-blocking; not introduced by this pipeline.
- Persian doc-comments + Development seed demo text are not API message contracts (repo idiom).

## Certification result

**CERTIFIED** — `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` / `microserviceExtractable: true`. Stop gate `USER_REVIEW_OPERATORPROFILE_AMSC_001_W3`.
