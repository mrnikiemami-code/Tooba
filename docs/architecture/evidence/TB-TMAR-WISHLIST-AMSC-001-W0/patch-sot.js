// TB-TMAR-WISHLIST-AMSC-001-W0 — persist the Analyze wave checkpoint into the durable TMAR SoT.
// Additive only: appends the wishlistAmsc001W0 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"wishlistAmsc001W0"')) {
  throw new Error('wishlistAmsc001W0 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-WISHLIST-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-analyze',
  target: 'src/backend/Modules/Wishlist/Tooba.Wishlist.*',
  startingHead: '8502b91a77b3f67ff1703df94d990f9537489c12',
  parentTask: 'TB-TMAR-WISHLIST-AMC-001',
  priorCertificationTask: 'TB-TMAR-WISHLIST-AMC-001-W4',
  state: 'ANALYZE_COMPLETE',
  verdict: 'READY_TO_MIGRATE',
  lockVersion: 'ARCH-COMPLETE-002',
  hostWishlistFolderState: 'ABSENT_CLOSED_HOST_ZERO',
  reVerificationNotEvacuation: true,
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED',
  hostOwnedRouteCount: 0,
  moduleOwnedRouteCount: 4,
  endpointReachableRequests: 4,
  routeSurface: 'GET /v1/customer/wishlist; POST /v1/customer/wishlist/{productId:guid}; DELETE /v1/customer/wishlist/{productId:guid}; POST /v1/customer/wishlist/membership.',
  routeGroupPrefix: '/v1/customer/wishlist',
  cqrsState: 'COMPLIANT_REAL_IREQUEST_IREQUESTHANDLER_ISENDER_MEDIATR_12_5',
  totalMediatRRequests: 4,
  validatorCoverageState: 'EXHAUSTIVE_3_VALIDATOR_REQUIRED_1_NO_VALIDATOR_REQUIRED',
  validatorRequiredObserved: 3,
  noValidatorRequiredObserved: 1,
  validatorRequiredRequests: 'AddWishlistItemCommand,RemoveWishlistItemCommand,GetWishlistMembershipQuery',
  noValidatorRequiredRequests: 'ListWishlistPageQuery',
  validatorMatrixSetEquality: 'PROVEN_4_ROUTES_4_SENDS_4_CLASSIFIED_EACH_EXACTLY_ONCE',
  foundationState: 'FOUNDATION_READY',
  ownershipState: 'correct',
  mustSplitState: 'NONE',
  fileCohesionState: 'COHESIVE',
  oversizedGodFileState: 'NONE',
  localizationState: 'CANONICAL_THREE_OF_THREE_CLIENT_OBSERVABLE_CODES_RESOURCED_EN_AND_FA_IMPLICIT_LOGICAL_NAMES',
  apiResultPatternState: 'ENDPOINTS_CLEAN_CANONICAL_APIRESPONSEFACTORY_ONE_RAW_FAULT_LITERAL_ON_OUTBOX_PATH',
  stableErrorCodeState: 'CATALOGUED_THREE_DECLARED_THREE_CATALOGUED_THREE_LOCALIZED_NO_DECLARED_CODE_GUARD',
  declaredCodeGuardState: 'ABSENT',
  typedFaultSeamState: 'SEMANTIC_ONLY_NO_CONTRACTOPERATIONEXCEPTION_ARM',
  rawFaultState: 'ONE_RAW_INVALID_OPERATION_EXCEPTION_LITERAL_IN_WISHLISTOUTBOXREGISTRATION',
  loggingState: 'CANONICAL_ZERO_LOGGER_CALLS_IN_MODULE',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  contractsBoundaryState: 'CLEAN_CONTRACTS_ONLY_CATALOG_AND_ORDER_FULFILLMENT_PORTS',
  crossModuleCouplingState: 'NONE_ZERO_FOREIGN_PRODUCTION_EDGES',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_ONE_WISHLISTDBCONTEXT_SCHEMA_WISHLIST_ONE_TABLE_PLUS_OUTBOX',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_AND_MODULE_OWNED_DEVELOPMENT_SEED_ADAPTER_ONLY',
  schemaMigrationState: 'UNCHANGED_ONE_MIGRATION_20260825162434_INITIALWISHLIST',
  behaviorPreservationRisk: 'LOW',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/Wishlist/',
  solutionProjectEntries: 5,
  pathNamespaceState: 'EXACT_ZERO_MISMATCHES_OVER_TWENTY_FIVE_PRODUCTION_FILES',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  rootAllowlistsObserved: {
    'Tooba.Wishlist.Contracts': [],
    'Tooba.Wishlist.Domain': [],
    'Tooba.Wishlist.Application': [],
    'Tooba.Wishlist.Infrastructure': ['WishlistModule.cs'],
    'Tooba.Wishlist.Endpoints': ['WishlistEndpointModule.cs']
  },
  singleFileRequestLeafState: 'NONE_ONLY_TECHNICAL_AXIS_AND_SHARED_SEAM_FOLDERS',
  technicalAxisFirstState: 'ABSENT',
  applicationPrimaryAxis: 'CAPABILITY_FIRST_CUSTOMER_PLUS_SHARED_COMPOSITION_MODELS_PORTS',
  structureHandoffState: 'NOT_REQUIRED_PHYSICAL_TREE_ALREADY_CANONICAL',
  structureHandoffDetail: 'Physical tree already PROFESSIONAL_SHALLOW with exact path<->namespace and enforced root allowlists; W2 owns only the manifest structure-authority record and the durable W2 structure guard for the AMSC lineage.',
  canonicalReferenceUsed: 'BuildingBlocks (Result, ApiResponseFactory, IErrorCatalogContributor, IErrorResourceSet, ContractOperationException, AddToobaCqrsFoundation); UserPreference/Tax/Wallet for the Contracts-Errors + Application/Composition/<Module>Operation seam + locked bilingual LogicalName shape.',
  blockers: [
    'B1_NO_DECLARED_CODE_GUARD: WishlistErrorCodes declares three module codes plus the Foundation-owned SessionRequired but exposes no KnownCodes set and no IsKnown(string?) guard, so the module cannot classify a typed contract fault by code.',
    'B2_RAW_FAULT_LITERAL: Tooba.Wishlist.Infrastructure/Persistence/WishlistOutboxRegistration.GetEventTypeName throws raw InvalidOperationException("Wishlist integration event is not registered."), carrying machine identity in prose Message with no declared code, descriptor or resource.',
    'B3_NO_TYPED_CONTRACT_FAULT_ARM: WishlistOperation maps only SemanticException; there is no catch (ContractOperationException ex) when (WishlistErrorCodes.IsKnown(ex.Code)) arm.',
    'B4_IMPLICIT_LOGICAL_NAMES: Tooba.Wishlist.Contracts.csproj has no explicit EmbeddedResource LogicalName lock for WishlistErrors.resx / WishlistErrors.fa.resx.',
    'B5_NO_AMSC_LINEAGE: no wishlistAmsc001W0..W3 SoT records, no AMSC evidence tree, no structureAuthorityTask record and no W1/W2/W3 durable guards.'
  ],
  nonBlockingObservations: [
    'WishlistModuleAmcW1/W2/W3/W4CertGuardTests, HostWishlistAmcGuardTests and WishlistFoundationTests pin the current Contracts-Errors surface, so W1 keeps the catalog/resource-set home in Tooba.Wishlist.Contracts and must not relocate it.',
    'TmarCompleteReferenceStructureGateTests already lists Wishlist in structureLock.certifiedModules (32) and keeps it out of uncertifiedHttpOwningModules; the AMSC waves must not disturb that global lock.',
    'Wishlist is present in completeReferenceModules from the earlier TB-TMAR-WISHLIST-AMC-001 lineage, so the AMSC waves refresh rather than create that record.'
  ],
  noArchitectureDecisionRequired: true,
  hostTouched: false,
  productionCodeChanged: false,
  commit: 'REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER',
  waveOutcome: 'MIGRATE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W0/analyze.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "wishlistAmsc001W0": ${line}` : `  ${line}`))
  .join(NL);

// The file mixes LF inside records with a trailing CRLF; anchor on the last record close instead of
// assuming a single newline convention.
const terminator = `  }\n}\r\n`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}
const content = original.slice(0, lastIndex) + `  },\n` + recordText + `\n}\r\n`;

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w0 = parsed.wishlistAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.verdict !== 'READY_TO_MIGRATE') {
  throw new Error('wishlistAmsc001W0 record missing or wrong state');
}

