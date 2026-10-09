// TB-TMAR-SUPPORT-AMSC-001-W0 — persist the Analyze wave checkpoint into the durable TMAR SoT.
// Additive only: appends the supportAmsc001W0 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"supportAmsc001W0"')) {
  throw new Error('supportAmsc001W0 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-SUPPORT-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-analyze',
  target: 'src/backend/Modules/Support/Tooba.Support.*',
  startingHead: 'c886698dd644088a2b3a5b31cbd3ec24d492c23d',
  parentTask: 'TB-TMAR-HOST-SUPPORT-AMC-001-R1',
  state: 'ANALYZE_COMPLETE',
  verdict: 'READY_TO_MIGRATE',
  lockVersion: 'ARCH-COMPLETE-002',
  priorHostEvacuationState: 'CLOSED_HOST_ZERO_R1_ACCESSCONTROL_CONTRACTS_SEAM_PRESERVED',
  hostSupportFolderState: 'ABSENT',
  reVerificationNotEvacuation: true,
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED',
  hostOwnedRouteCount: 0,
  endpointReachableRequests: 17,
  cqrsState: 'COMPLIANT_REAL_IREQUEST_IREQUESTHANDLER_ISENDER_MEDIATR_12_5',
  totalMediatRRequests: 17,
  validatorCoverageState: 'GAPS_NINE_VALIDATOR_REQUIRED_EIGHT_NO_VALIDATOR_REQUIRED_ZERO_ABSTRACTVALIDATOR_CLASSES',
  validatorRequiredObserved: 9,
  noValidatorRequiredObserved: 8,
  validatorMatrixSetEquality: 'PROVEN_17_ROUTES_17_CLASSIFIED_EACH_EXACTLY_ONCE',
  foundationState: 'FOUNDATION_PARTIAL_MISSING_TOOBA_SUPPORT_CONTRACTS',
  ownershipState: 'correct',
  mustSplitState: 'NONE',
  fileCohesionState: 'COHESIVE',
  oversizedGodFileState: 'WATCH_ONLY_DIRECTORIES_SUPPORTDIRECTORY_511_LOC_SINGLE_RESPONSIBILITY_BELOW_800_CEILING',
  localizationState: 'MISSING_INFRASTRUCTURE_USE_ONE_OF_SIX_CLIENT_OBSERVABLE_CODES_RESOURCED',
  apiResultPatternState: 'CANONICAL_APIRESPONSEFACTORY_FROM_FROMFAILURE_ONLY',
  stableErrorCodeState: 'UNREGISTERED_CODES_NINE_DECLARED_SIX_REACHABLE_TWO_FOUNDATION_OWNED_ONE_DEAD',
  loggingState: 'CANONICAL_ZERO_LOGGER_CALLS_IN_MODULE',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  contractsBoundaryState: 'CLEAN_LEGAL_CONTRACTS_ONLY_BUT_CONTRACTS_PROJECT_MISSING',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY_SINGLE_EDGE_SUPPORT_INFRASTRUCTURE_TO_NOTIFICATION_CONTRACTS',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_ONE_SUPPORTDBCONTEXT_SCHEMA_SUPPORT',
  endpointOwnershipStateDetail: 'SEVENTEEN_MODULE_ROUTES_OVER_V1_CUSTOMER_SELLER_ADMIN_SUPPORT',
  hostResidueState: 'ALLOWED_SECURITY_ADAPTER_TWO_FILES_AND_ALLOWED_COMPOSITION_ROOT_ONLY',
  schemaMigrationState: 'UNCHANGED_ONE_MIGRATION_20260827120000_INITIALSUPPORT',
  behaviorPreservationRisk: 'LOW',
  folderGranularityState: 'TECHNICAL_AXIS_FIRST_PLUS_OVER_FOLDERED_SEVENTEEN_SINGLE_FILE_REQUEST_LEAVES',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/Support/',
  solutionProjectEntries: 5,
  pathNamespaceState: 'EXACT_ZERO_MISMATCHES_OVER_FORTY_SIX_PRODUCTION_FILES',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  rootAllowlistsObserved: {
    'Tooba.Support.Domain': [],
    'Tooba.Support.Application': [],
    'Tooba.Support.Infrastructure': [],
    'Tooba.Support.Endpoints': ['SupportEndpointModule.cs']
  },
  singleFileRequestLeafState: 'SEVENTEEN_UNJUSTIFIED',
  technicalAxisFirstState: 'PRESENT',
  infrastructureMigrationDepthState: 'WRONG_DEPTH_INFRASTRUCTURE_MIGRATIONS_EXPECTED_PERSISTENCE_MIGRATIONS',
  seedsFolderState: 'NON_CANONICAL_INFRASTRUCTURE_SEEDS_ONLY_MODULE_IN_REPO',
  structureHandoffState: 'REQUIRED',
  structureHandoffDetail: 'W2 owns: (1) capability-first flatten of the 17 single-file request leaves into Application/Tickets/{Commands,Queries,Models,Ports,Validators}; (2) Application/Composition/ for the typed-fault seam; (3) Infrastructure/Migrations -> Infrastructure/Persistence/Migrations; (4) Infrastructure/Seeds + Development bootstrap + demo snapshot store consolidated under Development/; (5) Domain/ValueObjects/SupportEnums.cs -> Domain/Enums/; (6) split SupportEnumParsing out of Ports/ISupportDirectory.cs. Any move must update the Support manifest allowlists, SupportArchitectureGuardTests allowlists and the Support scoped AMSC guards atomically.',
  canonicalReferenceUsed: 'BuildingBlocks (Result, ApiResponseFactory, IErrorCatalogContributor, IErrorResourceSet, ContractOperationException, AddToobaCqrsFoundation, ObservabilityLogScope); Returns for Contracts-Errors + typed-fault + Application/Composition/<Module>Operation + Validation/ precedent; Story for validator + bilingual resx + Infrastructure/Messaging split; docs/architecture/32-persian-code-documentation-standard.md.',
  blockers: [
    'B1_MISSING_CONTRACTS_PROJECT: Tooba.Support.Contracts is absent although Support is HTTP_OWNING, owns stable cross-boundary machine codes, and is one of only two uncertifiedHttpOwningModules in tmar-module-structure-manifests.json (the other is Wallet). The AMC-R1 workaround (a hand-rolled SupportAdminAuthorizationCodes restatement so Host need not reference Support.Application) exists only because this boundary is missing.',
    'B2_LOCALIZATION_RESOURCE_HOLLOW: Resources/SupportErrors.resx localizes exactly one key (support.authorization.unavailable) while five further client-observable codes resolve to ErrorDescriptors with no .resx entry; support.action.rejected is registered but never emitted (dead registration).',
    'B3_STABLE_CODE_CATALOGUE_DUPLICATED: SupportExceptionMapper hardcodes a 20-entry known-code set duplicating the identity that belongs to the code catalogue; nine declared outcome codes are split across three homes (Application.Errors, Endpoints.Admin codes, Foundation codes) with no single machine-readable surface.',
    'B4_VALIDATOR_MATRIX_NOT_EXHAUSTIVE: zero AbstractValidator classes for nine caller-controlled malformable requests (status/requesterKind/category/priority free-text filters, subject/category/priority/body/relatedEntityType body fields, Idempotency-Key header); validation is performed implicitly by directory parsing and enum parsing far from the transport boundary.',
    'B5_MESSAGE_TEXT_FAULT_CLASSIFICATION: expected business faults are raised as InvalidOperationException carrying a machine code in Message and classified by exact message-text equality in SupportExceptionMapper. Returns/Story use typed code-carrying faults (ContractOperationException.Code / SemanticException.Error) classified by typed code only.',
    'B6_STRUCTURE_DIVERGENCE: technical-axis-first Application/Commands|Queries/<UseCase> with 17 single-file leaves; Infrastructure/Migrations at wrong depth; Infrastructure/Seeds non-canonical; Domain/ValueObjects holding five enums in one file.',
    'B7_NO_AMSC_LINEAGE: no supportAmsc001W0..W3 SoT record, no structureCertified manifest entry, no certificationNote, no AMSC evidence tree, no Master Recovery AMSC checkpoint.',
    'B8_NO_SUPPORT_SCOPED_AMSC_GUARD: current protections are the Host-evacuation HostSupportAmcGuardTests, SupportFoundationTests, HostModuleEndpointOwnershipTests and the in-module SupportArchitectureGuardTests; there is no route/request/validator set-equality certification guard for Support.'
  ],
  nonBlockingObservations: [
    'SupportArchitectureGuardTests asserts "Support_golden_boundaries_and_physical_layout_remain_clean" with no Allowlist for SupportEnumParsing, so a W2 split must keep the guard assertions intact rather than weakened.',
    'SupportArchitectureGuardTests.AllowedDomainFolders already lists "Events" and "Policies" which do not exist; AllowedInfrastructureFolders lists both "Migrations" and "Persistence" and "Seeds" - W2 must narrow these honestly rather than widen them.',
    'HostModuleEndpointOwnershipTests:28 pins (Support, Tooba.Support.Endpoints, MapSupportEndpoints(), Support/SupportEndpoints.cs) and asserts the deleted Host folder stays absent.',
    'SupportFoundationTests pins SupportEndpointModule.cs, the three audience endpoint files, SupportDbContext, SupportModule, the migration registry descriptor and Program.cs wiring.',
    'Returns.Endpoints has no error catalog contributor/resx because it is descriptor-light; Support has one, so Support keeps the Endpoints/Errors + Endpoints/Resources precedent.',
    'Persian strings in Infrastructure/Seeds/SupportDevelopmentSeed.cs are demo display values, not user-facing API text.',
    'Host Program.cs line 178 already registers the Support Application assembly with AddToobaCqrsFoundation, so new validators are discoverable without any Host change.'
  ],
  noArchitectureDecisionRequired: true,
  hostTouched: false,
  productionCodeChanged: false,
  commit: 'PENDING_W0_COMMIT',
  waveOutcome: 'MIGRATE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-SUPPORT-AMSC-001-W0/analyze.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "supportAmsc001W0": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}
const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w0 = parsed.supportAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.verdict !== 'READY_TO_MIGRATE') {
  throw new Error('supportAmsc001W0 record missing or wrong state');
}

