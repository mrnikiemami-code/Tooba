// TB-TMAR-STORY-AMSC-001-W3 — finalize the AMSC certification checkpoint in the durable TMAR SoT.
// Additive + truth-repair:
//   1) stamps storyAmsc001W2.commit (PENDING_W2_COMMIT -> 4cd9a6cc...) so the recorded wave lineage is
//      a real, verifiable ancestry chain;
//   2) appends the storyAmsc001W3 certification record before the closing brace;
//   3) appends "Story" to structureLock.certifiedModules (the certification promotion).
// The pre-existing global stale-pointer drift (certifiedModules vs TmarDurableGuardTests) is declared in
// the W3 evidence and deliberately NOT silently "fixed" here.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const rawOriginal = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const W0_COMMIT = '0c73390a3211e0ee9057e9234d62d3e4f14b5e4e';
const W1_COMMIT = '2a09e7bb7f1ab687435007951160b4bcfefb4c18';
const W2_COMMIT = '4cd9a6cc543ccd307d775dfe703459de7b12c95d';

if (rawOriginal.includes('"storyAmsc001W3"')) {
  throw new Error('storyAmsc001W3 already present — refusing to overwrite');
}
if ((rawOriginal.match(/"PENDING_W2_COMMIT"/g) || []).length !== 1) {
  throw new Error('expected exactly one PENDING_W2_COMMIT marker');
}
for (const wave of ['W0', 'W1', 'W2']) {
  if (!rawOriginal.includes(`"storyAmsc001${wave}"`)) {
    throw new Error(`storyAmsc001${wave} missing — earlier waves must land first`);
  }
}
if (rawOriginal.includes('"Story",' + NL + '      "Returns"')) {
  throw new Error('Story already present in certifiedModules');
}

// --- 1) stamp the W2 commit (real SHA) ------------------------------------
let original = rawOriginal.replace('"commit": "PENDING_W2_COMMIT"', `"commit": "${W2_COMMIT}"`);

// --- 2) promote Story into structureLock.certifiedModules -----------------
const promotedAnchor = '      "Promotion",' + NL + '      "Returns"' + NL + '    ],';
if (original.split(promotedAnchor).length !== 2) {
  throw new Error('certifiedModules tail anchor not found exactly once');
}
original = original.replace(promotedAnchor, '      "Promotion",' + NL + '      "Returns",' + NL + '      "Story"' + NL + '    ],');

