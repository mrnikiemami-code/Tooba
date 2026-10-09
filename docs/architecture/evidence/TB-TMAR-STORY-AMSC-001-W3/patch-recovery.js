// TB-TMAR-STORY-AMSC-001-W3 — append the module-local recovery checkpoint to the Master Recovery document.
// Purely additive: the existing bytes are preserved exactly (the file mixes CRLF and LF line endings, so the
// tail anchor is used verbatim) and only a new trailing section is written.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'TOOBA-TMAR-MASTER-RECOVERY.md');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('Story AMSC module recovery checkpoint')) {
  throw new Error('Story recovery checkpoint already present — refusing to overwrite');
}

const tailAnchor = '- `automaticNextImplementationTask = NONE`.\r\n';
if (!original.endsWith(tailAnchor) || original.lastIndexOf(tailAnchor) !== original.length - tailAnchor.length) {
  throw new Error('Master Recovery tail anchor not found at the end of the file');
}

const body = [
  'Story AMSC module recovery checkpoint (authoritative, module-local)',
  '',
  'Recorded by `TB-TMAR-STORY-AMSC-001-W3` (`tooba-architecture-certify`), the fourth and final wave of the AMSC re-standardization of `src/backend/Modules/Story` over the structure authority of W2. The earlier AMC-001 lineage (`TB-TMAR-STORY-AMC-001` .. `TB-TMAR-STORY-AMC-001-W6-CERT`) stays in the repository as historical evidence only and is not the current module authority.',
  '- Accepted lineage: `TB-TMAR-STORY-AMSC-001-W0` Analyze `0c73390a3211e0ee9057e9234d62d3e4f14b5e4e` -> `TB-TMAR-STORY-AMSC-001-W1` Migrate `2a09e7bb7f1ab687435007951160b4bcfefb4c18` -> `TB-TMAR-STORY-AMSC-001-W2` Structure `4cd9a6cc543ccd307d775dfe703459de7b12c95d` -> `TB-TMAR-STORY-AMSC-001-W3` Certify *(this commit, reported in the Bridge Result only; the Architect reconciles the final SHA separately)*. Each wave\'s recorded `startingHead` equals its parent wave\'s commit, so the chain is verifiable end to end.',
  '- Final verdict: `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` `STRUCTURE_CERTIFIED`; final `structureState = CERTIFIED` (the W2 `READY_FOR_CERTIFY` verdict is preserved as historical W2 truth).',
  '- Applicability: `HTTP_OWNING` — 25 module-owned routes over `/v1/admin/stories` (15), `/v1/seller/stories` (9) and `/v1/storefront/stories` (1); Host-owned route count `ZERO`; `endpointOwnership = MODULE_OWNED`; CQRS is 25 real `IRequest`/`IRequestHandler` dispatched through `ISender` on MediatR 12.5; the validator matrix is exhaustive at `16 VALIDATOR_REQUIRED + 9 NO_VALIDATOR_REQUIRED` with 17 discoverable `AbstractValidator` classes and zero unmapped endpoint-reachable request.',
  '- Canonical mechanisms: `Result<T>` + `ApiResponseFactory` `From`/`Created` only with zero ad-hoc `Results.*`/`ProblemDetails` and zero message-text classification; all 10 `story.*` validation codes and all 5 stable error codes are bilingually resourced in `StoryErrors.resx` / `StoryErrors.fa.resx` with exactly one descriptor owner for the 5 stable codes and no first/last-wins suppression; logging is canonical with zero `ILogger`/`Console`/`Debug`; OpenTelemetry and correlation are canonical with no parallel correlation and no manual `traceparent` handling.',
  '- Structure: capability-first shallow `Stories` foldering; the shared fault seam sits at the shallow `Application/Composition/StoryOperation.cs`; the Infrastructure composition entry is `DependencyInjection/StoryModule.cs` with the outbox registration split to `Messaging/StoryOutboxRegistration.cs` and the directory capability at `Directories/StoryDirectory.cs`; four project roots are empty and `Tooba.Story.Endpoints` keeps the single allowlisted composition entry; path<->namespace `EXACT` (0 mismatches over 40 production `.cs`); `/Modules/Story/` solution group holds the five projects exactly once; no over-foldering, no use-case-named single-file request leaf, no technical-axis-first request tree, no stale duplicate and no god file (`StoryDirectory.cs` 672 LOC stays `WATCH`, single responsibility below the 800 ceiling).',
  '- Boundaries: `Tooba.Story.Contracts` holds error codes only with no Application contracts dump and no duplicate CQRS shape; foreign module `Application`/`Infrastructure`/`Domain`/`Endpoints` edge `ZERO`; cross-module persistence and cross-module join `NONE`; namespace alias / `TypeForwardedTo` workaround `NONE`; one `StoryDbContext` on schema `story` with the two accepted migrations unchanged and no migration regenerated.',
  '- Microservice extractability: `ARCHITECTURAL_READY`. `Contracts` references nothing; `Domain`/`Application` reference only `Contracts` + `Tooba.BuildingBlocks`; `Endpoints` references `Application` + `Contracts` + `BuildingBlocks`; `Infrastructure` references `Application` + `Contracts` + `Domain` + `Tooba.ModuleContracts` + `Tooba.Persistence`; the module never references `Tooba.Host`, so `/Modules/Story/` can be dropped into its own host behind a Contracts-shaped inbound seam.',
  '- Host closure PRESERVED: Host keeps only the composition root (`ToobaModuleComposition` + `MapStoryModuleEndpoints` + development seed/migrator lines) and the legitimate `Host/Security/Seller/HostStorySellerAuthorizer.cs` platform adapter; `src/backend/Host/Tooba.Host/Story` remains ABSENT; `hostIllegalAuthorityState = ZERO`.',
  '- Durable guards: new `StoryModuleAmsc001W3CertGuardTests` (9 facts) locks the SoT/manifest certification truth with the chained wave lineage, the `HTTP_OWNING` endpoint/CQRS/validator truth, the W2 structure/cohesion verdict on disk, the Contracts-only microservice-extraction boundary, the canonical result/localization/logging/telemetry mechanisms, the preserved Host closure and the four-wave evidence tree; `StoryModuleAmsc001W2StructureGuardTests` (7 facts) and `StoryModuleAmsc001W1MigrateGuardTests` continue to lock structure and migration.',
  '- Focused validation: `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj` 0 errors; `StoryModuleAmsc001W2StructureGuardTests` 7/7 passed; `StoryModuleAmsc001W3CertGuardTests` 9/9 passed; the Story AMSC + AMC + `HostStoryAmc` + `StoryFoundation` focused filter passes every Story-owned fact.',
  '- Declared pre-existing, unrelated red guards (not Story debt, deliberately not repaired by this module-local wave): `HostGridAmcR3GuardTests`, `HostGridAmcR4GuardTests`, `HostGridAmcR5R1GuardTests` (Catalog/Party/Reviews folder drift plus a repository-global `lastAcceptedTask` pin), the `TmarDurableGuardTests` and `TmarCompleteReferenceStructureGateTests` `certifiedModules` literal lists, and `TmarSourceSizeAndInfraAppTests` (stale git-ignored `.tmp-baseline` worktree). All were proven red against a clean worktree at the untouched W2 HEAD.',
  '- This wave changed **no production file**; guards weakened NONE; baselines widened NONE; no migration, schema or frontend file was touched.',
  '- Global recovery lock preserved exactly: `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`, `lastAcceptedCommit = 7a6c353a98a761df9124beb1fce23ed8424230de`, repository-global `workflowStop = USER_REVIEW_HOST_ROOT_FINAL_CERT_001`, `automaticNextImplementationTask = NONE`.',
  '- Certification promotion: `Story` is now listed once in `structureLock.certifiedModules` (29 entries).',
  '- Evidence root: `docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W0..W3/`.',
  '- Stop gate: `USER_REVIEW_STORY_AMSC_001_W3`.',
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
if (!content.endsWith('`automaticNextImplementationTask = NONE`.' + NL)) {
  throw new Error('unexpected trailing bytes');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (Story certification checkpoint appended)');