// historical Wishlist lineage must be intact
if (!Object.prototype.hasOwnProperty.call(parsed, 'wishlistAmc001')) {
  throw new Error('historical record wishlistAmc001 was disturbed');
}
if (parsed.wishlistAmc001.state !== 'COMPLETE_REFERENCE_PATTERN') {
  throw new Error('wishlistAmc001 state was disturbed');
}

// previously accepted AMSC lineage must be intact
if (!parsed.walletAmsc001W3
  || parsed.walletAmsc001W3.state !== 'WALLET_AMSC_001_CERTIFIED'
  || parsed.walletAmsc001W3.verdict !== 'COMPLETE_REFERENCE_PATTERN') {
  throw new Error('walletAmsc001W3 record was disturbed');
}
if (!parsed.userPreferenceAmsc001W3R1) {
  throw new Error('userPreferenceAmsc001W3R1 record was disturbed');
}

// structureLock is a repository-global pointer set; Wishlist is already a member and W0 must not
// touch it. Assert only that it stays exactly as it was observed.
const certified = parsed.structureLock.certifiedModules;
if (!Array.isArray(certified) || certified.length !== 32
  || certified.filter((m) => m === 'Wishlist').length !== 1
  || certified.filter((m) => m === 'Wallet').length !== 1) {
  throw new Error('structureLock.certifiedModules was disturbed');
}
if (parsed.structureLock.version !== 'ARCH-COMPLETE-002') {
  throw new Error('structureLock.version changed');
}
if (parsed.completeReferenceModules.length !== 12) {
  throw new Error('completeReferenceModules length changed');
}
if (parsed.structureLock.uncertifiedModulesNote === undefined) {
  throw new Error('structureLock.uncertifiedModulesNote was disturbed');
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
if (!content.endsWith('}\r\n')) {
  throw new Error('unexpected trailing bytes');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: wishlistAmsc001W0)');
