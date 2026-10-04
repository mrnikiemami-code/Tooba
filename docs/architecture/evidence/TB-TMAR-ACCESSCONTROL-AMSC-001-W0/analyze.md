# TB-TMAR-ACCESSCONTROL-AMSC-001-W0 — Analyze (AMSC Wave 0)

- Task: `TB-TMAR-ACCESSCONTROL-AMSC-001-W0`
- Mode: `ANALYSIS_ONLY`
- Skill: `.cursor/skills/tooba-architecture-analyze/SKILL.md`
- Target: `src/backend/Modules/AccessControl/Tooba.AccessControl.*`
- Branch: `main`
- HEAD at analysis start: `eea29fb871ca21c5461420074356023bf7fbeb54` (== `origin/main`)
- Goal of the AMSC run: drive `AccessControl` to a professional, Visual-Studio-standard,
  zero-foreign-coupling surface that can be extracted as an independent microservice.
- Structure handoff owner: `.cursor/skills/tooba-architecture-structure/SKILL.md`

## Scope and bounded recovery unit

This analysis is deliberately **bounded to the AccessControl module surface**. The module is
already `structureCertified: true` under `ARCH-COMPLETE-002` in
`docs/architecture/tmar-module-structure-manifests.json`, with a large accepted disposition
history (`accessControlArchComplete002Structure`, `accessControlModuleAmc001W1..W5Cert`,
`accessControlModuleAmc002`, `accessControlModuleAmc002W1`, `accessControlModuleAmc002W2Cert`,
`accessControlStructureRepair001`, `accessControlStructureRecert001`).

Therefore this wave does **not** reopen ownership or coupling — those are already ZERO and
previously certified. It performs a fresh, current-disk audit to find the remaining
**cohesion / canonical-mechanism** debt that a certificate issued before the module was
re-examined must not hide, and it re-verifies every previously certified invariant against the
working tree at the current HEAD.

## Canonical mechanism discovery (performed before judging)

| Concern | Canonical mechanism discovered in repository | Reference |
| --- | --- | --- |
| Result / expected failure | `Tooba.BuildingBlocks.Results.Result` / `Result<T>` + `SemanticError` | `Tooba.BuildingBlocks` |
| API response mapping | `Tooba.BuildingBlocks.Presentation.ApiResponseFactory` | Offer, AccessControl |
| Error catalog | `IErrorCatalogContributor` + `ErrorDescriptor` + `IErrorDefinitionCatalog` + `SafeErrorMapper` | Offer, AccessControl |
| Localization | `IErrorResourceSet` + `.resx` / `.resx.fa` + `IErrorMessageLocalizer` | Offer, AccessControl |
| Stable codes | `Tooba.<Module>.Contracts.Errors.<Module>ErrorCodes` | Offer, AccessControl |
| Validation codes | `Tooba.<Module>.Application.Validators.<Module>ValidationCodes` | AccessControl |
| CQRS | `ToobaCqrsRegistration.AddToobaCqrsFoundation` (MediatR 12.5.0) | `TmarFoundation.cs` |
| Logging | `ILogger<T>` + `ObservabilityLogScope` | BuildingBlocks |
| Tracing / correlation | `ToobaTelemetry`, `IModuleCallTracer`, `ICorrelationIdProvider` (`X-Correlation-Id`) | BuildingBlocks |
| Clock / ids | `IClock` / `SystemUtcClock`, `IIdGenerator` / `UuidV7IdGenerator` (registered by `AddToobaCqrsFoundation`) | `TmarFoundation.cs`, Offer `Adapters/OfferDevelopmentSeedGateway.cs` |
| Typed cross-module fault | `Tooba.BuildingBlocks.ContractOperationException` (stable `.Code`) | Payment, Content, Inventory, Order |
| Size / cohesion guard | `TmarSourceSizeGuard` + `Baselines/tmar-source-size-baseline.json` | `Tooba.Host.Tests` |

**Two canonical mechanisms for expected failures exist and both are in active use:**

1. `ContractOperationException` — cross-module / directory boundary fault carrying a stable `.Code`,
   consumed by `catch (ContractOperationException ex) when (ex.Code == ...)`.