// --- 3) append the W3 certification record --------------------------------
const record = {
  task: 'TB-TMAR-STORY-AMSC-001-W3',
  parentTask: 'TB-TMAR-STORY-AMSC-001-W2',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-certify',
  target: 'src/backend/Modules/Story/Tooba.Story.*',
  startingHead: W2_COMMIT,
  state: 'STORY_AMSC_001_CERTIFIED',
  verdict: 'COMPLETE_REFERENCE_PATTERN',
  lockVersion: 'ARCH-COMPLETE-002',
  structureState: 'CERTIFIED',
  structureCertified: true,
  applicabilityClassification: 'HTTP_OWNING',
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED',
  hostOwnedRouteCount: 0,
  endpointReachableRequests: 25,
  cqrsState: 'COMPLIANT_REAL_IREQUEST_IREQUESTHANDLER_ISENDER_MEDIATR_12_5',
  totalMediatRRequests: 25,
  validatorCoverageState: 'EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED',
  validatorMatrix: '25 endpoint-reachable requests; 16 VALIDATOR_REQUIRED with 17 discoverable AbstractValidator classes; 9 NO_VALIDATOR_REQUIRED carrying no malformable transport shape (Guid route ids, authorization-seam actor ids, bodyless transitions).',
  unmappedEndpointReachableRequests: 0,
  duplicateRequestShapeState: 'NONE',
  pathNamespaceState: 'EXACT',
  pathNamespaceMismatches: 0,
  rootAllowlistState: 'ENFORCED_EMPTY_ROOT_ON_FOUR_PROJECTS_SINGLE_ALLOWLISTED_ENDPOINTS_ENTRY',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/Story/',
  solutionProjectEntries: 5,
  physicalCopyState: 'CLEAN',
  staleDuplicateTypeState: 'NONE',
  aliasWorkaroundState: 'NONE',
  typeForwardingWorkaroundState: 'NONE',
  fileCohesionState: 'COHESIVE',
  godFileState: 'WATCH_ONLY_STORYDIRECTORY_672_LOC_SINGLE_RESPONSIBILITY_BELOW_800_CEILING',
  contractsSemanticOwnershipState: 'CORRECT_CONTRACTS_HOLDS_ERROR_CODES_ONLY_NO_APPLICATION_CONTRACTS_DUMP',
  contractsBoundaryState: 'CLEAN_CONTRACTS_ONLY',
  foreignModuleLayerCoupling: 'ZERO',
  foreignModuleEdgeState: 'NONE_MODULE_REFERENCES_NO_FOREIGN_MODULE_AT_ALL',
  crossModuleJoinState: 'NONE',
  crossModulePersistenceState: 'NONE',
  persistenceOwnershipState: 'ONE_STORYDB_CONTEXT_SCHEMA_STORY',
  schemaMigrationState: 'UNCHANGED_TWO_MIGRATIONS_INITIALSTORY_ADD_STORY_REVIEW_OWNERSHIP',
  localizationState: 'CANONICAL_ALL_TEN_VALIDATION_AND_FIVE_ERROR_CODES_BILINGUALLY_RESOURCED',
  validationCodesLocalized: 10,
  errorCodesLocalized: 5,
  descriptorOwnershipState: 'ONE_DESCRIPTOR_OWNER_FIVE_CODES_VALIDATION_CODES_TRAVEL_VIA_VALIDATION_FAILED_ENVELOPE',
  duplicateErrorDescriptorState: 'NONE_NO_FIRST_OR_LAST_WINS_SUPPRESSION',
  apiResultPatternState: 'CANONICAL_APIRESPONSEFACTORY_FROM_AND_CREATED_ONLY',
  adHocResultState: 'NONE',
  messageTextClassificationState: 'NONE',
  loggingState: 'CANONICAL_ZERO_ILOGGER_ZERO_CONSOLE_ZERO_DEBUG',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL_NO_PARALLEL_CORRELATION',
  hostAuthorityState: 'ALLOWED_COMPOSITION_ROOT_AND_ALLOWED_SECURITY_ADAPTER_ONLY',
  hostIllegalAuthorityState: 'ZERO',
  hostResidueDetail: 'Host keeps exactly the composition root (ToobaModuleComposition using Tooba.Story.Infrastructure.DependencyInjection + MapStoryModuleEndpoints + development seed/migrator lines) and the legitimate platform adapter Host/Security/Seller/HostStorySellerAuthorizer.cs. Host owns zero Story route, business rule, DbContext or persistence. src/backend/Host/Tooba.Host/Story remains ABSENT.',
  hostFinalClosureState: 'PRESERVED',
  globalHostCheckpointState: 'PRESERVED',
  microserviceExtractable: true,
  microserviceExtractabilityDetail: 'Contracts references nothing; Domain/Application reference only Contracts + BuildingBlocks; Endpoints references Application + Contracts + BuildingBlocks; Infrastructure references Application + Contracts + Domain + ModuleContracts + Persistence. Foreign module Application/Infrastructure/Domain/Endpoints edge ZERO; cross-module persistence and join NONE; no namespace alias or type-forwarding workaround; the module never references Tooba.Host, so dropping /Modules/Story/ into its own host needs only a Contracts-shaped inbound seam.',
  persianDocumentationState: 'SATISFIED_PER_32_PERSIAN_CODE_DOCUMENTATION_STANDARD',
  behaviorChangeThisWave: 'NONE_NO_PRODUCTION_FILE_CHANGED',
  guardsAdded: 'StoryModuleAmsc001W3CertGuardTests (8 facts)',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  manifestState: 'CERTIFIED_RECORD_WITH_AMSC_001_CERTIFICATION_NOTE',
  waveLineage: `W0 ${W0_COMMIT} -> W1 ${W1_COMMIT} -> W2 ${W2_COMMIT} -> W3 this commit`,
  preExistingUnrelatedRedGuards: 'HostGridAmcR3GuardTests, HostGridAmcR4GuardTests and HostGridAmcR5R1GuardTests (plus TmarDurableGuardTests.certifiedModules literal, TmarCompleteReferenceStructureGateTests.certifiedModules literal and TmarSourceSizeAndInfraAppTests) are red at the untouched W2 HEAD for Catalog/Party/Reviews folder-drift, a repository-global lastAcceptedTask pin and a git-ignored .tmp-baseline worktree. None is Story debt and none is repaired by this module-local wave.',
  focusedValidation: 'dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -> 0 errors; StoryModuleAmsc001W2StructureGuardTests 7/7, StoryModuleAmsc001W3CertGuardTests 8/8, StoryModuleAmsc001W1MigrateGuardTests, StoryModuleAmcW1/W3/W4/W5/W6, HostStoryAmc and StoryFoundation focused filter -> all Story-owned facts pass.',
  blockingResidualDebt: 'ZERO',
  nonBlockingResidualDebt: 'StoryDirectory.cs 672 LOC WATCH only (single responsibility, below the 800 ceiling).',
  stopGate: 'USER_REVIEW_STORY_AMSC_001_W3',
  automaticNextImplementationTask: 'NONE',
  commit: 'PENDING_W3_COMMIT',
  waveOutcome: 'AMSC_WAVE_LINE_COMPLETE',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3/certification.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storyAmsc001W3": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}

const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// --- validate ------------------------------------------------------------
const parsed = JSON.parse(content);

const w0 = parsed.storyAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.commit !== W0_COMMIT) {
  throw new Error('storyAmsc001W0 record missing or disturbed');
}
const w1 = parsed.storyAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.commit !== W1_COMMIT) {
  throw new Error('storyAmsc001W1 record missing or disturbed');
}
const w2 = parsed.storyAmsc001W2;
if (!w2 || w2.state !== 'STRUCTURE_COMPLETE' || w2.commit !== W2_COMMIT
  || w2.startingHead !== W1_COMMIT || w2.verdict !== 'READY_FOR_CERTIFY') {
  throw new Error('storyAmsc001W2 record missing or wrong state');
}
const w3 = parsed.storyAmsc001W3;
if (!w3 || w3.state !== 'STORY_AMSC_001_CERTIFIED' || w3.verdict !== 'COMPLETE_REFERENCE_PATTERN'
  || w3.startingHead !== W2_COMMIT || w3.structureState !== 'CERTIFIED' || w3.structureCertified !== true
  || w3.lockVersion !== 'ARCH-COMPLETE-002' || w3.blockingResidualDebt !== 'ZERO') {
  throw new Error('storyAmsc001W3 record missing or wrong state');
}
if (w0.startingHead !== 'e9301c4edf219d1129d93e6869f346a246522019') {
  throw new Error('storyAmsc001W0 startingHead changed');
}

const certified = parsed.structureLock.certifiedModules;
if (certified.length !== 29 || certified.filter((m) => m === 'Story').length !== 1
  || certified.filter((m) => m === 'StoreContext').length !== 1) {
  throw new Error('structureLock.certifiedModules promotion wrong');
}
if (parsed.structureLock.version !== 'ARCH-COMPLETE-002') {
  throw new Error('structureLock.version changed');
}
if (parsed.completeReferenceModules.length !== 12) {
  throw new Error('completeReferenceModules length changed');
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

for (const pointer of ['lastAcceptedTask', 'lastAcceptedCommit', 'latestAcceptedImplementationWave',
  'currentHostCheckpoint', 'workflowStop', 'automaticNextImplementationTask']) {
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

if (!content.endsWith('}' + NL)) {
  throw new Error('unexpected trailing bytes');
}
if ((content.match(/"PENDING_W2_COMMIT"/g) || []).length !== 0) {
  throw new Error('PENDING_W2_COMMIT marker not stamped');
}
if ((content.match(/"PENDING_W3_COMMIT"/g) || []).length !== 1) {
  throw new Error('expected exactly one PENDING_W3_COMMIT marker');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: storyAmsc001W3 + Story certification promotion)');
