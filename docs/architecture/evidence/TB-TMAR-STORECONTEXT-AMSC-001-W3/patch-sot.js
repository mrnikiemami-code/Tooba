// TB-TMAR-STORECONTEXT-AMSC-001-W3 — persist the Certify wave checkpoint into the durable TMAR SoT.
// Additive only: appends the storeContextAmsc001W3 record with a text-anchor insertion so the existing
// file formatting (2-space record indent, CRLF) is preserved and no other record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const W0_COMMIT = 'c73545f547d970745cbcb0e5c9fc634aff3c96ae';
const W1_COMMIT = 'd8abe38af8920bb1ac0417270e2de5b8756a104d';
const W2_COMMIT = '452855fa2379080aa8ca9f8a93509f947901fa79';
const W3_STARTING_HEAD = W2_COMMIT;

if (original.includes('"storeContextAmsc001W3"')) {
  throw new Error('storeContextAmsc001W3 already present — refusing to overwrite');
}
for (const wave of ['W0', 'W1', 'W2']) {
  if (!original.includes(`"storeContextAmsc001${wave}"`)) {
    throw new Error(`storeContextAmsc001${wave} missing — earlier waves must land first`);
  }
}

const record = {
  task: 'TB-TMAR-STORECONTEXT-AMSC-001-W3',
  parentTask: 'TB-TMAR-STORECONTEXT-AMSC-001-W2',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-certify',
  target: 'src/backend/Modules/StoreContext/Tooba.StoreContext.*',
  startingHead: W3_STARTING_HEAD,
  state: 'STORECONTEXT_AMSC_001_CERTIFIED',
  verdict: 'COMPLETE_REFERENCE_PATTERN',
  lockVersion: 'ARCH-COMPLETE-002',
  structureState: 'CERTIFIED',
  structureCertified: true,
  applicabilityClassification: 'INTERNAL_ONLY',
  httpApplicability: 'INTERNAL_ONLY',
  endpointOwnershipState: 'NOT_APPLICABLE',
  cqrsState: 'NOT_APPLICABLE_NO_APPLICATION_USE_CASE',
  endpointReachableRequests: 0,
  validatorCoverageState: 'NOT_APPLICABLE_INTERNAL_ONLY_VACUOUS_SET_EQUALITY',
  validatorMatrix: '0_VALIDATOR_REQUIRED_0_NO_VALIDATOR_REQUIRED_NO_ENDPOINT_REACHABLE_REQUEST_EXISTS',
  ceremonialSurfaceState: 'NONE_NO_FAKE_ENDPOINTS_NO_EMPTY_MAPGROUP_NO_UNUSED_CQRS_TREE',
  waveLineage: 'W0 c73545f5 -> W1 d8abe38a -> W2 452855fa -> W3 this commit',
  productionFileCount: 3,
  productionProjectCount: 2,
  pathNamespaceState: 'EXACT',
  pathNamespaceMismatches: 0,
  rootAllowlistState: 'ENFORCED_EMPTY_ROOT_ON_BOTH_PROJECTS',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  capabilityLayout: 'Contracts/Current + Infrastructure/{Current,DependencyInjection}',
  solutionGroupingState: 'CANONICAL_MODULES_STORECONTEXT_FOLDER_TWO_PROJECTS',
  godFileState: 'NONE_MAX_32_LOC_INCLUDING_DOCUMENTATION',
  staleDuplicateCopyState: 'NONE',
  physicalCopyState: 'CLEAN',
  fileCohesionState: 'COHESIVE',
  contractsBoundaryState: 'CLEAN_CONTRACTS_ONLY',
  foreignModuleLayerCoupling: 'ZERO',
  crossModuleJoinState: 'NONE',
  namespaceAliasWorkaroundState: 'NONE',
  typeForwardingWorkaroundState: 'NONE',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY_INBOUND_CONSUMERS_ONLY',
  persistenceState: 'NOT_APPLICABLE_NO_PERSISTENCE',
  schemaMigrationState: 'NONE',
  localizationState: 'CANONICAL_NO_OWNED_ERROR_CODES_NO_RESOURCE_SET_BY_DESIGN',
  localizationRationale: 'StoreContext declares and emits zero error codes, so there is nothing to catalogue and no .resx pair is owned. Claiming the consumer-owned cart.commerce.* or platform.* keyspaces here would create duplicate descriptor ownership, which the canonical rule forbids.',
  apiResultPatternState: 'CANONICAL_NO_HTTP_SURFACE',
  loggingState: 'CANONICAL',
  adHocLoggingState: 'NONE',
  sensitiveLoggingState: 'NONE',
  correlationState: 'CANONICAL',
  parallelCorrelationState: 'NONE',
  hostAuthorityState: 'ALLOWED_COMPOSITION_ROOT_AND_WORKER_FACTORY_ADAPTER_ONLY',
  hostIllegalAuthorityState: 'ZERO',
  hostFinalClosureState: 'PRESERVED',
  globalHostCheckpointState: 'PRESERVED',
  microserviceExtractable: true,
  microserviceExtractabilityDetail: 'Contracts references the foundation only; Infrastructure references Contracts + ModuleContracts; foreign module Application/Infrastructure/Domain/Endpoints edge ZERO; cross-module persistence and join NONE; no namespace alias or type-forwarding workaround; the module never references Tooba.Host, so extraction needs only a Contracts-shaped inbound seam plus the worker-side factory adapter.',
  persianDocumentationState: 'SATISFIED_PER_32_PERSIAN_CODE_DOCUMENTATION_STANDARD',
  behaviorChangeThisWave: 'NONE_NO_PRODUCTION_FILE_CHANGED',
  guardsAdded: 'StoreContextModuleAmsc001W3CertGuardTests (9 facts)',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  manifestState: 'CERTIFIED_RECORD_WITH_CERTIFICATION_NOTE_AND_EMPTY_JUSTIFIED_ROOT_ALLOWLISTS',
  focusedValidation: 'dotnet build src/backend/Tooba.slnx -> 0 errors. StoreContextModuleAmsc001W2StructureGuardTests 9/9 passed; StoreContextModuleAmsc001W3CertGuardTests 9/9 passed; StoreContext + HostCartResidual focused filter 24/24 passed.',
  blockingResidualDebt: 'ZERO',
  nonBlockingResidualDebt: 'NONE',
  stopGate: 'USER_REVIEW_STORECONTEXT_AMSC_001_W3',
  automaticNextImplementationTask: 'NONE',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W3/certification.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storeContextAmsc001W3": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}

const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

if (!content.startsWith(original.slice(0, lastIndex))) {
  throw new Error('prefix of the original file was not preserved');
}
if (!content.endsWith('}' + NL)) {
  throw new Error('unexpected trailing bytes');
}

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w0 = parsed.storeContextAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.commit !== W0_COMMIT) {
  throw new Error('storeContextAmsc001W0 record missing or disturbed');
}
const w1 = parsed.storeContextAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.startingHead !== W0_COMMIT) {
  throw new Error('storeContextAmsc001W1 record missing or disturbed');
}
const w2 = parsed.storeContextAmsc001W2;
if (!w2 || w2.state !== 'STRUCTURE_COMPLETE' || w2.startingHead !== W1_COMMIT) {
  throw new Error('storeContextAmsc001W2 record missing or disturbed');
}
const w3 = parsed.storeContextAmsc001W3;
if (!w3 || w3.state !== 'STORECONTEXT_AMSC_001_CERTIFIED' || w3.verdict !== 'COMPLETE_REFERENCE_PATTERN') {
  throw new Error('storeContextAmsc001W3 record missing or wrong state');
}
if (w3.startingHead !== W2_COMMIT || w3.lockVersion !== 'ARCH-COMPLETE-002'
  || w3.structureState !== 'CERTIFIED' || w3.structureCertified !== true) {
  throw new Error('storeContextAmsc001W3 certification fields wrong');
}

