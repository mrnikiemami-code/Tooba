// TB-TMAR-STORECONTEXT-AMSC-001-W3 — append the module-local recovery checkpoint to the Master Recovery
// document. Purely additive: the existing bytes are preserved exactly (the file mixes CRLF and LF line
// endings, so the tail anchor is used verbatim) and only a new trailing section is written.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'TOOBA-TMAR-MASTER-RECOVERY.md');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('StoreContext AMSC module recovery checkpoint')) {
  throw new Error('StoreContext recovery checkpoint already present — refusing to overwrite');
}

const tailAnchor = 'evidence `docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W3-R1/verification.md`; '
  + 'own commit SHA reported in the Bridge Result only (Architect reconciles the final SHA separately).\n';
if (original.split(tailAnchor).length !== 2 || !original.endsWith(tailAnchor)) {
  throw new Error('Master Recovery tail anchor not found exactly once at the end of the file');
}

const body = [
  'StoreContext AMSC module recovery checkpoint (authoritative, module-local)',
  '',
  'Recorded by `TB-TMAR-STORECONTEXT-AMSC-001-W3` (`tooba-architecture-certify`), the fourth and final wave of the AMSC re-standardization of `src/backend/Modules/StoreContext` over the structure authority of W2. This is the first ARCH-COMPLETE-002 certification for StoreContext; the earlier foundation/golden lineage (`TB-TMAR-STORECONTEXT-FOUNDATION-001`, `TB-TMAR-STORECONTEXT-GOLDEN-001`) stays in the repository as historical evidence only and is not the current module authority.',
  '- Accepted lineage: `TB-TMAR-STORECONTEXT-AMSC-001-W0` Analyze `c73545f5` -> `TB-TMAR-STORECONTEXT-AMSC-001-W1` Migrate `d8abe38a` -> `TB-TMAR-STORECONTEXT-AMSC-001-W2` Structure `452855fa` -> `TB-TMAR-STORECONTEXT-AMSC-001-W3` Certify *(this commit, reported in the Bridge Result only; the Architect reconciles the final SHA separately)*. Each wave\'s recorded `startingHead` equals its parent wave\'s commit, so the chain is verifiable end to end.',
  '- Final verdict: `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` `STRUCTURE_CERTIFIED`; final `structureState = CERTIFIED` (W2 `structureState = READY_FOR_CERTIFY` preserved as historical W2 truth).',
  '- Applicability: `INTERNAL_ONLY` platform-context module — `httpApplicability = INTERNAL_ONLY`, `endpointOwnership = NOT_APPLICABLE`, `cqrs = NOT_APPLICABLE_NO_APPLICATION_USE_CASE` (no `Application` project, no MediatR), no `Tooba.StoreContext.Endpoints` project by design and no Host `StoreContext` folder. It therefore owns zero routes and zero endpoint-reachable requests, so the validator matrix is `NOT_APPLICABLE_INTERNAL_ONLY` by construction (vacuous set-equality gate: 0 routes, 0 requests, 0 required validators) and no ceremonial validator, empty route group or unused CQRS tree was added. The module\'s real responsibility — a scoped `StoreCommerceContext` (Market, DefaultCurrency, SalesChannel) provider/assigner exposed through `ICurrentStoreCommerceContext`, `IStoreCommerceContextAssigner` and `IWorkerStoreCommerceContextFactory` — is unchanged and its lifetimes are unchanged.',
  '- Canonical mechanisms: the module declares and emits zero error codes, so its localization state is `CANONICAL_NO_OWNED_ERROR_CODES_NO_RESOURCE_SET_BY_DESIGN` and it owns no `.resx` pair; claiming the consumer-owned `cart.commerce.*` or `platform.*` keyspaces here would create duplicate descriptor ownership, which the canonical rule forbids. `apiResultPatternState = CANONICAL_NO_HTTP_SURFACE` (no HTTP surface exists), and logging/correlation are `CANONICAL` with zero ad-hoc logging, zero `ILogger`, zero `ActivitySource`, zero `AsyncLocal`, zero `HttpContext` dependency and zero `ex.Message` classification.',
  '- Structure: capability-first shallow `Contracts/Current` + `Infrastructure/{Current,DependencyInjection}`; W2 moved the module composition entry `StoreContextModule.cs` from the Infrastructure project root to `Infrastructure/DependencyInjection/` with the path-derived namespace `Tooba.StoreContext.Infrastructure.DependencyInjection`, matching the newest ARCH-COMPLETE-002 certified precedent, so both project roots now carry an empty (justified) `rootAllowlist` with `StoreContextModule.cs` and `StoreCommerceContextAccessor.cs` forbidden at the root; path<->namespace `EXACT` (0 mismatches over 3 production `.cs`); `/Modules/StoreContext/` solution grouping (2 projects); no over-foldering, no empty capability folder, no stale duplicate copy and no god file (max 32 LOC including documentation). The accepted Persian code documentation standard (`docs/architecture/32-persian-code-documentation-standard.md`) is satisfied for every public member.',
  '- Microservice extractability: `ARCHITECTURAL_READY`. `Tooba.StoreContext.Contracts` references the foundation (`Tooba.BuildingBlocks`) only; `Tooba.StoreContext.Infrastructure` references `Tooba.StoreContext.Contracts` + `Tooba.ModuleContracts`; foreign module Application/Infrastructure/Domain/Endpoints project edge `ZERO`; cross-module persistence and cross-module join `NONE`; namespace alias / `TypeForwardedTo` workaround `NONE`; the module never references `Tooba.Host`. The only Host seams are the composition-root line in `ToobaModuleComposition.cs` and the worker-side `IWorkerStoreCommerceContextFactory` adapter, both of which are composition concerns rather than module dependencies.',
  '- Host closure PRESERVED: no `Host/Tooba.Host/StoreContext` folder, no `StoreContext*.cs` under Host, no `MapStoreContext` mapper; `hostIllegalAuthorityState = ZERO`.',
  '- Durable guards: new `StoreContextModuleAmsc001W3CertGuardTests` (9 facts) locks the SoT/manifest certification truth with the chained wave lineage, the `INTERNAL_ONLY` vacuous validator matrix, the W2 structure/cohesion verdict on disk, the Contracts-only microservice-extraction boundary, the absence of any parallel result/localization/logging/telemetry mechanism, the preserved Host closure and the four-wave evidence tree; `StoreContextModuleAmsc001W2StructureGuardTests` (9 facts) and `HostCartResidualGuardTests` continue to lock the structure and the Cart residual boundary.',
  '- Focused validation: `dotnet build src/backend/Tooba.slnx` 0 errors; `StoreContextModuleAmsc001W2StructureGuardTests` 9/9 passed; `StoreContextModuleAmsc001W3CertGuardTests` 9/9 passed; the `StoreContext` + `HostCartResidual` focused filter 24/24 passed.',
  '- This wave changed **no production file**; guards weakened NONE; baselines widened NONE; no migration, schema or frontend file was touched.',
  '- Global recovery lock preserved exactly: `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`, `lastAcceptedCommit = 7a6c353a98a761df9124beb1fce23ed8424230de`, repository-global `workflowStop = USER_REVIEW_HOST_ROOT_FINAL_CERT_001`, `automaticNextImplementationTask = NONE`.',
  '- Evidence root: `docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W0..W3/`.',
  '- Stop gate: `USER_REVIEW_STORECONTEXT_AMSC_001_W3`.',
  '- `automaticNextImplementationTask = NONE`.',
  '',
];

const addition = NL + body.join(NL);
const content = original + addition;

if (!content.startsWith(original)) {
  throw new Error('existing Master Recovery bytes were not preserved');
}
if (content.length <= original.length) {
  throw new Error('no change produced');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (StoreContext certification checkpoint appended)');
