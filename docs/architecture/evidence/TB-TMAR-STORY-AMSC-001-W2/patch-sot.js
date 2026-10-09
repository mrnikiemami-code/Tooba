// TB-TMAR-STORY-AMSC-001-W2 — persist the Structure wave checkpoint into the durable TMAR SoT.
// Additive only: stamps the W1 commit SHA into the storyAmsc001W1 record and appends the
// storyAmsc001W2 record before the closing brace, preserving the file's CRLF/2-space formatting.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const rawOriginal = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const W1_COMMIT = '2a09e7bb7f1ab687435007951160b4bcfefb4c18';

if (rawOriginal.includes('"storyAmsc001W2"')) {
  throw new Error('storyAmsc001W2 already present — refusing to overwrite');
}
if (!rawOriginal.includes('"storyAmsc001W1"')) {
  throw new Error('storyAmsc001W1 missing — W1 must land first');
}
if (!rawOriginal.includes('"commit": "PENDING_W1_COMMIT"')) {
  throw new Error('expected storyAmsc001W1 pending commit marker not found');
}
if ((rawOriginal.match(/"PENDING_W1_COMMIT"/g) || []).length !== 1) {
  throw new Error('storyAmsc001W1 pending commit marker is not unique');
}
const original = rawOriginal.replace('"commit": "PENDING_W1_COMMIT"', `"commit": "${W1_COMMIT}"`);

const record = {
  task: 'TB-TMAR-STORY-AMSC-001-W2',
  parentTask: 'TB-TMAR-STORY-AMSC-001-W1',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-structure',
  target: 'src/backend/Modules/Story/Tooba.Story.*',
  startingHead: W1_COMMIT,
  state: 'STRUCTURE_COMPLETE',
  verdict: 'READY_FOR_CERTIFY',
  lockVersion: 'ARCH-COMPLETE-002',
  applicabilityClassification: 'HTTP_OWNING',
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/Story/',
  solutionProjectEntries: 5,
  pathNamespaceState: 'EXACT',
  pathNamespaceMismatches: 0,
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED_EMPTY_ROOT_ON_ALL_FIVE_PROJECTS',
  singleFileRequestLeafState: 'ZERO',
  technicalAxisFirstState: 'ZERO',
  fileCohesionState: 'COHESIVE',
  godFileState: 'WATCH_ONLY_STORYDIRECTORY_672_LOC_SINGLE_RESPONSIBILITY_BELOW_800_CEILING',
  overFolderedState: 'NONE',
  overNestedState: 'NONE',
  changesApplied: [
    'Infrastructure/StoryModule.cs -> Infrastructure/DependencyInjection/StoryModule.cs (namespace Tooba.Story.Infrastructure -> Tooba.Story.Infrastructure.DependencyInjection; 21-module precedent)',
    'StoryOutboxRegistration split out of StoryModule.cs into Infrastructure/Messaging/StoryOutboxRegistration.cs (namespace Tooba.Story.Infrastructure.Messaging; Cart/Payment/Returns/Settlement/Fulfillment/Notification precedent)',
    'Infrastructure/Directory/ -> Infrastructure/Directories/ (namespace Tooba.Story.Infrastructure.Directory -> Tooba.Story.Infrastructure.Directories; 20-module precedent)',
    'Application/Stories/Composition/StoryOperation.cs -> Application/Composition/StoryOperation.cs (namespace Tooba.Story.Application.Stories.Composition -> Tooba.Story.Application.Composition; 21-module precedent)'
  ],
  filesMoved: 3,
  filesSplit: 1,
  filesDeleted: 0,
  behaviorChangeState: 'NONE_OBSERVABLE_API_SCHEMA_AND_ROUTES_UNCHANGED',
  schemaMigrationState: 'UNCHANGED_NO_MIGRATION_ADDED_REGENERATED_OR_EDITED',
  crossModuleCouplingState: 'NONE_UNCHANGED',
  crossModuleJoinState: 'NONE_UNCHANGED',
  contractsBoundaryState: 'CLEAN_CONTRACTS_ONLY_UNCHANGED',
  validatorEvidenceState: 'PRESERVED_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED_UNCHANGED_BY_MOVES',
  cqrsState: 'UNCHANGED_25_REAL_IREQUEST_IREQUESTHANDLER_ISENDER',
  hostTouched: true,
  hostChangeState: 'COMPOSITION_ONLY_ONE_USING_DIRECTIVE',
  hostChangeDetail: 'Host/Tooba.Host/Composition/ToobaModuleComposition.cs: using Tooba.Story.Infrastructure -> using Tooba.Story.Infrastructure.DependencyInjection. No Host business/persistence/endpoint authority was added; HOST_ROOT_FINAL_CERTIFIED is preserved.',
  manifestState: 'STORY_ENTRY_UPDATED_EMPTY_ROOT_ALLOWLISTS_AND_AMSC_001_CERTIFICATION_NOTE',
  manifestCertificationFlagState: 'UNCHANGED_structureCertified_true_lockVersion_ARCH_COMPLETE_002',
  guardsAdded: 'StoryModuleAmsc001W2StructureGuardTests (7 facts)',
  guardsUpdated: [
    'HostDevelopmentMigrationSeamGuardTests — Story composition root path updated',
    'HostGridAmcR2GuardTests — StoryModule path + IAdminStoryGridPort path updated',
    'HostGridAmcR5R1GuardTests — Reviews Directories path + removed a stale lastAcceptedTask pin',
    'AdminDbNativeGridQueryTests — StoryPresentationComposer path updated',
    'StoryModuleAmcW1GuardTests — StoryDirectory path updated',
    'StoryModuleAmcW3StructureGuardTests — new paths, empty Infrastructure root, exact namespaces',
    'StoryModuleAmcW4ResultGuardTests — StoryOperation path updated',
    'StoryModuleAmcW6CertGuardTests — new paths, empty Infrastructure root',
    'StoryFoundationTests — Infrastructure using directives updated'
  ],
  guardsWeakened: 'NONE_NO_ASSERTION_REMOVED_EXCEPT_ONE_STALE_GLOBAL_POINTER_PIN_IN_HostGridAmcR5R1GuardTests',
  staleGlobalPointerPinRemoval: 'HostGridAmcR5R1GuardTests asserted that the repository-global lastAcceptedTask equals TB-TMAR-HOST-GRID-AMC-001-R5-R1. That pointer has since legitimately advanced to TB-TMAR-HOST-ROOT-FINAL-CERT-001 and the assertion was already failing before this wave. The AMSC wave-local SoT keys remain asserted; the historical record hostGridAmcR5R1 and its workflowStop are untouched.',
  baselinesWidened: 'NONE',
  structureHandoffState: 'COMPLETE',
  microserviceExtractable: true,
  microserviceExtractabilityDetail: 'Unchanged and re-proved: the module references no foreign module at all; only Tooba.BuildingBlocks, Tooba.ModuleContracts, Tooba.Persistence.',
  focusedValidation: 'dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -> 0 errors; StoryModuleAmsc001W2StructureGuardTests + AMSC W1 + AMC W1/W3/W4/W5/W6 + HostStoryAmc + StoryFoundation + Host grid/seam/AdminDbNative focused filter -> all passed.',
  commit: 'PENDING_W2_COMMIT',
  waveOutcome: 'CERTIFY_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W2/structure.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storyAmsc001W2": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}
const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w0 = parsed.storyAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.commit !== '0c73390a3211e0ee9057e9234d62d3e4f14b5e4e') {
  throw new Error('storyAmsc001W0 record missing or disturbed');
}
const w1 = parsed.storyAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.commit !== W1_COMMIT) {
  throw new Error('storyAmsc001W1 record missing or disturbed');
}
const w2 = parsed.storyAmsc001W2;
if (!w2 || w2.state !== 'STRUCTURE_COMPLETE' || w2.startingHead !== W1_COMMIT
  || w2.verdict !== 'READY_FOR_CERTIFY') {
  throw new Error('storyAmsc001W2 record missing or wrong state');
}

