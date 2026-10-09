// TB-TMAR-STORY-AMSC-001-W0 — persist the Analyze wave checkpoint into the durable TMAR SoT.
// Additive only: appends the storyAmsc001W0 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"storyAmsc001W0"')) {
  throw new Error('storyAmsc001W0 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-STORY-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-analyze',
  target: 'src/backend/Modules/Story/Tooba.Story.*',
  startingHead: 'e9301c4edf219d1129d93e6869f346a246522019',
  parentTask: 'TB-TMAR-STORY-AMC-001-W6-CERT',
  state: 'ANALYZE_COMPLETE',
  verdict: 'READY_TO_MIGRATE',
  lockVersion: 'ARCH-COMPLETE-002',
  priorCertificationState: 'STORY_STRUCTURE_CERTIFIED_COMPLETE_REFERENCE_PATTERN_UNDER_TB_TMAR_STORY_AMC_001_W6',
  reVerificationNotEvacuation: true,
  hostStoryFolderState: 'ABSENT',
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED',
  endpointReachableRequests: 25,
  cqrsState: 'COMPLIANT_REAL_IREQUEST_IREQUESTHANDLER_ISENDER',
  validatorCoverageState: 'GAPS_GETPUBLICSTORIESQUERY_LOCALE_MARKET_UNCLASSIFIED',
  validatorRequiredObserved: 15,
  noValidatorRequiredObserved: 10,
  foundationState: 'FOUNDATION_READY',
  ownershipState: 'correct',
  mustSplitState: 'NONE',
  fileCohesionState: 'COHESIVE',
  oversizedGodFileState: 'WATCH_ONLY_DIRECTORY_STORYDIRECTORY_672_LOC_SINGLE_RESPONSIBILITY_BELOW_800_CEILING',
  localizationState: 'HARDCODED_TEXT_SEVEN_OF_EIGHT_STORY_VALIDATION_CODES_HAVE_NO_RESX_RESOURCE',
  apiResultPatternState: 'CANONICAL_APIRESPONSEFACTORY_FROM_CREATED_ONLY',
  stableErrorCodeState: 'CATALOGUED_FIVE_STORY_CODES_ONE_DESCRIPTOR_OWNER',
  loggingState: 'CANONICAL_ZERO_LOGGER_CALLS_IN_MODULE',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  contractsBoundaryState: 'CLEAN_CONTRACTS_HOLDS_ERROR_CODES_ONLY',
  crossModuleCouplingState: 'NONE_MODULE_REFERENCES_NO_FOREIGN_MODULE_AT_ALL',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_ONE_STORYDB_CONTEXT_SCHEMA_STORY',
  hostResidueState: 'ALLOWED_SECURITY_ADAPTER_AND_ALLOWED_COMPOSITION_ROOT_ONLY',
  schemaMigrationState: 'UNCHANGED_TWO_MIGRATIONS_INITIALSTORY_ADD_STORY_REVIEW_OWNERSHIP',
  behaviorPreservationRisk: 'LOW',
  folderGranularityState: 'PROFESSIONAL_SHALLOW_SINGLE_CAPABILITY_STORIES',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/Story/',
  solutionProjectEntries: 5,
  pathNamespaceState: 'EXACT_ZERO_MISMATCHES_OVER_40_PRODUCTION_FILES',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  rootAllowlistsObserved: {
    'Tooba.Story.Contracts': [],
    'Tooba.Story.Domain': [],
    'Tooba.Story.Application': [],
    'Tooba.Story.Endpoints': ['StoryEndpointModule.cs'],
    'Tooba.Story.Infrastructure': ['StoryModule.cs']
  },
  singleFileRequestLeafState: 'ZERO',
  technicalAxisFirstState: 'ZERO',
  structureHandoffState: 'REQUIRED',
  structureHandoffDetail: 'W2 owns: (1) composition-entry placement Infrastructure/StoryModule.cs vs Infrastructure/DependencyInjection/; (2) StoryOutboxRegistration split from StoryModule.cs into Infrastructure/Messaging or Outbox; (3) Infrastructure/Directory singular -> Infrastructure/Directories plural to match 20 certified modules; (4) Stories/Composition/StoryOperation.cs vs Application/Composition/; (5) Stories/StoryFailureMapper.cs technical-axis placement; (6) whether to keep the single-capability Stories/ wrapper. Any move must update the manifest, HostDevelopmentMigrationSeamGuardTests, HostGridAmcR2/R5/R5R1 guards, AdminDbNativeGridQueryTests and StoryModuleAmcW3/W6 guards atomically.',
  canonicalReferenceUsed: 'BuildingBlocks (Result/ApiResponseFactory/SafeErrorMapper/IErrorDefinitionCatalog/IErrorResourceSet/ValidationBehavior); Promotion for validation-code localization semantics; Settlement/Offer/Payment/Promotion/Returns for Contracts-Errors + Infrastructure/DependencyInjection + Infrastructure/Messaging structure precedent; docs/architecture/32-persian-code-documentation-standard.md.',
  blockers: [
    'B1_VALIDATION_CODE_LOCALIZATION_GAP: 7 of the 8 declared story.* validation machine codes (story.title.required, story.ids.required, story.itemIds.required, story.mediaType.required, story.grid.request.required, story.rejectionReason.required, story.schedule.range.invalid) have no StoryErrors.resx/fa.resx resource while the certified Promotion precedent localizes its own validation keys. W1 closes this additively with zero code change.',
    'B2_VALIDATOR_MATRIX_NOT_EXHAUSTIVE: GetPublicStoriesQuery classifies the caller-controlled locale and market query strings as NO_VALIDATOR_REQUIRED although both are unbounded text reaching StoryRules.MatchesLocale/MatchesMarket and the DB filter. W1 adds the canonical transport-shape validator and re-derives the 25-row matrix.',
    'B3_DOCUMENTATION_STANDARD_GAP: 18 Story production files carry English XML while the Architect-accepted docs/architecture/32-persian-code-documentation-standard.md requires strong professional Persian documentation. W1 rewrites them documentation-only.',
    'B4_STRUCTURAL_DIVERGENCE: Infrastructure/StoryModule.cs at the root instead of DependencyInjection/, StoryOutboxRegistration sharing the composition file instead of Messaging/Outbox, Infrastructure/Directory/ singular instead of Directories/, Stories/Composition/StoryOperation.cs instead of Application/Composition/, and Stories/StoryFailureMapper.cs with no technical axis. W2 decides and aligns.',
    'B5_NO_AMSC_LINEAGE: no storyAmsc001W0..W3 SoT record, no certificationNote on the Story manifest entry, no AMSC evidence tree and no Master Recovery AMSC checkpoint. W0-W3 add them additively without rewriting the AMC-001 history.',
    'B6_NO_STORY_SCOPED_AMSC_GUARD: current protections are the AMC-era StoryModuleAmcW1/W3/W4/W5/W6 guards plus shared Host guards. W2/W3 add module-scoped AMSC durable guards.'
  ],
  nonBlockingObservations: [
    'HostDevelopmentMigrationSeamGuardTests:39 pins Story/Tooba.Story.Infrastructure/StoryModule.cs among 28 module composition roots.',
    'HostGridAmcR2GuardTests:26,34, HostGridAmcR5GuardTests:66, HostGridAmcR5R1GuardTests:72 and AdminDbNativeGridQueryTests:140 pin exact Story Infrastructure Grid paths.',
    'StoryModuleAmcW3StructureGuardTests:53 and StoryModuleAmcW6CertGuardTests:57 assert the Infrastructure root file set is exactly [StoryModule.cs].',
    'TmarSourceSizeAndInfraAppTests fails for a pre-existing, unrelated environment reason (stale git-ignored .tmp-baseline clone); not Story debt.',
    'Intra-module using alias StoryEntity = Tooba.Story.Domain.Aggregates.Story is precedent-legal disambiguation, not folder-debt hiding.',
    'Persian strings in Infrastructure/Development/StoryDevelopmentSeed.cs are seed display values, not user-facing API text.'
  ],
  noArchitectureDecisionRequired: true,
  hostTouched: false,
  productionCodeChanged: false,
  commit: 'PENDING_W0_COMMIT',
  waveOutcome: 'MIGRATE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W0/analyze.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storyAmsc001W0": ${line}` : `  ${line}`))
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
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.verdict !== 'READY_TO_MIGRATE') {
  throw new Error('storyAmsc001W0 record missing or wrong state');
}

// historical AMC-001 lineage must be intact
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

// previously accepted AMSC lineage must be intact
if (!parsed.storeContextAmsc001W3
  || parsed.storeContextAmsc001W3.state !== 'STORECONTEXT_AMSC_001_CERTIFIED') {
  throw new Error('storeContextAmsc001W3 record was disturbed');
}

// structureLock is a repository-global pointer set; Story is not a member yet and W0 must not
// touch it. Assert only that it stays exactly as it was observed.
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: storyAmsc001W0)');
