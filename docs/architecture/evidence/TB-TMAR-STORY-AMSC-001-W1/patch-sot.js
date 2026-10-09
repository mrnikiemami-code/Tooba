// TB-TMAR-STORY-AMSC-001-W1 — persist the Migrate wave checkpoint into the durable TMAR SoT.
// Additive only: appends the storyAmsc001W1 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const rawOriginal = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const W0_COMMIT = '0c73390a3211e0ee9057e9234d62d3e4f14b5e4e';

if (rawOriginal.includes('"storyAmsc001W1"')) {
  throw new Error('storyAmsc001W1 already present — refusing to overwrite');
}
if (!rawOriginal.includes('"storyAmsc001W0"')) {
  throw new Error('storyAmsc001W0 missing — W0 must land first');
}
// W0 recorded its own commit as PENDING at write time (a record cannot contain its own SHA);
// stamp the real, pushed W0 SHA now that it is known.
if (!rawOriginal.includes('"commit": "PENDING_W0_COMMIT"')) {
  throw new Error('expected storyAmsc001W0 pending commit marker not found');
}
if ((rawOriginal.match(/"PENDING_W0_COMMIT"/g) || []).length !== 1) {
  throw new Error('storyAmsc001W0 pending commit marker is not unique');
}
const original = rawOriginal.replace('"commit": "PENDING_W0_COMMIT"', `"commit": "${W0_COMMIT}"`);

const record = {
  task: 'TB-TMAR-STORY-AMSC-001-W1',
  parentTask: 'TB-TMAR-STORY-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-migrate',
  target: 'src/backend/Modules/Story/Tooba.Story.*',
  startingHead: W0_COMMIT,
  state: 'MIGRATE_COMPLETE',
  verdict: 'READY_TO_STRUCTURE',
  lockVersion: 'ARCH-COMPLETE-002',
  foundationState: 'FOUNDATION_READY',
  ownershipState: 'correct',
  filesMoved: 0,
  filesDeleted: 0,
  behaviorChangeState: 'NONE_OBSERVABLE_API_AND_SCHEMA_UNCHANGED',
  documentationStandardState: 'PERSIAN_CODE_DOCUMENTATION_STANDARD_32_SATISFIED_ON_TOUCHED_SURFACE',
  documentationFilesRepaired: [
    'Tooba.Story.Contracts/Errors/StoryErrorCodes.cs',
    'Tooba.Story.Application/Stories/StoryFailureMapper.cs',
    'Tooba.Story.Application/Stories/Composition/StoryOperation.cs',
    'Tooba.Story.Application/Stories/Ports/IAdminStoryGridPort.cs',
    'Tooba.Story.Application/Stories/Validators/StoryValidators.cs',
    'Tooba.Story.Endpoints/Errors/StoryHttpErrors.cs',
    'Tooba.Story.Endpoints/Seller/IStorySellerAuthorizer.cs',
    'Tooba.Story.Infrastructure/Adapters/AdminStoryGridAdapter.cs',
    'Tooba.Story.Infrastructure/Grid/StoryAdminGridPolicies.cs'
  ],
  localizationState: 'CANONICAL_VALIDATION_AND_ERROR_CODES_BILINGUALLY_RESOURCED',
  validationLocalizationState: 'STORY_VALIDATION_CODES_LOCALIZED',
  localizationDetail: 'StoryValidationCodes and StoryErrorCodes now have full EN + FA entries in StoryErrors.resx / StoryErrors.fa.resx. Validation codes remain deliberately unregistered as ErrorDescriptor entries (they travel inside the foundation validation.failed envelope via SafeErrorMapper.MapValidation), matching the certified Returns/Promotion precedent. No stable error code was renamed or repurposed; no descriptor ownership changed.',
  validationCodesLocalized: 10,
  errorCodesLocalized: 5,
  resxKeysAdded: 9,
  validatorCoverageState: 'EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED',
  validatorMatrix: '25 endpoint-reachable requests; GetPublicStoriesQuery locale/market provenance gap closed by a new GetPublicStoriesQueryValidator; the 9 remaining NO_VALIDATOR_REQUIRED rows carry no malformable transport shape (Guid route ids, authorization-seam actor ids, bodyless transitions).',
  validatorsAdded: ['GetPublicStoriesQueryValidator'],
  validationCodesAdded: ['story.locale.invalid', 'story.market.invalid'],
  validatorProvenanceNote: 'The new validator enforces transport shape only (empty allowed, bounded length, alphanumeric) and never owns authorization, ownership, DB existence or domain rules; the existing StoryRules.MatchesLocale/MatchesMarket semantics are untouched.',
  cqrsState: 'UNCHANGED_25_REAL_IREQUEST_IREQUESTHANDLER_ISENDER',
  apiResultPatternState: 'CANONICAL_APIRESPONSEFACTORY_UNCHANGED',
  stableErrorCodeState: 'UNCHANGED_FIVE_CODES_ONE_DESCRIPTOR_OWNER',
  loggingState: 'CANONICAL_UNCHANGED',
  openTelemetryState: 'CANONICAL_UNCHANGED',
  correlationTraceState: 'CANONICAL_UNCHANGED',
  contractsBoundaryState: 'CLEAN_CONTRACTS_ONLY_UNCHANGED',
  crossModuleCouplingState: 'NONE_UNCHANGED',
  crossModuleJoinState: 'NONE_UNCHANGED',
  persistenceOwnershipState: 'UNCHANGED_ONE_STORYDB_CONTEXT_SCHEMA_STORY',
  schemaMigrationState: 'UNCHANGED_NO_MIGRATION_ADDED_OR_REGENERATED',
  hostTouched: false,
  hostResidueState: 'UNCHANGED_ALLOWED_SECURITY_ADAPTER_AND_COMPOSITION_ROOT_ONLY',
  guardsAdded: 'StoryModuleAmsc001W1MigrateGuardTests (6 facts)',
  guardsUpdated: [
    'StoryModuleAmcW5ValidatorGuardTests — matrix closed to 16 VALIDATOR_REQUIRED + 9 NO_VALIDATOR_REQUIRED (tightened, not weakened)'
  ],
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  structureHandoffState: 'REQUIRED',
  structureHandoffDetail: 'Unchanged from W0: composition-entry placement (Infrastructure/StoryModule.cs vs Infrastructure/DependencyInjection/), StoryOutboxRegistration split, Infrastructure/Directory -> Directories rename, Stories/Composition/StoryOperation.cs and Stories/StoryFailureMapper.cs technical-axis placement.',
  microserviceExtractable: true,
  microserviceExtractabilityDetail: 'Unchanged: the module still references no foreign module at all; only Tooba.BuildingBlocks, Tooba.ModuleContracts, Tooba.Persistence.',
  commit: 'PENDING_W1_COMMIT',
  waveOutcome: 'STRUCTURE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W1/migrate.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storyAmsc001W1": ${line}` : `  ${line}`))
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
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.commit !== W0_COMMIT) {
  throw new Error('storyAmsc001W0 record missing or disturbed');
}
const w1 = parsed.storyAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.startingHead !== W0_COMMIT
  || w1.verdict !== 'READY_TO_STRUCTURE') {
  throw new Error('storyAmsc001W1 record missing or wrong state');
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: storyAmsc001W1)');
