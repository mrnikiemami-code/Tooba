// TB-TMAR-INVENTORY-AMSC-001-W1 — persist the Migrate wave checkpoint into the durable TMAR SoT.
// Additive only: appends the inventoryModuleAmsc001W1 record with a text-anchor insertion so the
// existing file formatting (4-space indent, CRLF) is preserved and no other record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
let content = original;
const NL = '\r\n';

if (content.includes('"inventoryModuleAmsc001W1"')) {
  throw new Error('inventoryModuleAmsc001W1 already present — refusing to overwrite');
}
if (!content.includes('"inventoryModuleAmsc001W0"')) {
  throw new Error('inventoryModuleAmsc001W0 missing — W0 SoT record must exist first');
}

const record = {
  task: 'TB-TMAR-INVENTORY-AMSC-001-W1',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-migrate',
  target: 'src/backend/Modules/Inventory/Tooba.Inventory.*',
  parentTask: 'TB-TMAR-INVENTORY-AMSC-001-W0',
  startingHead: 'c6917553',
  state: 'MIGRATE_COMPLETE',
  verdict: 'MIGRATED',
  lockVersion: 'ARCH-COMPLETE-002',
  localizationState: 'CANONICAL_29_DECLARED_CODES_28_OWNED_DESCRIPTORS_BILINGUAL_RESX_AND_RESOURCE_SET',
  stableErrorCodeState: 'REGISTERED_SINGLE_OWNER_PER_CODE',
  descriptorOwnership: 'InventoryErrorCatalogContributor owns 28 Inventory-produced descriptors; inventory.reservation.retry_limit_reached stays Order-owned; inventory.recovery.* stays Order-owned and outside the Inventory Owns() keyspace.',
  fileCohesionState: 'COHESIVE',
  cohesionAction: 'Directories/InventoryDirectory.cs 791 LOC -> 324 LOC persistence seam + OrderSupply(302) + Availability(84) + Reclaimer(57) + SellerWrite(47) + Lookups(43) partials; OpenInventoryUseCaseGuard moved to its own file; Application/Ports/InventoryDirectoryPorts.cs split into IInventoryDirectory/IInventoryUseCaseGuard/ReservationReceipt.',
  faultTypingState: 'SINGLE_TYPED_MECHANISM',
  faultTypingAction: 'Raw InvalidOperationException and raw inventory.* literals removed from Domain/Application/Infrastructure; ContractOperationException + InventoryErrorCodes everywhere; InventoryOperation composition seam maps typed faults to Result.',
  apiResultPatternState: 'CANONICAL',
  contractsBoundaryState: 'CLEAN',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY',
  crossModuleJoinState: 'NONE',
  domainToOwnContractsEdge: 'Tooba.Inventory.Domain now references Tooba.Inventory.Contracts (own module only) so aggregates raise typed module-owned codes; no foreign module edge added.',
  behaviorPreservationRisk: 'LOW',
  behaviorPreservationEvidence: 'All 24 emitted code strings preserved; 4 migrations untouched; EnsureOrderSupply mode matrix, atomic ExecuteUpdateAsync guards, FOR UPDATE SKIP LOCKED reclaim and idempotency semantics unchanged; DI lifetimes preserved.',
  hostTouched: false,
  hostTestGuardsAligned: [
    'Host.Tests/CartLifetimeSeparationTests.cs asserts the adapter uses InventoryErrorCodes.SupplyUnavailable instead of a raw literal (the literal the migration deliberately removed)',
    'Host.Tests/OrderSupplyFoundationTests.cs InvInfra() aggregates InventoryDirectory*.cs partials',
    'Host.Tests/UnpaidOrderExpiryTests.cs reads InventoryDirectory.OrderSupply.cs'
  ],
  verification: {
    inventoryInfrastructureBuild: 'succeeded',
    hostTestsBuild: 'succeeded',
    inventoryTests: '13/13 passed',
    inventoryBehaviorHostSubset: '37 passed / 5 skipped / 0 failed',
    errorCatalogUniqueCodeGuard: '3/3 passed',
    fullHostTestsVsW0Baseline: 'identical 82 pre-existing failures, zero new failures'
  },
  durableGuard: 'Tooba.Inventory.Tests/Architecture/InventoryAmcW1MigrateGuardTests.cs (7 facts)',
  structureHandoffState: 'REQUIRED',
  remainingCertificationBlockers: [
    'W0 blocker 5 only: SoT/manifest honesty — no structureCertified manifest entry, no ARCH-COMPLETE-002 durable cert guard, no Master Recovery checkpoint.'
  ],
  waveOutcome: 'STRUCTURE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W1/migrate.md'
};

const recordText = JSON.stringify(record, null, 4)
  .split('\n')
  .map((line, index) => (index === 0 ? `    "inventoryModuleAmsc001W1": ${line}` : `    ${line}`))
  .join(NL);

const terminator = `    }${NL}}${NL}`;
const lastIndex = content.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}
content = content.slice(0, lastIndex) + `    },${NL}` + recordText + NL + `}${NL}`;

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

const w0 = parsed.inventoryModuleAmsc001W0;
if (!w0 || w0.state !== 'ANALYZE_COMPLETE') {
  throw new Error('inventoryModuleAmsc001W0 record disturbed');
}
const w1 = parsed.inventoryModuleAmsc001W1;
if (!w1 || w1.state !== 'MIGRATE_COMPLETE' || w1.verdict !== 'MIGRATED') {
  throw new Error('inventoryModuleAmsc001W1 record missing or wrong state');
}

const inventory = parsed.completeReferenceModules.find((m) => m.module === 'Inventory');
if (!inventory) {
  throw new Error('completeReferenceModules Inventory record missing');
}
const expectedInventory = {
  state: 'COMPLETE_REFERENCE_PATTERN',
  httpApplicability: 'INTERNAL_ONLY',
  endpointOwnership: 'NOT_APPLICABLE',
  cqrs: 'INTERNAL_USE_CASE_BOUNDARIES',
  lastAcceptedTask: 'TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001',
  lastAcceptedCommit: '2814da32245b25a718aa952ba0e836d7550a3ee0'
};
for (const [key, value] of Object.entries(expectedInventory)) {
  if (inventory[key] !== value) {
    throw new Error(`completeReferenceModules Inventory.${key} was disturbed`);
  }
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

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: inventoryModuleAmsc001W1)');