2. `AccessControlException` — the module's own stable-code exception, mapped once into
   `Result.Failure<T>(new SemanticError(ex.Code))` by `AccessControlOperation.ExecuteAsync`.

`AccessControlException` is therefore **not** a parallel invention; it is the module-local
analogue of the canonical typed-fault exception. Its duplication with `ContractOperationException`
is reported as a *cohesion/consolidation opportunity*, not as an illegal mechanism.

## Structured state fields

1. **Foundation-State** — `FOUNDATION_READY` (5 projects present, `structureCertified: true`).
2. **Ownership-State** — `correct` for all five layers.
3. **File-Cohesion-State** — `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (one file:
   `Application/Models/AccessControlDtos.cs`, mixed 8-type bundle). All other production files
   are `COHESIVE`.
4. **Oversized/God-File-State** — `AccessControlDirectory.cs` 877 LOC (`OVERSIZED_ONLY`,
   single cohesive responsibility, already baselined as `OVERSIZED_LEGACY` at 957 LOC).
   No `MULTI_RESPONSIBILITY_COHESION_VIOLATION` god-file.
5. **Localization-State** — `CANONICAL` (no hard-coded user-facing text; Persian appears only in
   XML-doc comments and `.resx` values).
6. **API-Result-Pattern-State** — `CANONICAL` (`api.From(...)`, `api.FromFailure(...)` only).
7. **Stable-Error-Code-State** — `CATALOGUED`, with one defect:
   `AccessControlDirectory.cs` emits 20 **string literals** instead of the
   `AccessControlErrorCodes.*` constants owned by the module's own Contracts.
8. **Logging-State** — `CANONICAL` (no `Console.WriteLine` / `Debug.WriteLine`, no second pipeline).
9. **Sensitive-Logging-State** — `NONE`.
10. **OpenTelemetry-State** — `CANONICAL`.
11. **Correlation-Trace-State** — `CANONICAL`.
12. **CQRS-State** — `COMPLIANT` (20 requests, 20 real handlers, 60 `ISender` dispatch sites).
13. **Validator-Coverage-State** — `EXHAUSTIVE` (6 `VALIDATOR_REQUIRED` + 14
    `NO_VALIDATOR_REQUIRED`, guarded by `AccessControlValidatorTests`).
14. **Contracts-Boundary-State** — `CLEAN` (module-boundary DTOs/ports/events/enums/errors only).
15. **Cross-Module-Coupling-State** — `LEGAL_CONTRACTS_ONLY`
    (foreign references are `Identity.Contracts`, `OperatorProfile.Contracts`, `Catalog.Contracts`,
    `Party.Contracts` only; zero foreign Application/Infrastructure/Domain usings or csproj edges).
16. **Cross-Module-Join-State** — `NONE`.
17. **Persistence-Ownership-State** — `CORRECT` (one DbContext, module-owned migrations, no
    foreign DbSet/DbContext reach-through).
18. **Endpoint-Ownership-State** — `MODULE_OWNED` (Host `AccessControl/` folder no longer exists).
19. **Host-Residue-State** — ZERO illegal; the Host `AccessControl/` folder is gone and only
    legitimate composition remains (`Program.cs` calls
    `AddAccessControlEndpointPresentation` / `MapAccessControlModuleEndpoints`).
20. **Schema-Migration-State** — `UNCHANGED` (two migrations; no drift planned in this run).
21. **Behavior-Preservation-Risk** — `LOW` (planned changes are rename/split/constant-substitution
    only; no route, DTO shape, status code, error code or persistence semantics change).
22. **Canonical-Reference-Used** — Offer (CQRS/Contracts/Endpoints/error-catalog shape),
    BuildingBlocks (`ApiResponseFactory`, `Result`, `IClock`, `IIdGenerator`,
    `ContractOperationException`).
23. **Final-Disposition** — `READY_TO_MIGRATE`.

## Findings

### F1 — `Application/Models/AccessControlDtos.cs` is a mixed responsibility bundle (blocker for W1)

One file declares eight unrelated top-level types with different reasons to change:

| Type | Responsibility | Target capability |
| --- | --- | --- |
| `AccessOwnerScope` | internal scope value used by the directory port | `Models/` (shared, Application-internal) |
| `CreateAccessRoleCommand`, `UpdateAccessRoleCommand`, `CloneAccessRoleCommand` | **transport request bodies** posted by the Endpoints layer | `Roles/Models/` |
| `RolePermissionGrant` | grant envelope used by `SetRolePermissionsCommand` and the port | `Permissions/Models/` |
| `AccessRoleDto` | role read model | `Roles/Models/` |
| `UserRoleAssignmentDto` | assignment read model | `Assignments/Models/` |
| `SellerCeilingEntryDto` | ceiling read model | `Ceiling/Models/` |
| `EffectivePermissionDto`, `EffectiveAccessDto` | effective-access read models | `Access/Models/` |
| `AccessUserHitDto` | user search read model | `Access/Models/` |

The three `*AccessRoleCommand` records are the clearest smell: they are **command-shaped** types
sitting in `Models` while their authoritative MediatR requests (`CreateRoleCommand`,
`UpdateRoleCommand`, `CloneRoleCommand`) live in `Roles/Commands/`. They are not duplicates of the
MediatR requests (they are HTTP bodies), but their name collides with the CQRS vocabulary and their
placement in a shared `Models` dump is exactly the anti-pattern the Migrate/Certify skills forbid.
They must be renamed to transport-body semantics and co-located with the Roles capability.

### F2 — `AccessControlDirectory.cs` emits raw string literals instead of owned error constants

20 stable codes are written inline (`"access.role.code_conflict"`, `"access.scope.unsupported"`, …)
while the module already owns the typed constants in
`Tooba.AccessControl.Contracts.Errors.AccessControlErrorCodes`. The file also imports
`Tooba.AccessControl.Contracts.Enums` twice (lines 8 and 9).

This is a **duplicate source of truth** for a module-boundary vocabulary. A typo in a literal would
silently bypass the composed `IErrorDefinitionCatalog` and surface as an unmapped error.

### F3 — `Application/Exceptions/AccessControlException.cs` is a single-file `Exceptions/` folder

A 14-LOC file occupying its own top-level Application folder. It is the module's typed-fault
vocabulary and belongs next to the other cross-capability primitives. It is also duplicative in
shape with `BuildingBlocks.ContractOperationException`.

### F4 — One pre-existing stale durable guard (environment-sensitive, NOT an AccessControl defect)

`AccessControlModuleAmcW1SolutionGuardTests.AccessControl_projects_are_grouped_under_Modules_AccessControl_including_Endpoints`
fails with `ArgumentOutOfRangeException` because it assumes a flat `<Folder Name="/Modules/">`
window exists in `src/backend/Tooba.slnx`. The canonical `/Modules/AccessControl/` grouping is
present and correct; the guard's own parsing assumption is stale.

This is recorded for W1 to repair (guard correctness, **not** guard weakening: the same assertions
are preserved with a correct parser).

### F5 — Test-project parity gap (out of scope, recorded only)

`AccessControl` has no `Tooba.AccessControl.Tests` project. Its behaviour and architecture guards
live in `Tooba.Host.Tests`. Many sibling modules (Cart, Fulfillment, Offer, Order, Payment,
Settlement, Wallet, …) do have a module test project. Creating one would be a new-project scope
expansion and is **not** part of this AMSC run.

### F6 — Worktree/environment observation (not a module defect)

`TmarSourceSizeAndInfraAppTests` fails at this HEAD because the repository root contains a stale
sibling git worktree `.tmp-baseline` (detached at `87a22d7c`, ancestor of `main`, clean except one
`.csproj` modification). `TmarSourceSizeGuard.ScanHandWrittenSources` walks `.tmp-baseline` (its
excluded-directory list contains `tmp` and `.tmp` but not `.tmp-baseline`) and therefore reports the
**snapshot's** files as new oversized files and reports the snapshot as a second
Infrastructure→foreign-Application edge source.

Evidence that this is environment-only: `docs/architecture/tmar-current-state.json` line ~2492
already records the same reproduction ("sibling `.tmp-baseline` worktree is scanned; reproduced at
clean HEAD"), and the failing violations are attributed to `.tmp-baseline/...` paths while the
in-tree `AccessControlDirectory.cs` is matched to its baseline entry (`BASELINE_ENTRY_MISSING_FILE`
for the pre-move path plus a `.tmp-baseline` copy). The worktree contains no commit that is not
already in `main`, so removing the worktree registration destroys no work.

## Illegal dependencies

**None.** Zero foreign `*.Application`, `*.Infrastructure`, `*.Domain` references; zero foreign
`DbContext`/`DbSet`; zero cross-module SQL/EF joins; zero Host business/persistence authority.

## Cross-module join inventory

**None.** `AccessControlDirectory.cs` joins only its own `AccessRole`, `UserRoleAssignment`,
`RolePermission`, `PlatformSellerCeiling` and `AccessAuditEvent` sets. Category/Brand/Product
resolution goes through the narrow `Tooba.Catalog.Contracts.Ports.IAccessControlScopeResourceLookup`
port implemented by Catalog (`CatalogAccessControlScopeResourceLookup`).

## Contracts-only replacement map

No replacement is required. The existing narrow boundaries already satisfy the rule:

- `Contracts/Access/AccessControlEffectiveAccessContracts.cs` — `IAccessControlEffectiveAccessReader`,
  `EffectiveAccess`, `EffectivePermission`, `AccessOwnerScope`.
- `Contracts/Readiness/AuthorizationReadinessContracts.cs` — `IAuthorizationReadinessProbe`.
- `Contracts/Development/AccessControlDevelopmentSeedContracts.cs` — development prelude port.
- `Contracts/Enums/` — `AccessOwnerScopeKind`, `AccessScopeKind`.
- `Contracts/Errors/AccessControlErrorCodes.cs` — module-owned stable codes.

## Target paths for W1

```text
Application/
  Models/AccessOwnerScope.cs                                  (shared Application-internal value)
  Roles/Models/AccessRoleDto.cs
  Roles/Models/CreateAccessRoleBody.cs
  Roles/Models/UpdateAccessRoleBody.cs
  Roles/Models/CloneAccessRoleBody.cs
  Assignments/Models/UserRoleAssignmentDto.cs
  Permissions/Models/RolePermissionGrant.cs
  Ceiling/Models/SellerCeilingEntryDto.cs
  Access/Models/EffectiveAccessDto.cs
  Access/Models/EffectivePermissionDto.cs
  Access/Models/AccessUserHitDto.cs
  Validation/AccessControlValidationCodes.cs                  (merge of Validators + Exceptions)
  Validation/AccessControlFluentRules.cs
  Validation/AccessControlException.cs