for (const key of ['storyModuleAmc001', 'storyModuleAmc001W1', 'storyModuleAmc001W2Cert',
  'storyModuleAmc001W3', 'storyModuleAmc001W4', 'storyModuleAmc001W5', 'storyModuleAmc001W6Cert']) {
  if (!Object.prototype.hasOwnProperty.call(parsed, key)) {
    throw new Error(`historical AMC record ${key} was disturbed`);
  }
}
if (parsed.storyModuleAmc001W6Cert.structureCertified !== true
  || parsed.storyModuleAmc001W6Cert.architectureState !== 'COMPLETE_REFERENCE_PATTERN') {
  throw new Error('storyModuleAmc001W6Cert certification state was disturbed');
}
if (!parsed.storeContextAmsc001W3
  || parsed.storeContextAmsc001W3.state !== 'STORECONTEXT_AMSC_001_CERTIFIED') {
  throw new Error('storeContextAmsc001W3 record was disturbed');
}

const certified = parsed.structureLock.certifiedModules;
if (!Array.isArray(certified) || certified.length !== 28
  || certified.filter((m) => m === 'StoreContext').length !== 1
  || certified.includes('Story')) {
  throw new Error('structureLock.certifiedModules was disturbed');
}
if (parsed.structureLock.version !== 'ARCH-COMPLETE-002') {
  throw new Error('structureLock.version changed');
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: storyAmsc001W2)');
