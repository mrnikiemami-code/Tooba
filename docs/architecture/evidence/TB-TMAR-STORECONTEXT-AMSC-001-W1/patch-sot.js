// TB-TMAR-STORECONTEXT-AMSC-001-W1 — persist the Migrate wave checkpoint into the durable TMAR SoT.
// Additive: appends the storeContextAmsc001W1 record and reconciles the W0 placeholder commit to the
// real W0 SHA. Text-anchor insertion preserves the existing 2-space record indent and CRLF endings.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const W0_COMMIT = 'c73545f547d970745cbcb0e5c9fc634aff3c96ae';
const W1_STARTING_HEAD = W0_COMMIT;

if (original.includes('"storeContextAmsc001W1"')) {
  throw new Error('storeContextAmsc001W1 already present — refusing to overwrite');
}
if (!original.includes('"storeContextAmsc001W0"')) {
  throw new Error('storeContextAmsc001W0 record missing — W0 must land first');
}
if (original.split('"commit": "PENDING_W0_COMMIT"').length !== 2) {
  throw new Error('expected exactly one PENDING_W0_COMMIT placeholder');
}

const record = {
  task: 'TB-TMAR-STORECONTEXT-AMSC-001-W1',
  parentTask: 'TB-TMAR-STORECONTEXT-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-migrate',
  target: 'src/backend/Modules/StoreContext/Tooba.StoreContext.*',
  startingHead: W1_STARTING_HEAD,
  state: 'MIGRATE_COMPLETE',
  verdict: 'READY_FOR_CERTIFICATION',
  lockVersion: 'ARCH-COMPLETE-002',
  ownershipMovesState: 'ZERO_NO_RESPONSIBILITY_WAS_MISPLACED',
  behaviorChange: 'NONE_DOCUMENTATION_ONLY',
  migratedSurface: 'DOCUMENTATION_STANDARD_ONLY',
  migrationScope: 'Every public member of the three StoreContext production files carried English XML while docs/architecture/32-persian-code-documentation-standard.md (Architect-accepted, COMPLETE) requires strong professional Persian documentation and explicitly rejects name-echo comments. W1 rewrote that XML in professional Persian with no code change.',
  filesModified: [
    'Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs',
    'Tooba.StoreContext.Infrastructure/StoreContextModule.cs',
    'Tooba.StoreContext.Infrastructure/Current/StoreCommerceContextAccessor.cs'
  ],
  filesCreated: [],
  filesMovedOrDeleted: 'NONE',
  namespacesChanged: 'NONE',
  publicApiChanged: 'NONE',
  diRegistrationChanged: 'NONE',
  projectEdgesChanged: 'NONE',
  behaviorPreservationProof: 'Code-token hash equality: for each of the three files every /// comment line and every blank line was stripped from both the pre-change blob (git show HEAD:<path>) and the post-change file, and the remaining code-only text was SHA-256 hashed. Contracts/Current/StoreCommerceContext.cs 18 code lines B15192DF3A112D1D == B15192DF3A112D1D; Infrastructure/StoreContextModule.cs 20 code lines 28D4269CD646CAF1 == 28D4269CD646CAF1; Infrastructure/Current/StoreCommerceContextAccessor.cs 12 code lines 616D2C4DC528EB45 == 616D2C4DC528EB45. The unified diff contains no added or removed line other than /// documentation lines and blank lines.',
  preservedInvariants: [
    'DefaultCurrency is a default-selection input only: never the currency of a transaction/line/order/settlement/payment-group and never a single-currency Cart/Order invariant; consumer transaction lines may carry their own currency.',
    'A null StoreCommerceContext means the context is unresolved: the consumer must fail closed instead of inventing Market/DefaultCurrency/SalesChannel.',
    'Assign(null) still throws ArgumentNullException via ArgumentNullException.ThrowIfNull; Current is null until assigned; the last Assign inside one scope wins.',
    'The accessor keeps no static mutable state, depends on no HttpContext and uses no AsyncLocal: lifetime is exactly one DI scope (request or worker cycle).'
  ],
  contractsBoundaryState: 'CLEAN_REUSED_NO_NEW_TYPE',
  illegalReferencesState: 'ZERO',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY_INBOUND_CONSUMERS_ONLY',
  crossModuleJoinState: 'NONE',
  persistenceState: 'NOT_APPLICABLE_NO_PERSISTENCE',
  cqrsState: 'NOT_APPLICABLE_NO_APPLICATION_USE_CASE',
  endpointOwnershipState: 'NOT_APPLICABLE',
  validatorCoverageState: 'NOT_APPLICABLE_INTERNAL_ONLY_ZERO_ENDPOINT_REACHABLE_REQUESTS',
  localizationState: 'CANONICAL_NO_OWNED_ERROR_CODES_NO_RESOURCE_SET_BY_DESIGN',
  localizationRationale: 'StoreContext declares and emits zero error codes, so there is nothing to catalogue. Registering descriptors for the consumer-owned cart.commerce.* or platform.* codes here would create duplicate descriptor ownership, which the canonical rule forbids; those codes stay with their natural owners (Cart, Foundation).',
  apiResultPatternState: 'CANONICAL_NO_HTTP_SURFACE',
  loggingState: 'CANONICAL',
  sensitiveLoggingState: 'NONE',
  correlationState: 'CANONICAL',
  fileCohesionState: 'COHESIVE',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  pathNamespaceState: 'EXACT',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_ONLY',
  hostFilesChanged: 0,
  schemaMigrationState: 'UNCHANGED',
  structureHandoffState: 'REQUIRED',
  structureHandoffDetail: 'W2 (tooba-architecture-structure) must decide the Infrastructure composition-entry placement: the ARCH-COMPLETE-002 standard permits StoreContextModule.cs at the Infrastructure root, while the newest certified modules place *Module.cs under Infrastructure/DependencyInjection/. If it moves the file, the allowlist, the two HostCartResidualGuardTests root-file facts and the manifest must be updated atomically.',
  focusedValidation: 'dotnet build src/backend/Tooba.slnx -> 0 errors / 0 warnings with CS1591 still enforced on non-test projects. Focused filter (HostCartResidualGuardTests, HostConfigurationAmcCertGuardTests, HostConfigurationAmcW1GuardTests, CartModuleAmsc001W3CertGuardTests, FulfillmentModuleAmsc001W3CertGuardTests, ErrorCatalogUniqueCodeGuardTests, HostMultiTenancyAmcCertGuardTests, HostOutboxAmcCertGuardTests) -> 56 passed / 0 failed / 0 skipped.',
  guardsAdded: 'NONE',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  blockingResidualDebt: 'ZERO',
  waveOutcome: 'STRUCTURE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W1/migrate.md'
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storeContextAmsc001W1": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}

let content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// Prefix/trailing integrity is checked before the in-prefix commit reconciliation below.
if (!content.startsWith(original.slice(0, lastIndex))) {
  throw new Error('prefix of the original file was not preserved');
}
if (!content.endsWith('}' + NL)) {
  throw new Error('unexpected trailing bytes');
}

content = content.replace('"commit": "PENDING_W0_COMMIT"', `"commit": "${W0_COMMIT}"`);

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w0 = parsed.storeContextAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.commit !== W0_COMMIT) {
  throw new Error('storeContextAmsc001W0 record missing or its commit was not reconciled');
}
const w1 = parsed.storeContextAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.verdict !== 'READY_FOR_CERTIFICATION') {
  throw new Error('storeContextAmsc001W1 record missing or wrong state');
}
if (w1.startingHead !== W0_COMMIT) {
  throw new Error('W1 startingHead must equal the W0 commit');
}

const storeContext = parsed.storeContext;
if (!storeContext || storeContext.state !== 'PLATFORM_CONTEXT_REFERENCE_PATTERN'
  || storeContext.httpApplicability !== 'INTERNAL_ONLY'
  || storeContext.endpointOwnership !== 'NOT_APPLICABLE'
  || storeContext.cqrs !== 'NOT_APPLICABLE_NO_APPLICATION_USE_CASE'
  || storeContext.structureCertifiedUnderArchComplete002 !== true) {
  throw new Error('historical storeContext block was disturbed');
}

if (parsed.structureLock.certifiedModules.filter((m) => m === 'StoreContext').length !== 1) {
  throw new Error('structureLock.certifiedModules must contain StoreContext exactly once');
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
if (!content.startsWith(original.slice(0, lastIndex).replace('"commit": "PENDING_W0_COMMIT"', `"commit": "${W0_COMMIT}"`))) {
  throw new Error('prefix of the original file was not preserved');
}
if (!content.endsWith('}' + NL)) {
  throw new Error('unexpected trailing bytes');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: storeContextAmsc001W1; reconciled W0 commit)');
