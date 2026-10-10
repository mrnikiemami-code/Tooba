// TB-TMAR-WALLET-AMSC-001-W0 — persist the Analyze wave checkpoint into the durable TMAR SoT.
// Additive only: appends the walletAmsc001W0 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"walletAmsc001W0"')) {
  throw new Error('walletAmsc001W0 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-WALLET-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-analyze',
  target: 'src/backend/Modules/Wallet/Tooba.Wallet.*',
  startingHead: '4f875042176c2c1f249acffeaad795a2971dd310',
  parentTask: 'TB-TMAR-HOST-WALLET-AMC-001',
  state: 'ANALYZE_COMPLETE',
  verdict: 'READY_TO_MIGRATE',
  lockVersion: 'ARCH-COMPLETE-002',
  priorHostEvacuationState: 'CLOSED_HOST_ZERO_COMPOSITION_SEED_HOST_AND_ADMIN_ADAPTER_ONLY',
  hostWalletFolderState: 'ABSENT',
  reVerificationNotEvacuation: true,
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED',
  hostOwnedRouteCount: 0,
  endpointReachableRequests: 11,
  moduleOwnedRouteCount: 11,
  routeSurface: '/v1/customer/wallet x3 (GET summary, GET ledger, POST gift-cards/redeem); /v1/admin/gift-cards x4 (GET list, POST issue, GET by id, POST revoke); /v1/admin/wallets/{customerActorUserId:guid} x3 (GET summary, GET ledger, POST adjustments); /v1/admin/wallet/demo-preview x1.',
  cqrsState: 'COMPLIANT_REAL_IREQUEST_IREQUESTHANDLER_ISENDER_MEDIATR_12_5',
  totalMediatRRequests: 11,
  validatorCoverageState: 'GAPS_FOUR_VALIDATOR_REQUIRED_SEVEN_NO_VALIDATOR_REQUIRED_ZERO_ABSTRACTVALIDATOR_CLASSES',
  validatorRequiredObserved: 4,
  noValidatorRequiredObserved: 7,
  validatorRequiredRequests: 'RedeemCustomerGiftCardCommand,ListAdminGiftCardsQuery,IssueAdminGiftCardCommand,AdjustAdminWalletCommand',
  noValidatorRequiredRequests: 'GetCustomerWalletSummaryQuery,ListCustomerWalletLedgerQuery,GetAdminGiftCardQuery,RevokeAdminGiftCardCommand,GetAdminWalletQuery,ListAdminWalletLedgerQuery,GetWalletDemoPreviewQuery',
  validatorMatrixSetEquality: 'PROVEN_11_ROUTES_11_CLASSIFIED_EACH_EXACTLY_ONCE',
  foundationState: 'CONTRACTS_PROJECT_PRESENT_BUT_DOMAIN_DOES_NOT_REFERENCE_IT',
  ownershipState: 'correct_except_stable_code_home',
  mustSplitState: 'TWO_W2_CANDIDATES_WALLETDIRECTORY_AND_WALLETDTOS',
  fileCohesionState: 'COHESIVE_BUT_TWO_MULTI_RESPONSIBILITY_FILES',
  oversizedGodFileState: 'WATCH_ONLY_DIRECTORIES_WALLETDIRECTORY_682_LOC_THREE_PORTS_BELOW_800_CEILING',
  localizationState: 'CANONICAL_MECHANISM_ONE_OF_TEN_CLIENT_OBSERVABLE_CODES_RESOURCED_IMPLICIT_LOGICAL_NAMES',
  apiResultPatternState: 'ENDPOINTS_CLEAN_CANONICAL_APIRESPONSEFACTORY_APPLICATION_PARALLEL_MESSAGE_MAPPER',
  stableErrorCodeState: 'MISLOCATED_IN_APPLICATION_ELEVEN_DECLARED_TEN_CATALOGUED_ONE_LOCALIZED_PLUS_SHADOW_FORTY_THREE_KNOWN_CODES',
  declaredCodeGuardState: 'ABSENT',
  typedFaultSeamState: 'ABSENT_PARALLEL_MESSAGE_TEXT_MAPPER_WALLETEXCEPTIONMAPPER',
  rawFaultState: 'SIXTY_THREE_RAW_INVALID_OPERATION_EXCEPTION_LITERALS_FIVE_HANDLERS_MAPPER_MEDIATED',
  legacyBase64CodeState: 'THIRTEEN_BASE64_PERSIAN_SUFFIX_CODES',
  loggingState: 'CANONICAL_ZERO_LOGGER_CALLS_IN_MODULE',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  contractsBoundaryState: 'CLEAN_LEGAL_CONTRACTS_ONLY_SINGLE_EDGE_WALLET_INFRASTRUCTURE_TO_NOTIFICATION_CONTRACTS',
  crossModuleCouplingState: 'NONE_ZERO_FOREIGN_PRODUCTION_EDGES',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_ONE_WALLETDBCONTEXT_SCHEMA_WALLET_FOUR_DBSETS_PLUS_OUTBOX',
  endpointOwnershipStateDetail: 'ELEVEN_MODULE_ROUTES_OVER_V1_CUSTOMER_WALLET_AND_V1_ADMIN_GIFTCARDS_WALLETS',
  hostResidueState: 'ALLOWED_COMPOSITION_SEED_HOST_AND_ADMIN_AUTHORIZER_ONLY_HOST_WALLET_FOLDER_ABSENT',
  schemaMigrationState: 'UNCHANGED_ONE_MIGRATION_20260827180000_INITIALWALLET',
  behaviorPreservationRisk: 'LOW',
  folderGranularityState: 'TECHNICAL_AXIS_FIRST_APPLICATION_PLUS_SINGLE_FILE_REQUEST_LEAVES',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/Wallet/',
  solutionProjectEntries: 6,
  pathNamespaceState: 'EXACT_ZERO_MISMATCHES_OVER_FORTY_NINE_PRODUCTION_FILES',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  rootAllowlistsObserved: {
    'Tooba.Wallet.Contracts': [],
    'Tooba.Wallet.Domain': [],
    'Tooba.Wallet.Application': [],
    'Tooba.Wallet.Infrastructure': [],
    'Tooba.Wallet.Endpoints': ['WalletEndpointModule.cs']
  },
  singleFileRequestLeafState: 'ELEVEN_TECHNICAL_AXIS_LEAVES_W2_FLATTENS',
  technicalAxisFirstState: 'PRESENT',
  infrastructureMigrationDepthState: 'WRONG_DEPTH_INFRASTRUCTURE_MIGRATIONS_EXPECTED_PERSISTENCE_MIGRATIONS',
  structureHandoffState: 'REQUIRED',
  structureHandoffDetail: 'W2 owns: (1) capability-first flatten of Application/{Commands,Queries}/{UseCase} single-file leaves into Customer/ and Admin/ capability folders with shared Composition/, Ports/, Models/ and Validation/; (2) split WalletDtos.cs (public result DTOs vs internal command-shaped inputs) by capability; (3) split WalletDirectory.cs (682 LOC implementing IWalletDirectory + IWalletOrderPaymentPort + IWalletRefundCreditPort) by capability; (4) Infrastructure/Migrations -> Infrastructure/Persistence/Migrations; (5) Domain/ValueObjects stays (WalletCurrency.Normalize moved to Contracts.Errors ownership in W1). Any move must update the Wallet manifest allowlists, WalletArchitectureGuardTests allowlists and the Wallet scoped AMSC guards atomically.',
  canonicalReferenceUsed: 'BuildingBlocks (Result, ApiResponseFactory, IErrorCatalogContributor, IErrorResourceSet, ContractOperationException, AddToobaCqrsFoundation, IClock, IIdGenerator); Support for Contracts/Errors + Application/Composition/<Module>Operation + Validation/ + bilingual resx + Endpoints/Errors + Endpoints/Resources precedent; Returns/Promotion for Contracts-Errors + typed-fault; docs/architecture/32-persian-code-documentation-standard.md.',
  blockers: [
    'B1_STABLE_CODE_HOME_MISLOCATED: Tooba.Wallet.Application.Errors.WalletErrorCodes declares eleven wallet.* codes while Tooba.Wallet.Domain does not reference Tooba.Wallet.Contracts, so the Domain and Infrastructure cannot consume the module stable codes and must throw raw literals.',
    'B2_PARALLEL_MESSAGE_MAPPER: Tooba.Wallet.Application.Errors.WalletExceptionMapper holds a shadow forty-three entry KnownCodes set (thirteen with base64 Persian suffixes) and classifies faults by exact message text, which is a second declaration of stable-code identity.',
    'B3_RAW_FAULT_LITERALS: sixty-three raw InvalidOperationException("<literal>") throws across Domain, Application and Infrastructure carry the machine code in Message instead of a typed code property.',
    'B4_VALIDATOR_MATRIX_GAP: zero AbstractValidator classes for the four caller-controlled malformable requests (RedeemCustomerGiftCardCommand code/idempotency, ListAdminGiftCardsQuery status/q, IssueAdminGiftCardCommand amount/currency, AdjustAdminWalletCommand direction/reason).',
    'B5_LOCALIZATION_HOLLOW: WalletErrors.resx / WalletErrors.fa.resx localize exactly one key (wallet.authorization.unavailable) while ten client-observable codes are catalogued; the remaining nine resolve only through descriptor SafeTitleFallback.',
    'B6_IMPLICIT_LOGICAL_NAMES: Tooba.Wallet.Endpoints.csproj has no explicit EmbeddedResource logical-name lock for WalletErrors.resx / WalletErrors.fa.resx.',
    'B7_STRUCTURE_DIVERGENCE: technical-axis-first Application/Commands|Queries/<UseCase> single-file leaves, Infrastructure/Migrations at the wrong depth, WalletDtos.cs mixing public results with internal command inputs, WalletDirectory.cs mixing customer/admin/payment/refund responsibilities.',
    'B8_NO_AMSC_LINEAGE: no walletAmsc001W0..W3 SoT record, no structureCertified manifest entry, no certificationNote, no AMSC evidence tree, no Master Recovery AMSC checkpoint.',
    'B9_NO_WALLET_SCOPED_AMSC_GUARD: current protections are HostWalletAmcGuardTests, WalletFoundationTests, HostModuleEndpointOwnershipTests and the in-module WalletArchitectureGuardTests; there is no route/request/validator set-equality certification guard for Wallet.'
  ],
  nonBlockingObservations: [
    'HostAdminAccessAmcCertGuardTests / HostAdminCanon003GuardTests / HostAdminCanon009GuardTests pin WalletErrorResourceSet, WalletErrors.resx, WalletErrors.fa.resx, WalletErrorCatalogContributor and WalletAdminAuthorizationCodes to Tooba.Wallet.Endpoints, so W1 keeps the catalog/resource-set/authorization-code surface in Endpoints (the certified Support shape) and must not relocate it to Contracts.',
    'Payment.WalletPaymentGateway catches ContractOperationException generically and returns FailureCode WALLET_SPEND_REJECTED, so retiring the Wallet base64 codes preserves the Payment boundary behaviour.',
    'PaymentPrecertHygieneTests / WalletFinancialCharacterizationTests / WalletCqrsAndHttpContractTests pin the base64 codes (wallet.rejected.2YXZiNis) and the WalletExceptionMapper public surface; W1 must repoint them to the canonical typed codes and the WalletOperation seam.',
    'WalletArchitectureGuardTests asserts Application contains WalletExceptionMapper and Domain does not reference Tooba.Wallet.Contracts; both assertions are inverted by the canonical W1 migration and must be updated atomically.',
    'TmarCompleteReferenceStructureGateTests pins uncertifiedHttpOwningModules and structureLock.certifiedModules; Wallet is the last outstanding uncertified HTTP-owning module and must be removed from the uncertified list and added to certifiedModules by W2/W3.',
    'Wallet is present in completeReferenceModules (12 members) from the earlier HOST-WALLET-AMC-001 evacuation, so the AMSC waves refresh rather than create that record.'
  ],
  noArchitectureDecisionRequired: true,
  hostTouched: false,
  productionCodeChanged: false,
  commit: 'REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER',
  waveOutcome: 'MIGRATE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-WALLET-AMSC-001-W0/analyze.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "walletAmsc001W0": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}
const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w0 = parsed.walletAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.verdict !== 'READY_TO_MIGRATE') {
  throw new Error('walletAmsc001W0 record missing or wrong state');
}