// historical Host-evacuation lineage must be intact
for (const key of ['hostSupportAmc', 'hostSupportAmcR1']) {
  if (!Object.prototype.hasOwnProperty.call(parsed, key)) {
    throw new Error(`historical record ${key} was disturbed`);
  }
}
if (parsed.hostSupportAmc.status !== 'CLOSED_HOST_ZERO'
  || parsed.hostSupportAmcR1.status !== 'CLOSED_HOST_ZERO_R1_ACCESSCONTROL_CONTRACTS_SEAM') {
  throw new Error('Support Host-evacuation state was disturbed');
}

// previously accepted AMSC lineage must be intact
if (!parsed.storyAmsc001W3R2
  || parsed.storyAmsc001W3R2.state !== 'STORY_AMSC_001_W3_R2_SET_EQUALITY_PROVEN'
  || parsed.storyAmsc001W3R2.verdict !== 'COMPLETE_REFERENCE_PATTERN') {
  throw new Error('storyAmsc001W3R2 record was disturbed');
}

// structureLock is a repository-global pointer set; Support is not a member yet and W0 must not
// touch it. Assert only that it stays exactly as it was observed.
const certified = parsed.structureLock.certifiedModules;
if (!Array.isArray(certified) || certified.length !== 29
  || certified.filter((m) => m === 'Story').length !== 1
  || certified.includes('Support')) {
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: supportAmsc001W0)');
