// TB-TMAR-WISHLIST-AMSC-001-W1 — persist the Migrate wave checkpoint into the durable TMAR SoT.
// Additive only: appends the wishlistAmsc001W1 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"wishlistAmsc001W1"')) {
  throw new Error('wishlistAmsc001W1 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-WISHLIST-AMSC-001-W1',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-migrate',
  target: 'src/backend/Modules/Wishlist/Tooba.Wishlist.*',
  parentTask: 'TB-TMAR-WISHLIST-AMSC-001-W0',
  parentCommit: 'e80045ca',
  startingHead: 'e80045ca711a25972858991aa80b8831a50fae1c',
  state: 'MIGRATE_COMPLETE',
  verdict: 'READY_TO_STRUCTURE',
  structureHandoffState: 'REQUIRED',
  lockVersion: 'ARCH-COMPLETE-002',
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED_4_ROUTES_HOST_ZERO',
  stableCodeHome: 'src/backend/Modules/Wishlist/Tooba.Wishlist.Contracts/Errors/WishlistErrorCodes.cs',
  stableCodeNamespace: 'Tooba.Wishlist.Contracts.Errors',
  declaredConstantCount: 5,
  declaredCodeCount: 5,
  wishlistOwnedCodeCount: 4,
  foundationSharedCodeCount: 1,
  registeredDescriptorCount: 4,
  localizedEnCount: 4,
  localizedFaCount: 4,
  declaredCodeGuard: 'KnownCodes HashSet<string> (Ordinal) + IsKnown(string?) — null/empty/whitespace-safe',
  declaredCodeGuardExclusion: 'customer.session.required (SessionRequired) is Foundation-owned, consumable by the HTTP boundary, deliberately excluded from KnownCodes so it can never be surfaced as a Wishlist use-case fault.',
  codeIdentityPreservation: 'The three pre-existing client-observable codes (customer.wishlist.product_unavailable, customer.wishlist.product_id_required, customer.wishlist.product_ids_required) keep their exact wire values. The single raw InvalidOperationException("Wishlist integration event is not registered.") literal on the outbox path is re-expressed as the declared machine-shaped code wishlist.outbox.unmapped_event_type. That path is unreachable through the four HTTP routes, so no client-visible response changes.',
  newDeclaredCode: 'wishlist.outbox.unmapped_event_type (Platform, HTTP 500, EN+FA localized, registered exactly once)',
  typedFaultSeam: 'src/backend/Modules/Wishlist/Tooba.Wishlist.Application/Composition/WishlistOperation.cs',
  typedFaultMechanisms: 'ContractOperationException_via_WishlistErrorCodes.IsKnown + SemanticException_via_Error',
  typedFaultOverloads: 'Task<Result<T>> ExecuteAsync<T>(Func<Task<T>>) + Task<Result> ExecuteAsync(Func<Task>) — both arms present on both overloads, ArgumentNullException.ThrowIfNull added',
  messageClassification: 'ZERO',
  rawFaultState: 'ZERO_RAW_INVALID_OPERATION_EXCEPTION_LITERALS',
  rawFaultDetail: 'WishlistOutboxRegistration.GetEventTypeName now throws ContractOperationException(WishlistErrorCodes.OutboxUnmappedEventType). Translate(...) and ResolveEventClrType(...) still return null, so outbox observability and the message-bus surface are unchanged. No raw InvalidOperationException("<literal>") remains in any Wishlist production file.',
  catalogOwnershipState: 'FOUR_DESCRIPTORS_REGISTERED_EXACTLY_ONCE',
  catalogDetail: 'WishlistErrorCatalogContributor registers the four Wishlist-owned codes exactly once (ProductUnavailable, ProductIdRequired, ProductIdsRequired, OutboxUnmappedEventType) and deliberately does not register SessionRequired, whose descriptor is Foundation-owned.',
  localizationState: 'CANONICAL_4_EN_4_FA_EXPLICIT_LOCKED_LOGICAL_NAMES',
  localizationDetail: 'WishlistErrors.resx/.fa.resx carry 4 EN + 4 FA distinct keys == the 4 Wishlist-owned declared codes (no orphan key, no unlocalized owned code). Tooba.Wishlist.Contracts.csproj pins the explicit EmbeddedResource + LogicalName pair (Tooba.Wishlist.Contracts.Resources.WishlistErrors.resources and .fa.resources) so the bilingual logical names are locked rather than left to SDK convention.',
  resourceSetOwnership: 'PREFIX_BASED_CUSTOMER_WISHLIST_AND_WISHLIST_KEYSPACES',
  resourceSetOwnershipDetail: 'WishlistErrorResourceSet.Owns covers customer.wishlist.* and wishlist.*, the two Wishlist-owned keyspaces; SessionRequired and every foreign code are not owned. Resolving by convention would silently break after an assembly rename, which is why the LogicalName lock matters for microservice extraction.',
  presentationRegistration: 'EXACTLY_ONCE_IN_TOOBA_WISHLIST_ENDPOINTS_OR_CONTRACTS_COMPOSITION (IErrorCatalogContributor + IErrorResourceSet)',
  endpointSurfaceLocationConstraint: 'WishlistErrorCodes, WishlistErrorCatalogContributor, WishlistErrorResourceSet and WishlistErrors.resx/.fa.resx stay in Tooba.Wishlist.Contracts because WishlistModuleAmcW1/W2/W3/W4CertGuardTests, HostWishlistAmcGuardTests and WishlistFoundationTests pin that location. Recorded in W0 section 18 as an architecture constraint, not a preference.',
  cqrsState: 'COMPLIANT_REAL_IREQUEST_IREQUESTHANDLER_ISENDER_MEDIATR_12_5',
  endpointReachableRequests: 4,
  validatorRequiredCount: 3,
  validatorsPresentCount: 3,
  noValidatorRequiredCount: 1,
  validatorCoverageState: 'EXHAUSTIVE_3_OF_3_REQUIRED_PLUS_1_NO_VALIDATOR_REQUIRED',
  validatorDetail: 'AddWishlistItemCommandValidator, RemoveWishlistItemCommandValidator and GetWishlistMembershipQueryValidator were already present and stay in place; they emit WishlistErrorCodes.* machine codes with zero WithMessage prose. ListWishlistPageQuery stays validator-free for its W0-recorded reason (server-derived paging/actor, no caller-controlled input). The W1 guard proves the three validators resolve from a real AddToobaCqrsFoundation container and that the exempt request resolves to null (non-circular).',
  contractsBoundaryState: 'CLEAN',
  crossModuleCouplingState: 'NONE',
  crossModuleCouplingDetail: 'Unchanged and correct: zero foreign Application/Infrastructure/Domain using-directive and zero foreign *DbContext reference in any Wishlist production file. Tooba.Wishlist.Domain references BuildingBlocks + its own Tooba.Wishlist.Contracts. Tooba.Wishlist.Endpoints references Wishlist.Application + BuildingBlocks only (never Wishlist.Infrastructure, never Tooba.Host). The customer endpoint consumes WishlistErrorCodes.SessionRequired; no Wishlist-owned duplicate of the session code exists.',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_ONE_WISHLISTDBCONTEXT_SCHEMA_WISHLIST_ONE_TABLE_PLUS_OUTBOX',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_AND_MODULE_OWNED_DEVELOPMENT_SEED_ADAPTER_ONLY',
  loggingState: 'CANONICAL_ZERO_CALL_SITES',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  schemaMigrationState: 'UNCHANGED',
  schemaMigrationDetail: 'The single migration 20260825162434_InitialWishlist (migration + designer) is untouched; no snapshot or DbContext file changed.',
  behaviorPreservation: 'PRESERVED',
  behaviorPreservationDetail: 'The 4 routes/verbs/group prefix /v1/customer/wishlist/templates, the success response shapes, the three wire-visible outcome codes, the domain invariants, the authorization and actor-resolution order, the wishlist schema and its single migration, the outbox Translate/ResolveEventClrType/Schema/TableName and the DI lifetimes are all unchanged. The only delta is that the previously untyped outbox fault now carries a declared code instead of prose — a path unreachable through the HTTP surface.',
  guardTest: 'src/backend/Host/Tooba.Host.Tests/Architecture/WishlistModuleAmsc001W1MigrateGuardTests.cs',
  guardTestResult: '11_OF_11_PASSED',
  guardTestDesign: 'Assertions locate files by name (not by technical-axis path) and never assert absolute file counts, so the W2 structure wave can folder and split the module without weakening this guard.',
  guardUpdates: [
    'No existing Wishlist guard needed weakening: W1 is additive (new code constant, new catalog descriptor, new resource keys, new catch arm, typed replacement of one literal).',
    'WishlistModuleAmsc001W1MigrateGuardTests.cs is new and is the durable W1 lineage guard.'
  ],
  focusedGuardResult: '28_PASSED_1_SKIPPED_0_FAILED (all Host.Tests Wishlist* guards + foundation tests)',
  hostTestsBuildResult: 'SUCCEEDED_0_ERRORS',
  microserviceExtractable: 'TRUE_PENDING_W2_STRUCTURE',
  jsonParseState: 'PASS',
  productionCodeChanged: true,
  hostProductionCodeChanged: false,
  manifestMutation: 'NONE',
  schemaChange: 'NONE',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  evidenceRoot: 'docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W1/',
  workflowStop: 'USER_REVIEW_WISHLIST_AMSC_001_W1',
  automaticNextImplementationTask: 'NONE',
  evidence: 'docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W1/migrate.md',
  commit: 'REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "wishlistAmsc001W1": ${line}` : `  ${line}`))
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

const w1 = parsed.wishlistAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.verdict !== 'READY_TO_STRUCTURE') {
  throw new Error('wishlistAmsc001W1 record missing or wrong state');
}
if (parsed.wishlistAmsc001W0.state !== 'ANALYZE_COMPLETE'
  || parsed.wishlistAmsc001W0.verdict !== 'READY_TO_MIGRATE') {
  throw new Error('wishlistAmsc001W0 record was disturbed');
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

// structureLock is a repository-global pointer set; Wishlist is already a member and W1 must not
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: wishlistAmsc001W1)');