// historical Host-evacuation lineage must be intact
if (!Object.prototype.hasOwnProperty.call(parsed, 'hostWalletAmc')) {
  throw new Error('historical record hostWalletAmc was disturbed');
}
if (parsed.hostWalletAmc.status !== 'CLOSED_HOST_ZERO') {
  throw new Error('Wallet Host-evacuation state was disturbed');
}

// previously accepted AMSC lineage must be intact
if (!parsed.supportAmsc001W3
  || parsed.supportAmsc001W3.state !== 'SUPPORT_AMSC_001_CERTIFIED'
  || parsed.supportAmsc001W3.verdict !== 'COMPLETE_REFERENCE_PATTERN') {
  throw new Error('supportAmsc001W3 record was disturbed');
}
if (!parsed.userPreferenceAmsc001W3R1) {
  throw new Error('userPreferenceAmsc001W3R1 record was disturbed');
}

// structureLock is a repository-global pointer set; Wallet is not a member yet and W0 must not
// touch it. Assert only that it stays exactly as it was observed.
const certified = parsed.structureLock.certifiedModules;
if (!Array.isArray(certified) || certified.length !== 31
  || certified.filter((m) => m === 'Support').length !== 1
  || certified.includes('Wallet')) {
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
if (!content.endsWith('}' + NL)) {
  throw new Error('unexpected trailing bytes');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: walletAmsc001W0)');
