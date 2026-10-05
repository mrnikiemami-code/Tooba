// TB-TMAR-INVENTORY-AMSC-001-W2 — persist the Structure wave checkpoint into the durable TMAR SoT.
// Additive only: appends the inventoryModuleAmsc001W2 record with a text-anchor insertion so the
// existing file formatting (4-space indent, CRLF) is preserved and no other record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
let content = original;
const NL = '\r\n';

if (content.includes('"inventoryModuleAmsc001W2"')) {
  throw new Error('inventoryModuleAmsc001W2 already present — refusing to overwrite');
}
if (!content.includes('"inventoryModuleAmsc001W1"')) {
  throw new Error('inventoryModuleAmsc001W1 missing — W1 SoT record must exist first');
}

const record = {
  task: 'TB-TMAR-INVENTORY-AMSC-001-W2',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-structure',
  target: 'src/backend/Modules/Inventory/Tooba.Inventory.*',
  parentTask: 'TB-TMAR-INVENTORY-AMSC-001-W1',
  startingHead: '133d413d',
  state: 'STRUCTURE_COMPLETE',
  verdict: 'STRUCTURE_READY_FOR_CERTIFY',
  lockVersion: 'ARCH-COMPLETE-002',
  structureState: 'READY_FOR_CERTIFY',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  pathNamespaceState: 'EXACT_ZERO_MISMATCHES',
  rootAllowlistState: 'EMPTY_ROOT_ON_ALL_FIVE_PROJECTS',
  overFolderingState: 'NONE',
  staleDuplicateCopyState: 'NONE',
  solutionGroupingState: 'CANONICAL_MODULES_INVENTORY_FOLDER',
  endpointsProjectState: 'ABSENT_BY_DESIGN_INTERNAL_ONLY',
  manifestState: 'PRE_CERT_RECORD_ADDED_PRE_CERT_MODULES_STRUCTURE_CERTIFIED_FALSE',
  capabilityLayout: 'Contracts/{Availability,Cart,Checkout,Errors,Fulfillment,Orders,Resources,Returns,Seller} + Domain/{Aggregates,Events,ValueObjects} + Application/{Checkout,Composition,Orders,Ports} + Infrastructure/{Adapters,DependencyInjection,Directories,Events,Messaging,Persistence/Migrations}',
  cohesiveDirectoryState: 'InventoryDirectory split into persistence seam + OrderSupply + Availability + Reclaimer + SellerWrite + Lookups partials with OpenInventoryUseCaseGuard in its own file; every Directories/*.cs <= 500 LOC',
  durableGuard: 'Tooba.Host.Tests/Architecture/InventoryModuleAmsc001W2StructureGuardTests.cs (10 facts)',
  verification: {
    hostTestsBuild: 'succeeded',
    structureGuard: '10/10 passed',
    inventoryTests: '13/13 passed',
    pathNamespaceAudit: '0 mismatches',
    fullHostTestsVsW0Baseline: 'identical 82 pre-existing failures by test name, zero new failures'
  },
  inheritedDriftRecordedNotOwned: [
    'TmarCompleteReferenceStructureGateTests hardcodes a stale certified module list (Catalog and CustomerProfile were certified without updating it) — pre-existing at the W0 starting HEAD, scheduled for W3 repair.'
  ],
  structureHandoffState: 'READY_FOR_W3_CERTIFY',
  waveOutcome: 'CERTIFY_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W2/structure.md'
};

const recordText = JSON.stringify(record, null, 4)
  .split('\n')
  .map((line, index) => (index === 0 ? `    "inventoryModuleAmsc001W2": ${line}` : `    ${line}`))
  .join(NL);

const terminator = `    }${NL}}${NL}`;
const lastIndex = content.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}
content = content.slice(0, lastIndex) + `    },${NL}` + recordText + NL + `}${NL}`;

// --- validate: additive only, accepted pointers untouched -----------------
const parsed = JSON.parse(content);

for (const wave of ['inventoryModuleAmsc001W0', 'inventoryModuleAmsc001W1']) {
  if (!parsed[wave]) {
    throw new Error(`${wave} record disturbed`);
  }
}
const w2 = parsed.inventoryModuleAmsc001W2;
if (!w2 || w2.state !== 'STRUCTURE_COMPLETE' || w2.verdict !== 'STRUCTURE_READY_FOR_CERTIFY') {
  throw new Error('inventoryModuleAmsc001W2 record missing or wrong state');
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: inventoryModuleAmsc001W2)');