const storeContext = parsed.storeContext;
if (!storeContext || storeContext.state !== 'PLATFORM_CONTEXT_REFERENCE_PATTERN'
  || storeContext.httpApplicability !== 'INTERNAL_ONLY'
  || storeContext.endpointOwnership !== 'NOT_APPLICABLE'
  || storeContext.cqrs !== 'NOT_APPLICABLE_NO_APPLICATION_USE_CASE'
  || storeContext.structureCertifiedUnderArchComplete002 !== true) {
  throw new Error('historical storeContext block was disturbed');
}

if (parsed.structureLock.certifiedModules.filter((m) => m === 'StoreContext').length !== 1) {
  throw new Error('structureLock.certifiedModules must contain StoreContext exactly once');
}
if (parsed.completeReferenceModules.length !== 12) {
  throw new Error('completeReferenceModules length changed');
}

for (const pointer of [
  'lastAcceptedTask',
  'lastAcceptedCommit',
  'latestAcceptedImplementationWave',
  'currentHostCheckpoint',
  'workflowStop',
  'automaticNextImplementationTask'
]) {
  if (!Object.prototype.hasOwnProperty.call(parsed, pointer)) {
    throw new Error(`global pointer ${pointer} missing`);
  }
}
if (parsed.lastAcceptedTask !== 'TB-TMAR-HOST-ROOT-FINAL-CERT-001'
  || parsed.currentHostCheckpoint !== 'HOST_ROOT_FINAL_CERTIFIED'
  || parsed.workflowStop !== 'USER_REVIEW_HOST_ROOT_FINAL_CERT_001'
  || parsed.automaticNextImplementationTask !== 'NONE') {
  throw new Error('repository-global Host root checkpoint was disturbed');
}

if (content === original) {
  throw new Error('no change produced');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: storeContextAmsc001W3)');
