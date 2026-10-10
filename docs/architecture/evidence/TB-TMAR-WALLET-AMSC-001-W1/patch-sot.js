// TB-TMAR-WALLET-AMSC-001-W1 — persist the Migrate wave checkpoint into the durable TMAR SoT.
// Additive only: appends the walletAmsc001W1 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"walletAmsc001W1"')) {
  throw new Error('walletAmsc001W1 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-WALLET-AMSC-001-W1',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-migrate',
  target: 'src/backend/Modules/Wallet/Tooba.Wallet.*',
  parentTask: 'TB-TMAR-WALLET-AMSC-001-W0',
  parentCommit: '7ab117fb',
  startingHead: '7ab117fb',
  state: 'MIGRATE_COMPLETE',
  verdict: 'READY_TO_STRUCTURE',
  structureHandoffState: 'REQUIRED',
  lockVersion: 'ARCH-COMPLETE-002',
  stableCodeHome: 'src/backend/Modules/Wallet/Tooba.Wallet.Contracts/Errors/WalletErrorCodes.cs',
  stableCodeNamespace: 'Tooba.Wallet.Contracts.Errors',
  declaredConstantCount: 50,
  declaredCodeCount: 50,
  httpReachableCount: 10,
  domainInvariantCount: 40,
  registeredDescriptorCount: 10,
  localizedEnCount: 50,
  localizedFaCount: 50,
  declaredCodeGuard: 'HttpReachableCodes/DomainInvariantCodes HashSets + IsKnown/IsHttpReachable/IsDomainInvariant',
  codeIdentityPreservation: 'The ten pre-existing client-observable codes (wallet.rejected, wallet.redeem.rejected, giftcard.rejected, giftcard.issue.rejected, giftcard.revoke.rejected, giftcard.missing, wallet.missing, wallet.adjust.rejected, wallet.authorization.unavailable, wallet.demo.not_ready) keep their exact wire values. The thirteen legacy base64 Persian suffix codes and every raw InvalidOperationException("<literal>") fault are retired and re-expressed as declared machine-shaped invariant codes that map onto the stable public outcome code of the owning use case, so no client-visible code is added and the existing response shape is preserved byte-for-byte.',
  foreignOwnedCodeRespected: 'customer.session.required stays Foundation-owned (FoundationErrorCatalogContributor). WalletErrorCodes deliberately does not declare it and the customer endpoint consumes FoundationErrorCodes.CustomerSessionRequired directly, matching Support/Returns.',
  typedFaultSeam: 'src/backend/Modules/Wallet/Tooba.Wallet.Application/Composition/WalletOperation.cs',
  typedFaultMechanisms: 'ContractOperationException_via_IsKnown + SemanticException_via_Error',
  typedFaultOverloads: 'Task<Result<T>> ExecuteAsync<T>(Func<Task<T>>, string?) + Task<Result> ExecuteAsync(Func<Task>, string?) + Result<T> NotFoundIfNull<T>(T?, string) + SemanticError ToSemanticError(ContractOperationException, string?)',
  messageClassification: 'ZERO',
  rawFaultState: 'ZERO_RAW_INVALID_OPERATION_EXCEPTION_LITERALS',
  rawFaultDetail: 'Every former literal is now ContractOperationException(WalletErrorCodes.<Constant>) in Domain/Aggregates (WalletAccount, WalletLedgerEntry, GiftCard, GiftCardRedemption), Infrastructure/Directories/WalletDirectory, Infrastructure/DependencyInjection/WalletModule.GetEventTypeName and Application/Models/WalletEnumParsing. Translate(...) and ResolveEventClrType(...) stay null so the outbox observable behavior is unchanged. WalletCurrency.Normalize and WalletAccount.NormalizeCurrency now throw CurrencyRequired/CurrencyInvalid typed faults with the same code identity as before.',
  legacyBase64CodeState: 'ZERO',
  legacyMapperState: 'RETIRED_APPLICATION_ERRORS_FOLDER_DELETED',
  localizationState: 'CANONICAL_50_EN_50_FA_EXPLICIT_LOCKED_LOGICAL_NAMES',
  localizationDetail: 'WalletErrors.resx/.fa.resx carry 50 EN + 50 FA distinct keys == the 50 declared codes. WalletErrorCatalogContributor registers the 10 client-observable descriptors exactly once plus WalletAdminAuthorizationCodes.AuthorizationUnavailable; no domain-only invariant is catalogued. Tooba.Wallet.Endpoints.csproj pins the explicit EmbeddedResource + LogicalName pair (Tooba.Wallet.Endpoints.Resources.WalletErrors.resources and .fa.resources) so the bilingual logical names are locked rather than left to EmbeddedResourceUseDependentUponConvention.',
  resourceSetOwnership: 'PREFIX_BASED_WALLET_AND_GIFTCARD_KEYSPACES',
  resourceSetOwnershipDetail: 'WalletErrorResourceSet.Owns now covers wallet.* and giftcard.*, the two Wallet-owned keyspaces. Repository-wide scan proves both are collision-free: the only other giftcard.* strings are the AccessControl permission ids giftcard.view/giftcard.manage, which never enter the error-localization pipeline.',
  presentationRegistration: 'EXACTLY_ONCE_IN_TOOBA_WALLET_ENDPOINTS (services.AddSingleton<IErrorCatalogContributor, WalletErrorCatalogContributor>() + services.AddSingleton<IErrorResourceSet, WalletErrorResourceSet>() in WalletEndpointModule.AddWalletEndpointPresentation)',
  endpointSurfaceLocationConstraint: 'WalletErrorCatalogContributor, WalletErrorResourceSet, WalletErrors.resx/.fa.resx and WalletAdminAuthorizationCodes stay in Tooba.Wallet.Endpoints because HostAdminAccessAmcCertGuardTests/HostAdminCanon003GuardTests/HostAdminCanon009GuardTests pin that location (the certified Support shape). Recorded in W0 section 18 as an architecture constraint, not a preference.',
  cqrsState: 'COMPLIANT_11_OF_11_HANDLERS_ROUTE_THROUGH_WALLETOPERATION',
  endpointReachableRequests: 11,
  validatorRequiredCount: 4,
  validatorsPresentCount: 4,
  noValidatorRequiredCount: 7,
  validatorCoverageState: 'EXHAUSTIVE_11_OF_11',
  validatorDetail: 'Four transport validators added (RedeemCustomerGiftCardCommand, IssueAdminGiftCardCommand, ListAdminGiftCardsQuery, AdjustAdminWalletCommand) emitting wallet.validation.* machine codes with zero WithMessage prose. The seven NO_VALIDATOR_REQUIRED requests stay validator-free for their W0-recorded reasons (:guid route constraint, server-derived actor, executed canonical clamp, or Development-gated parameterless).',
  contractsBoundaryState: 'CLEAN',
  crossModuleCouplingState: 'NONE',
  crossModuleCouplingDetail: 'Unchanged and correct: zero foreign Application/Infrastructure/Domain reference in any Wallet production file. The single legal edge stays Tooba.Wallet.Infrastructure -> Tooba.Notification.Contracts. Tooba.Wallet.Domain now references Tooba.Wallet.Contracts (its own module Contracts) in addition to BuildingBlocks. Tooba.Wallet.Endpoints references Wallet.Application + BuildingBlocks only. Host is touched only at the composition root and no Host production file was modified by W1.',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_ONE_WALLETDBCONTEXT_SCHEMA_WALLET_FOUR_DBSETS_PLUS_OUTBOX',
  endpointOwnershipState: 'MODULE_OWNED_11_ROUTES_HOST_ZERO',
  hostResidueState: 'ALLOWED_COMPOSITION_SEED_HOST_AND_ADMIN_ADAPTER_ONLY',
  loggingState: 'CANONICAL_ZERO_CALL_SITES',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  schemaMigrationState: 'UNCHANGED',
  behaviorPreservation: 'PRESERVED',
  behaviorPreservationDetail: 'The 11 routes/verbs/group prefixes/templates, the nine success response shapes plus {}, the ten wire-visible outcome codes, the domain invariants (currency length/trim/upper, Guid.Empty rejection, positive amounts, idempotency, metadata cap), the authorization and actor-resolution order, the wallet schema and the single migration 20260827180000_InitialWallet, the outbox Translate/ResolveEventClrType/Schema/TableName and the DI lifetimes are unchanged. No migration, designer, snapshot or DbContext file was touched. The only intentional wire delta is the invalid-input envelope of the four VALIDATOR_REQUIRED requests, which moves from a mapped business 400 to the canonical validation.failed 400 (the accepted AMSC transport-validation contract).',
  guardTest: 'src/backend/Host/Tooba.Host.Tests/Architecture/WalletModuleAmsc001W1MigrateGuardTests.cs',
  guardTestResult: '9_OF_9_PASSED',
  guardUpdates: [
    'WalletArchitectureGuardTests: Application allowlist Errors -> Composition + Validation; Domain/Contracts assertion inverted to require the Contracts reference; WalletExceptionMapper positive assertion replaced by WalletOperation plus negative message-text assertions; localized-prose filter widened.',
    'WalletCqrsAndHttpContractTests: WalletSemanticPresentationTests repointed from WalletExceptionMapper/InvalidOperationException to WalletOperation/ContractOperationException, plus a new assertion that a client-observable typed code is surfaced as-is.',
    'WalletFinancialCharacterizationTests: wallet.rejected.2YXZiNis -> WalletErrorCodes.BalanceInsufficient.',
    'NotificationContractsArchitectureGuardTests: PRE-EXISTING STALENESS REPAIRED — the guard was RED at HEAD because it predates commit c660b933 (the Notification AMSC W1 that added Notification.Contracts/Errors + Resources and the BuildingBlocks reference). Allowlist gains Errors/Resources and Assert.Empty(refs) becomes the canonical no-implementation-project-reference assertion. This is not a weakening: the guard now asserts the same canonical Contracts rule every certified module Contracts project satisfies.',
    'WalletCurrencyContractsTests (Host.Tests): InvalidOperationException + message-string assertions -> ContractOperationException + WalletErrorCodes.CurrencyRequired/CurrencyInvalid.',
    'Tooba.Host.Tests.csproj: adds Wallet.Application + Wallet.Endpoints project references so the new guard can consume the public surfaces.'
  ],
  focusedGuardResult: '64_PASSED_1_SKIPPED_0_FAILED (Wallet* + HostAdminAccessAmcCertGuard + HostAdminCanon003 + HostAdminCanon009 + ErrorCatalogUniqueCodeGuard + PaymentPrecertHygiene)',
  walletModuleTestResult: '24_PASSED_0_FAILED',
  paymentModuleTestResult: '107_PASSED_0_FAILED',
  solutionBuildResult: 'SUCCEEDED_0_ERRORS',
  microserviceExtractable: 'TRUE_PENDING_W2_STRUCTURE',
  jsonParseState: 'PASS',
  productionCodeChanged: true,
  manifestMutation: 'NONE',
  schemaChange: 'NONE',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  evidenceRoot: 'docs/architecture/evidence/TB-TMAR-WALLET-AMSC-001-W1/',
  workflowStop: 'USER_REVIEW_WALLET_AMSC_001_W1',
  automaticNextImplementationTask: 'NONE',
  evidence: 'docs/architecture/evidence/TB-TMAR-WALLET-AMSC-001-W1/migrate.md',
  commit: 'REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "walletAmsc001W1": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}
const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w1 = parsed.walletAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.verdict !== 'READY_TO_STRUCTURE') {
  throw new Error('walletAmsc001W1 record missing or wrong state');
}
if (parsed.walletAmsc001W0.state !== 'ANALYZE_COMPLETE') {
  throw new Error('walletAmsc001W0 record was disturbed');
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

// structureLock is a repository-global pointer set; Wallet is not a member yet and W1 must not
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: walletAmsc001W1)');
