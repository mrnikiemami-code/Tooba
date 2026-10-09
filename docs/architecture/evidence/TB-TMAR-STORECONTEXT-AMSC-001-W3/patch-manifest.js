// TB-TMAR-STORECONTEXT-AMSC-001-W3 — add the certificationNote to the manifest StoreContext entry.
// Text-anchor insertion: only the certificationNote line is added between lockVersion and projects, so
// the existing manifest formatting (6-space module-property indent, CRLF) is preserved byte for byte.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"module": "StoreContext",' + NL + '      "structureCertified": true,' + NL
  + '      "lockVersion": "ARCH-COMPLETE-002",' + NL + '      "certificationNote"')) {
  throw new Error('StoreContext certificationNote already present — refusing to overwrite');
}

const certificationNote = 'TB-TMAR-STORECONTEXT-AMSC-001-W3 (tooba-architecture-certify) certified StoreContext under '
  + 'ARCH-COMPLETE-002: verdict COMPLETE_REFERENCE_PATTERN, structureState CERTIFIED, structureCertified true. '
  + 'INTERNAL_ONLY platform-context module: httpApplicability INTERNAL_ONLY, endpointOwnership NOT_APPLICABLE, '
  + 'cqrs NOT_APPLICABLE_NO_APPLICATION_USE_CASE (no Application project, no MediatR), no Endpoints project by design '
  + 'and no Host StoreContext folder, therefore zero endpoint-reachable requests and the validator matrix is '
  + 'NOT_APPLICABLE_INTERNAL_ONLY by construction (vacuous set-equality gate: 0 routes, 0 requests, 0 required '
  + 'validators) - no ceremonial validator was added. Wave lineage W0 c73545f5 -> W1 d8abe38a -> W2 452855fa -> W3 '
  + 'this commit. Structure: capability-first shallow Contracts/Current + Infrastructure/{Current,DependencyInjection}; '
  + 'the module composition entry was moved from the Infrastructure project root to DependencyInjection/ in W2, so both '
  + 'project roots have an empty rootAllowlist (justified) with StoreContextModule.cs and '
  + 'StoreCommerceContextAccessor.cs forbidden at the root; path<->namespace EXACT (0 mismatches over 3 production .cs); '
  + '/Modules/StoreContext/ solution grouping (2 projects); no over-foldering, no empty capability folder, no god file '
  + '(max 32 LOC including documentation) and no stale duplicate. Canonical mechanisms: the scoped '
  + 'StoreCommerceContextAccessor is exposed through ICurrentStoreCommerceContext / IStoreCommerceContextAssigner / '
  + 'IWorkerStoreCommerceContextFactory with unchanged lifetimes; localization '
  + 'CANONICAL_NO_OWNED_ERROR_CODES_NO_RESOURCE_SET_BY_DESIGN (StoreContext declares and emits zero error codes, so '
  + 'claiming the consumer-owned cart.commerce.* or platform.* keyspaces here would create duplicate descriptor '
  + 'ownership); apiResultPattern CANONICAL_NO_HTTP_SURFACE; logging CANONICAL with no ad-hoc logging, no ILogger, no '
  + 'ActivitySource, no ex.Message classification and no Results.*/ProblemDetails. Boundaries: Contracts references the '
  + 'foundation (Tooba.BuildingBlocks) only; Infrastructure references Contracts + ModuleContracts; foreign module '
  + 'Application/Infrastructure/Domain/Endpoints edge ZERO; cross-module persistence and join NONE; namespace '
  + 'alias/type-forwarding workaround NONE. Microservice extractability ARCHITECTURAL_READY: the only Host seam is the '
  + 'composition-root line plus the worker-side IWorkerStoreCommerceContextFactory adapter, and the module never '
  + 'references Tooba.Host. The Persian code documentation standard '
  + '(docs/architecture/32-persian-code-documentation-standard.md) is satisfied for every public member. Durable guards: '
  + 'StoreContextModuleAmsc001W2StructureGuardTests and StoreContextModuleAmsc001W3CertGuardTests. Focused validation: '
  + 'StoreContext + HostCartResidual filter 24/24 passed; StoreContextModuleAmsc001W2StructureGuardTests 9/9 passed; '
  + 'full solution build 0 errors. This wave changed no production file; guards weakened NONE; baselines widened NONE; '
  + 'the repository-global Host root checkpoint (lastAcceptedTask TB-TMAR-HOST-ROOT-FINAL-CERT-001, '
  + 'currentHostCheckpoint HOST_ROOT_FINAL_CERTIFIED, workflowStop USER_REVIEW_HOST_ROOT_FINAL_CERT_001, '
  + 'automaticNextImplementationTask NONE) is preserved exactly. Stop gate USER_REVIEW_STORECONTEXT_AMSC_001_W3. '
  + 'Evidence docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W3/certification.md.';

if (certificationNote.includes('"') || certificationNote.includes('\\')) {
  throw new Error('certificationNote must not contain a quote or backslash');
}

const anchor = '      "module": "StoreContext",' + NL
  + '      "structureCertified": true,' + NL
  + '      "lockVersion": "ARCH-COMPLETE-002",' + NL
  + '      "projects": [';

if (original.split(anchor).length !== 2) {
  throw new Error('StoreContext manifest anchor not found exactly once');
}

const content = original.replace(anchor, '      "module": "StoreContext",' + NL
  + '      "structureCertified": true,' + NL
  + '      "lockVersion": "ARCH-COMPLETE-002",' + NL
  + `      "certificationNote": "${certificationNote}",` + NL
  + '      "projects": [');

// --- validate: additive only, everything else untouched -------------------
const parsed = JSON.parse(content);
const entries = parsed.modules.filter((m) => m.module === 'StoreContext');
if (entries.length !== 1) {
  throw new Error('expected exactly one StoreContext manifest entry');
}
const entry = entries[0];
if (entry.structureCertified !== true || entry.lockVersion !== 'ARCH-COMPLETE-002') {
  throw new Error('StoreContext certification flags disturbed');
}
if (!entry.certificationNote || !entry.certificationNote.includes('TB-TMAR-STORECONTEXT-AMSC-001-W3')) {
  throw new Error('StoreContext certificationNote missing');
}
if (entry.projects.length !== 2) {
  throw new Error('StoreContext project count changed');
}
for (const project of entry.projects) {
  if (!Array.isArray(project.rootAllowlist) || project.rootAllowlist.length !== 0) {
    throw new Error(`${project.projectName} rootAllowlist is not empty`);
  }
  if (!project.rootAllowlistJustification) {
    throw new Error(`${project.projectName} rootAllowlistJustification missing`);
  }
}
const infra = entry.projects.find((p) => p.projectName === 'Tooba.StoreContext.Infrastructure');
if (!infra.forbiddenRootFiles.includes('StoreContextModule.cs')
  || !infra.forbiddenRootFiles.includes('StoreCommerceContextAccessor.cs')) {
  throw new Error('Infrastructure forbiddenRootFiles disturbed');
}
if (parsed.modules.length !== 29) {
  throw new Error(`manifest module count changed: ${parsed.modules.length}`);
}
if (parsed.preCertModules.length !== 0) {
  throw new Error('preCertModules is not empty');
}
if (JSON.parse(original).modules.length !== 29) {
  throw new Error('original module count unexpected');
}
if (content.split('"certificationNote"').length !== original.split('"certificationNote"').length + 1) {
  throw new Error('exactly one certificationNote must have been added');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-module-structure-manifests.json (StoreContext certificationNote added)');