```

## Migration order

1. Split `Application/Models/AccessControlDtos.cs` into capability-cohesive `Models` files
   (behaviour-preserving rename of the three transport bodies).
2. Merge `Validators/` + `Exceptions/` into a single `Validation/` folder
   (`AccessControlValidationCodes`, `AccessControlFluentRules`, `AccessControlException`).
3. Replace 20 raw code literals in `AccessControlDirectory.cs` with
   `AccessControlErrorCodes.*` constants; de-duplicate the `using` directives.
4. Repair the stale `AccessControlModuleAmcW1SolutionGuardTests` parser without weakening its
   assertions.
5. Update `tmar-module-structure-manifests.json` forbidden-root lists for the new filenames.
6. Focused build + focused guards + AccessControl behaviour tests; commit and push W1.

## Verification plan

- `dotnet build src/backend/Tooba.slnx` (0 errors).
- `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~AccessControl` (all green, except the
  environment-only `TmarSourceSizeAndInfraAppTests` which is separately classified).
- `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~TmarCompleteReferenceStructureGateTests`.
- `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~TmarDurableGuardTests`.
- Post-change re-scan for raw `access.*` literals, `using Tooba.AccessControl.Domain` in Endpoints,
  `Results.Json` in Endpoints, foreign Application/Infrastructure/Domain usings.

## Certification blockers (to be closed in W1–W3)

1. F1 mixed `Models` bundle.
2. F2 raw error-code literals.
3. F3 single-file `Exceptions/` folder.
4. F4 stale solution guard parser.

F5 and F6 are recorded as out-of-scope observations, not AccessControl certification blockers.
