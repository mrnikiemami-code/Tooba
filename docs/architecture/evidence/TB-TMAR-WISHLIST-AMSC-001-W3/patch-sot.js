// TB-TMAR-WISHLIST-AMSC-001-W3 — persist the Certify wave checkpoint into the durable TMAR SoT.
// Additive only: appends the wishlistAmsc001W3 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"wishlistAmsc001W3"')) {
  throw new Error('wishlistAmsc001W3 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-WISHLIST-AMSC-001-W3',
  parentTask: 'TB-TMAR-WISHLIST-AMSC-001-W2',
  mode: 'FRESH_ARCH_COMPLETE_002_CERTIFY_AFTER_MIGRATE_AND_STRUCTURE',
  skill: 'tooba-architecture-certify',
  target: 'src/backend/Modules/Wishlist/Tooba.Wishlist.*',
  startingHead: '03871df68d3204214680afec9aae7992dc481b2e',
  authorityAncestry: 'W0 e80045ca (analyze) -> W1 d7fa18f7 (migrate) -> W2 03871df6 (structure) -> W3 (this certify wave).',
  state: 'WISHLIST_AMSC_001_CERTIFIED',
  verdict: 'COMPLETE_REFERENCE_PATTERN',
  lockVersion: 'ARCH-COMPLETE-002',
  structureCertified: true,
  currentStructureAuthority: 'TB-TMAR-WISHLIST-AMSC-001-W2',
  currentStructureCommit: '03871df68d3204214680afec9aae7992dc481b2e',
  currentStructureState: 'READY_FOR_CERTIFY_CONSUMED_BY_W3',
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED_4_ROUTES_HOST_ZERO',
  moduleOwnedRouteCount: 4,
  hostOwnedRouteCount: 0,
  routeSurface: '/v1/customer/wishlist x4 (GET list, POST /{productId:guid}, DELETE /{productId:guid}, POST /membership).',
  routeGroupPrefix: '/v1/customer/wishlist',
  endpointReachableRequests: 4,
  cqrsState: 'MEDIATR_12_5_ISENDER_4_OF_4',
  validatorMatrixState: 'EXHAUSTIVE_3_VALIDATOR_REQUIRED_1_NO_VALIDATOR_REQUIRED',
  validatorRequiredCount: 3,
  noValidatorRequiredCount: 1,
  validatorRequiredRequests: 'AddWishlistItemCommand,RemoveWishlistItemCommand,GetWishlistMembershipQuery',
  noValidatorRequiredRequests: 'ListWishlistPageQuery',
  noValidatorRequiredReason: 'ListWishlistPageQuery binds no client-controlled transport shape: its single argument is the server-derived actor resolved by the module actor-resolver seam and the route carries no parameter. The two :guid routes are routing-constrained so a malformed id never reaches a handler; the membership body collection carries a validator and is additionally clamped at the endpoint.',
  validatorDiscoveryState: 'DISCOVERABLE_AND_EXECUTED',
  routeRequestSetEqualityState: 'EXACT_SOURCE_DERIVED_4_OF_4_ENFORCED',
  apiResultMappingState: 'CANONICAL_APIRESPONSEFACTORY',
  typedFaultState: 'WISHLISTOPERATION_TYPED_CODE_ONLY',
  declaredCodeCount: 5,
  wishlistOwnedCodeCount: 4,
  foundationSharedCodeCount: 1,
  registeredDescriptorCount: 4,
  localizedEnCount: 4,
  localizedFaCount: 4,
  messageClassification: 'ZERO',
  rawFaultState: 'ZERO_RAW_INVALID_OPERATION_EXCEPTION_LITERALS',
  contractsBoundaryState: 'CLEAN',
  foreignApplicationInfrastructureDomainEdges: 'ZERO',
  crossModuleJoinState: 'ZERO',
  schemaState: 'UNCHANGED',
  behaviorPreservation: 'PRESERVED',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  pathNamespaceState: 'EXACT',
  rootAllowlistState: 'ENFORCED',
  aliasWorkaroundState: 'NONE',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  physicalCopyState: 'CLEAN',
  manifestCertificationState: 'WISHLIST_CERTIFIED_MODULES_ENTRY_REFRESHED_IN_PLACE',
  structureLockState: 'WISHLIST_PRESENT_EXACTLY_ONCE',
  certifiedModulesCount: 32,
  microserviceExtractable: true,
  microserviceExtractableState: 'TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE_EXACT_PATH_NAMESPACE',
  hostFinalClosureState: 'HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED',
  hostAuthorityState: 'ZERO_BUSINESS_ZERO_HTTP',
  durableGuard: 'src/backend/Host/Tooba.Host.Tests/Architecture/WishlistModuleAmsc001W3CertGuardTests.cs',
  evidence: 'docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W3/certification.md',
  workflowStop: 'USER_REVIEW_WISHLIST_AMSC_001_W3',
  automaticNextImplementationTask: 'NONE'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "wishlistAmsc001W3": ${line}` : `  ${line}`))
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

const w3 = parsed.wishlistAmsc001W3;
if (!w3 || w3.state !== 'WISHLIST_AMSC_001_CERTIFIED' || w3.verdict !== 'COMPLETE_REFERENCE_PATTERN') {
  throw new Error('wishlistAmsc001W3 record missing or wrong state');
}
if (parsed.wishlistAmsc001W2.state !== 'STRUCTURE_COMPLETE') {
  throw new Error('wishlistAmsc001W2 record was disturbed');
}
if (parsed.wishlistAmsc001W1.state !== 'MIGRATE_COMPLETE') {
  throw new Error('wishlistAmsc001W1 record was disturbed');
}
if (parsed.wishlistAmsc001W0.state !== 'ANALYZE_COMPLETE') {
  throw new Error('wishlistAmsc001W0 record was disturbed');
}
if (parsed.wishlistAmc001.state !== 'COMPLETE_REFERENCE_PATTERN') {
  throw new Error('historical record wishlistAmc001 was disturbed');
}
if (!parsed.walletAmsc001W3 || parsed.walletAmsc001W3.state !== 'WALLET_AMSC_001_CERTIFIED') {
  throw new Error('walletAmsc001W3 record was disturbed');
}
if (!parsed.userPreferenceAmsc001W3R1) {
  throw new Error('userPreferenceAmsc001W3R1 record was disturbed');
}

const certified = parsed.structureLock.certifiedModules;
if (!Array.isArray(certified) || certified.length !== 32
  || certified.filter((m) => m === 'Wishlist').length !== 1) {
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: wishlistAmsc001W3)');
