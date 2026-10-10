// TB-TMAR-WISHLIST-AMSC-001-W2 — persist the Structure wave checkpoint into the durable TMAR SoT.
// Additive only: appends the wishlistAmsc001W2 record before the closing brace with a text-anchor
// insertion so the existing file formatting (2-space record indent, CRLF) is preserved and no other
// record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"wishlistAmsc001W2"')) {
  throw new Error('wishlistAmsc001W2 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-WISHLIST-AMSC-001-W2',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-structure',
  target: 'src/backend/Modules/Wishlist/Tooba.Wishlist.*',
  parentTask: 'TB-TMAR-WISHLIST-AMSC-001-W1',
  parentCommit: 'd7fa18f7',
  startingHead: 'd7fa18f7830b6749df0edc29ccd5ee1366f2cf6c',
  state: 'STRUCTURE_COMPLETE',
  verdict: 'READY_FOR_CERTIFY',
  structureState: 'READY_FOR_CERTIFY',
  structureHandoffState: 'READY_FOR_CERTIFY',
  lockVersion: 'ARCH-COMPLETE-002',
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_OWNED_4_ROUTES_HOST_ZERO',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/Wishlist/',
  solutionProjectEntries: 5,
  pathNamespaceState: 'EXACT',
  pathNamespaceDetail: 'Exact path<->namespace for all 27 production .cs files across the five production projects, machine-verified by the W2 guard (0 mismatches). The three W2 namespace repoints are the moved files: WishlistDirectory (Directories -> Persistence), WishlistDevelopmentSeed (Development -> Adapters) and the three split transport validators (Customer.Validators -> Application.Validation).',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  fileCohesionState: 'COHESIVE',
  oversizedGodFileState: 'NONE',
  technicalAxisFirstState: 'ABSENT',
  singleFileRequestLeafState: 'NONE',
  applicationPrimaryAxis: 'CAPABILITY_FIRST_CUSTOMER_PLUS_SHARED_COMPOSITION_MODELS_PORTS_VALIDATION',
  domainStructureState: 'CANONICAL_AGGREGATES',
  contractsStructureState: 'CANONICAL_ERRORS_PORTS_RESOURCES',
  endpointsStructureState: 'CANONICAL_COMPOSITION_ROOT_PLUS_CUSTOMER_AUDIENCE_FOLDER',
  solutionGroupingDetail: 'The canonical <Folder Name="/Modules/Wishlist/"> group carries exactly the five production projects (Domain, Contracts, Application, Infrastructure, Endpoints). No module-local Tooba.Wishlist.Tests project exists on disk, so no solution entry may be invented; the module durable guards live in Tooba.Host.Tests/Architecture.',
  retiredTechnicalFolders: [
    'Infrastructure/Directories/ — exactly one implementation file (WishlistDirectory.cs) in a technical-axis folder; the type moved next to its own WishlistDbContext under Infrastructure/Persistence/ with the path-exact namespace Tooba.Wishlist.Infrastructure.Persistence.',
    'Infrastructure/Development/ — exactly one file (WishlistDevelopmentSeed.cs); moved to Infrastructure/Adapters/ (namespace Tooba.Wishlist.Infrastructure.Adapters), the module adapter home that mirrors the certified Support/Wallet shape, so no invented ceremony folder is created.',
    'Application/Customer/Validators/ — the merged three-validator bundle (WishlistValidators.cs) was split into one cohesive file per request under the shared Application/Validation/ seam, matching the certified Support/AddressBook/Payment precedent (a module-wide transport axis rather than a per-capability folder).'
  ],
  cohesionDetail: 'No file is oversized or multi-responsibility. The largest production file is the single WishlistDirectory implementation (90 LOC) which is cohesive around one aggregate + two narrow ports, so no god-file split is required (contrast Wallet, whose 682-LOC four-capability WalletDirectory needed partials). The three split validator files are each single-request and < 25 LOC. Splitting the directory into capability partials would be cosmetic here and is explicitly not done (skill section 9/12: do not split cosmetically to game size guards).',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_AND_MODULE_OWNED_DEVELOPMENT_SEED_ADAPTER_ONLY',
  hostFinalClosureState: 'PRESERVED',
  hostFinalClosureDetail: 'Zero Host production file added, moved or removed. The single Host edit is a using repoint in Host/Development/DevelopmentSchemaMigrator.cs (Tooba.Wishlist.Infrastructure.Development -> Tooba.Wishlist.Infrastructure.Adapters) — a mechanical consequence of the module-internal Adapters move, no behavior change. HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED and HOST_ROOT_FINAL_CERTIFIED are untouched.',
  crossModuleCouplingState: 'NONE',
  crossModuleCouplingDetail: 'Unchanged: zero foreign Application/Infrastructure/Domain edge and zero foreign *DbContext in any Wishlist production file. The only foreign references remain the Contracts-only Tooba.Catalog.Contracts lookup seams and the Tooba.Order.Contracts.Fulfillment session seam.',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_ONE_WISHLISTDBCONTEXT_SCHEMA_WISHLIST_ONE_TABLE_PLUS_OUTBOX',
  schemaMigrationState: 'UNCHANGED',
  schemaMigrationDetail: 'The single migration 20260825162434_InitialWishlist (+ designer + snapshot) is byte-identical; no table, column, index, constraint, migration id or Up/Down semantics changed. The EF model surface has no dependency on the moved namespaces.',
  behaviorPreservation: 'PRESERVED',
  behaviorPreservationDetail: 'Pure physical reorganization plus namespace/using repointing plus one behavior-neutral file split of a merged validator bundle into three files declaring the same three validator types with identical rules and identical stable error codes. The 4 routes/verbs/group prefix/templates, the success response shapes, the three wire-visible outcome codes, the domain invariants, the authorization and actor-resolution order, the wishlist schema and its single migration, the outbox Translate/ResolveEventClrType/Schema/TableName, the DI lifetimes and the declared stable codes/resources are all unchanged.',
  validatorCoverageState: 'EXHAUSTIVE_3_OF_3_REQUIRED_PLUS_1_NO_VALIDATOR_REQUIRED',
  validatorCoverageDetail: 'W0/W1 semantics preserved through the physical move: the three VALIDATOR_REQUIRED requests keep exactly one validator each (now one file each) and ListWishlistPageQuery stays validator-free. The W1 non-circular AddToobaCqrsFoundation discoverability proof still passes unchanged.',
  cqrsState: 'COMPLIANT_REAL_IREQUEST_IREQUESTHANDLER_ISENDER_MEDIATR_12_5',
  endpointReachableRequests: 4,
  manifestMutation: 'WISHLIST_ENTRY_GAINS_STRUCTURE_AUTHORITY_KEYS_AND_DISK_ACCURATE_FORBIDDEN_LISTS; structureCertified NOT flipped (already true from the prior AMC lineage); Wishlist NOT re-added to structureLock.certifiedModules (already a member, 32 total)',
  structureAuthorityTask: 'TB-TMAR-WISHLIST-AMSC-001-W2',
  guardTest: 'src/backend/Host/Tooba.Host.Tests/Architecture/WishlistModuleAmsc001W2StructureGuardTests.cs',
  guardTestResult: '8_OF_8_PASSED',
  guardUpdates: [
    'HostWishlistAmcGuardTests: seed path Development -> Adapters, directory path Directories -> Persistence, seed namespace Development -> Adapters (path/namespace repoints only, no assertion weakened or deleted).',
    'WishlistModuleAmcW2StructureGuardTests: validator path Customer/Validators/WishlistValidators.cs -> the three Validation/*Validator.cs files; directory path Directories -> Persistence.',
    'WishlistModuleAmcW3CqrsGuardTests: validator bundle read -> the three Validation/*Validator.cs files concatenated; the same WishlistErrorCodes/no-prose assertions are kept.',
    'WishlistModuleAmcW4CertGuardTests: validator folder Customer/Validators -> Validation; Infrastructure Adapters present and Directories/Development absent; directory path Directories -> Persistence.',
    'WishlistModuleAmsc001W1MigrateGuardTests: the W1 validator fact now locates the three cohesive files under Validation/ (count 3, no WithMessage, stable codes) — same semantics, stronger file-cohesion assertion.'
  ],
  guardWeakening: 'NONE',
  wishlistTestsResult: '28_PASSED_1_SKIPPED_0_FAILED (all Host.Tests Wishlist* guards + foundation tests)',
  hostTestsBuildResult: 'SUCCEEDED_0_ERRORS',
  hostBuildResult: 'SUCCEEDED_0_ERRORS',
  knownBaselineFailures: 'TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment remains RED for the documented pre-existing Tooba.Catalog.Contracts/Cart project-level namespace deviation, which pre-dates and is unrelated to Wishlist (Wishlist is absent from that failure message); it is unchanged by W2.',
  microserviceExtractable: 'TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE_EXACT_PATH_NAMESPACE',
  jsonParseState: 'PASS',
  productionCodeChanged: true,
  hostProductionCodeChanged: false,
  schemaChange: 'NONE',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  evidenceRoot: 'docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W2/',
  workflowStop: 'USER_REVIEW_WISHLIST_AMSC_001_W2',
  automaticNextImplementationTask: 'NONE',
  evidence: 'docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W2/structure.md',
  commit: 'REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "wishlistAmsc001W2": ${line}` : `  ${line}`))
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

const w2 = parsed.wishlistAmsc001W2;
if (!w2 || w2.state !== 'STRUCTURE_COMPLETE' || w2.verdict !== 'READY_FOR_CERTIFY') {
  throw new Error('wishlistAmsc001W2 record missing or wrong state');
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: wishlistAmsc001W2)');
