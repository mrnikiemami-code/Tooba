// TB-TMAR-STORECONTEXT-AMSC-001-W2 — persist the Structure wave checkpoint into the durable TMAR SoT.
// Additive only: appends the storeContextAmsc001W2 record with a text-anchor insertion so the existing
// file formatting (2-space record indent, CRLF) is preserved and no other record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const W1_COMMIT = 'd8abe38af8920bb1ac0417270e2de5b8756a104d';
const W2_STARTING_HEAD = W1_COMMIT;

if (original.includes('"storeContextAmsc001W2"')) {
  throw new Error('storeContextAmsc001W2 already present — refusing to overwrite');
}
if (!original.includes('"storeContextAmsc001W1"')) {
  throw new Error('storeContextAmsc001W1 missing — W1 SoT record must exist first');
}

const record = {
  task: 'TB-TMAR-STORECONTEXT-AMSC-001-W2',
  parentTask: 'TB-TMAR-STORECONTEXT-AMSC-001-W1',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-structure',
  target: 'src/backend/Modules/StoreContext/Tooba.StoreContext.*',
  startingHead: W2_STARTING_HEAD,
  state: 'STRUCTURE_COMPLETE',
  verdict: 'STRUCTURE_READY_FOR_CERTIFY',
  lockVersion: 'ARCH-COMPLETE-002',
  structureState: 'READY_FOR_CERTIFY',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  capabilityLayout: 'Contracts/Current + Infrastructure/{Current,DependencyInjection}',
  pathNamespaceState: 'EXACT_ZERO_MISMATCHES',
  rootAllowlistState: 'EMPTY_ROOT_ON_BOTH_PROJECTS',
  overFolderingState: 'NONE',
  singleFileRequestLeafState: 'NOT_APPLICABLE_NO_APPLICATION_NO_ENDPOINTS',
  staleDuplicateCopyState: 'NONE',
  godFileState: 'NONE_MAX_32_LOC_INCLUDING_DOCUMENTATION',
  solutionGroupingState: 'CANONICAL_MODULES_STORECONTEXT_FOLDER_TWO_PROJECTS',
  endpointsProjectState: 'ABSENT_BY_DESIGN_INTERNAL_ONLY',
  applicationProjectState: 'ABSENT_BY_DESIGN_NO_APPLICATION_USE_CASE',
  domainProjectState: 'ABSENT_BY_DESIGN_NO_DOMAIN_MODEL',
  technicalAxisFirstState: 'NONE',
  structureChange: 'StoreContextModule.cs moved from the Tooba.StoreContext.Infrastructure project root to Tooba.StoreContext.Infrastructure/DependencyInjection/ so the module composition entry matches the newest ARCH-COMPLETE-002 certified precedent (Inventory, CustomerProfile, AddressBook, Pricing, Promotion).',
  structureChangeRationale: 'ARCH-COMPLETE-002 permits a composition entry at the Infrastructure root, but the newest certified modules converge on Infrastructure/DependencyInjection/*Module.cs. Aligning StoreContext now removes a per-module special case before AMSC certification and keeps the root allowlist empty, which is the stronger, self-evident invariant.',
  namespacesChanged: [
    'Tooba.StoreContext.Infrastructure -> Tooba.StoreContext.Infrastructure.DependencyInjection (StoreContextModule only)'
  ],
  filesMoved: [
    'Tooba.StoreContext.Infrastructure/StoreContextModule.cs -> Tooba.StoreContext.Infrastructure/DependencyInjection/StoreContextModule.cs'
  ],
  filesDeleted: [],
  filesCreated: [],
  filesModified: [
    'src/backend/Modules/StoreContext/Tooba.StoreContext.Infrastructure/DependencyInjection/StoreContextModule.cs',
    'src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs',
    'src/backend/Host/Tooba.Host.Tests/Architecture/HostCartResidualGuardTests.cs',
    'docs/architecture/tmar-module-structure-manifests.json'
  ],
  publicApiChanged: 'NONE',
  diRegistrationChanged: 'NONE_SAME_TYPES_SAME_LIFETIMES_SAME_ORDER',
  projectEdgesChanged: 'NONE',
  compositionEdgeProof: 'ToobaModuleComposition.cs only changed its using directive from Tooba.StoreContext.Infrastructure to Tooba.StoreContext.Infrastructure.DependencyInjection; the module instance list, its position and every registration inside StoreContextModule.cs are byte-identical.',
  behaviorPreservationProof: 'Code-token hash equality after stripping /// lines, blank lines and the byte-order mark: StoreContextModule.cs pre-move 20 code lines B9FBE86326EFF86F vs post-move 20 code lines AA79F3BDA89EA222; the only two differing code lines are the leading BOM line (formatting) and the namespace declaration line, which is exactly the intended path<->namespace alignment. Contracts/Current/StoreCommerceContext.cs and Infrastructure/Current/StoreCommerceContextAccessor.cs are byte-identical to W1.',
  preservedInvariants: [
    'StoreContext registers exactly one scoped StoreCommerceContextAccessor exposed through ICurrentStoreCommerceContext, IStoreCommerceContextAssigner and IWorkerStoreCommerceContextFactory with unchanged lifetimes.',
    'DefaultCurrency remains a default-selection input only: never the currency of a transaction/line/order/settlement/payment-group and never a single-currency Cart/Order invariant.',
    'A null StoreCommerceContext still means unresolved and the consumer still fails closed.',
    'The accessor keeps no static mutable state, depends on no HttpContext and uses no AsyncLocal: lifetime is exactly one DI scope (request or worker cycle).',
    'StoreContext remains INTERNAL_ONLY with no HTTP route, no application use case, no domain model, no persistence and no Host folder.'
  ],
  contractsBoundaryState: 'CLEAN',
  illegalReferencesState: 'ZERO',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY_INBOUND_CONSUMERS_ONLY',
  crossModuleJoinState: 'NONE',
  foreignLayerReferenceState: 'ZERO',
  namespaceAliasWorkaroundState: 'NONE',
  persistenceState: 'NOT_APPLICABLE_NO_PERSISTENCE',
  cqrsState: 'NOT_APPLICABLE_NO_APPLICATION_USE_CASE',
  endpointOwnershipState: 'NOT_APPLICABLE',
  validatorCoverageState: 'NOT_APPLICABLE_INTERNAL_ONLY_ZERO_ENDPOINT_REACHABLE_REQUESTS',
  localizationState: 'CANONICAL_NO_OWNED_ERROR_CODES_NO_RESOURCE_SET_BY_DESIGN',
  apiResultPatternState: 'CANONICAL_NO_HTTP_SURFACE',
  loggingState: 'CANONICAL_NO_AD_HOC_LOGGING_NO_TELEMETRY_MECHANISM',
  fileCohesionState: 'COHESIVE',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_ONLY',
  hostFilesChanged: 2,
  schemaMigrationState: 'UNCHANGED',
  manifestState: 'CERTIFIED_RECORD_ROOT_ALLOWLISTS_EMPTY_WITH_JUSTIFICATIONS_AND_FORBIDDEN_ROOTS',
  durableGuard: 'Tooba.Host.Tests/Architecture/StoreContextModuleAmsc001W2StructureGuardTests.cs (9 facts)',
  verification: {
    hostTestsBuild: 'succeeded (0 errors, 166 pre-existing analyzer warnings)',
    structureGuard: '9/9 passed',
    storeContextAndCartResidualGuard: '24/24 passed',
    pathNamespaceAudit: '0 mismatches',
    fullSolutionBuild: 'succeeded'
  },
  structureHandoffState: 'READY_FOR_W3_CERTIFY',
  structureHandoffDetail: 'W3 (tooba-architecture-certify) must add certificationNote to the manifest StoreContext entry, record the AMSC lineage (W0 c73545f5 -> W1 d8abe38a -> W2 this commit -> W3), keep the durable W2 structure guard, and certify the microservice-extraction readiness claim.',
  waveOutcome: 'CERTIFY_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W2/structure.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storeContextAmsc001W2": ${line}` : `  ${line}`))
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
if (!w0 || w0.state !== 'ANALYZE_COMPLETE') {
  throw new Error('storeContextAmsc001W0 record missing or disturbed');
}
const w1 = parsed.storeContextAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.startingHead !== 'c73545f547d970745cbcb0e5c9fc634aff3c96ae') {
  throw new Error('storeContextAmsc001W1 record missing or disturbed');
}
const w2 = parsed.storeContextAmsc001W2;
if (!w2 || w2.state !== 'STRUCTURE_COMPLETE' || w2.verdict !== 'STRUCTURE_READY_FOR_CERTIFY') {
  throw new Error('storeContextAmsc001W2 record missing or wrong state');
}
if (w2.startingHead !== W1_COMMIT) {
  throw new Error('W2 startingHead must equal the W1 commit');
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: storeContextAmsc001W2)');
