// TB-TMAR-STORECONTEXT-AMSC-001-W0 — persist the Analyze wave checkpoint into the durable TMAR SoT.
// Additive only: appends the storeContextAmsc001W0 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"storeContextAmsc001W0"')) {
  throw new Error('storeContextAmsc001W0 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-STORECONTEXT-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-analyze',
  target: 'src/backend/Modules/StoreContext/Tooba.StoreContext.*',
  startingHead: 'e7131dc05a9fe5a310972cee9064cfffbe8c3630',
  parentTask: null,
  state: 'ANALYZE_COMPLETE',
  verdict: 'READY_TO_MIGRATE',
  lockVersion: 'ARCH-COMPLETE-002',
  priorCertificationState: 'PLATFORM_CONTEXT_REFERENCE_PATTERN_INTERNAL_ONLY_STRUCTURE_CERTIFIED_NOT_AMSC_CERTIFIED',
  httpApplicability: 'INTERNAL_ONLY',
  endpointOwnershipState: 'NOT_APPLICABLE',
  cqrsState: 'NOT_APPLICABLE_NO_APPLICATION_USE_CASE',
  endpointReachableRequests: 0,
  validatorCoverageState: 'NOT_APPLICABLE_INTERNAL_ONLY_ZERO_ENDPOINT_REACHABLE_REQUESTS',
  foundationState: 'FOUNDATION_READY',
  ownershipState: 'correct',
  fileCohesionState: 'COHESIVE',
  oversizedGodFileState: 'NONE_MAX_55_LOC',
  productionProjects: 2,
  productionFileCount: 3,
  localizationState: 'CANONICAL_NO_USER_FACING_TEXT_NO_OWNED_ERROR_CODES',
  apiResultPatternState: 'CANONICAL_NO_HTTP_SURFACE',
  stableErrorCodeState: 'NOT_APPLICABLE_NO_OWN_CODES',
  loggingState: 'CANONICAL',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  contractsBoundaryState: 'CLEAN',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY_INBOUND_CONSUMERS_ONLY',
  crossModuleContractsReference: 'Consumers only: Host (IStoreCommerceContextAssigner, IWorkerStoreCommerceContextFactory, StoreCommerceContext) and Cart (ICurrentStoreCommerceContext, IWorkerStoreCommerceContextFactory). StoreContext itself references no foreign module; its only project edges are Tooba.BuildingBlocks and Tooba.ModuleContracts.',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'NOT_APPLICABLE_NO_PERSISTENCE',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_ONLY_NO_HOST_STORECONTEXT_FOLDER',
  schemaMigrationState: 'UNCHANGED',
  behaviorPreservationRisk: 'LOW',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/StoreContext/',
  solutionProjectEntries: 2,
  pathNamespaceState: 'EXACT',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  singleFileRequestLeafState: 'ZERO',
  technicalAxisFirstState: 'ZERO',
  structureHandoffState: 'REQUIRED',
  canonicalReferenceUsed: 'BuildingBlocks for SemanticException/SemanticError consumed by Host+Cart; Inventory/CustomerProfile/AddressBook for the Infrastructure/DependencyInjection composition-folder precedent; docs/architecture/32-persian-code-documentation-standard.md for the W1 documentation repair.',
  blockers: [
    'DOCUMENTATION_STANDARD_GAP: every public StoreContext member carries English XML while the Architect-accepted docs/architecture/32-persian-code-documentation-standard.md requires strong professional Persian documentation. W1 closes this with a documentation-only, behavior-preserving change.',
    'MANIFEST_JUSTIFICATION_GAP: the StoreContext manifest entry has empty rootAllowlistJustification on both projects and carries no certificationNote recording the AMSC lineage or the INTERNAL_ONLY rationale, unlike every other certified module.',
    'COMPOSITION_ENTRY_PLACEMENT_OPEN: the repository standard permits the module composition entry at the Infrastructure root, while the newest certified modules place *Module.cs under Infrastructure/DependencyInjection/. W2 must decide and, if it moves the file, update the allowlist, the two HostCartResidualGuardTests root-file facts and the manifest atomically.',
    'NO_AMSC_SOT_RECORD: StoreContext has only the historical storeContext block and the golden evidence under docs/evidence/; no AMSC W0-W3 SoT records, no Master Recovery AMSC checkpoint, no AMSC evidence tree.',
    'NO_MODULE_SCOPED_AMSC_GUARD: current StoreContext protections live in shared Host guard files (HostCartResidualGuardTests, HostConfigurationAmcW1GuardTests, HostConfigurationAmcCertGuardTests); W2/W3 add module-scoped durable guards.'
  ],
  noArchitectureDecisionRequired: true,
  hostTouched: false,
  productionCodeChanged: false,
  commit: 'PENDING_W0_COMMIT',
  waveOutcome: 'MIGRATE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W0/analyze.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storeContextAmsc001W0": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}
const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w0 = parsed.storeContextAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.verdict !== 'READY_TO_MIGRATE') {
  throw new Error('storeContextAmsc001W0 record missing or wrong state');
}

const storeContext = parsed.storeContext;
if (!storeContext || storeContext.state !== 'PLATFORM_CONTEXT_REFERENCE_PATTERN') {
  throw new Error('historical storeContext block was disturbed');
}
if (storeContext.httpApplicability !== 'INTERNAL_ONLY'
  || storeContext.endpointOwnership !== 'NOT_APPLICABLE'
  || storeContext.cqrs !== 'NOT_APPLICABLE_NO_APPLICATION_USE_CASE'
  || storeContext.structureCertifiedUnderArchComplete002 !== true) {
  throw new Error('historical storeContext applicability fields were disturbed');
}

const certified = parsed.structureLock.certifiedModules;
if (certified.filter((m) => m === 'StoreContext').length !== 1) {
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
if (!content.startsWith(original.slice(0, lastIndex))) {
  throw new Error('prefix of the original file was not preserved');
}
if (!content.endsWith('}' + NL)) {
  throw new Error('unexpected trailing bytes');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: storeContextAmsc001W0)');
