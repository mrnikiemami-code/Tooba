// TB-TMAR-INVENTORY-AMSC-001-W0 — persist the Analyze wave checkpoint into the durable TMAR SoT.
// Additive only: appends the inventoryModuleAmsc001W0 record with a text-anchor insertion so the
// existing file formatting (4-space indent, CRLF) is preserved and no other record is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
let content = original;
const NL = '\r\n';

if (content.includes('"inventoryModuleAmsc001W0"')) {
  throw new Error('inventoryModuleAmsc001W0 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-INVENTORY-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-analyze',
  target: 'src/backend/Modules/Inventory/Tooba.Inventory.*',
  startingHead: '9ec8e3e9',
  parentTask: null,
  state: 'ANALYZE_COMPLETE',
  verdict: 'READY_TO_MIGRATE',
  lockVersion: 'ARCH-COMPLETE-002',
  priorCertificationState: 'COMPLETE_REFERENCE_PATTERN_INTERNAL_ONLY_NEVER_AMSC_CERTIFIED',
  foundationState: 'FOUNDATION_READY',
  ownershipState: 'correct',
  fileCohesionState: 'MULTI_RESPONSIBILITY_COHESION_VIOLATION',
  oversizedGodFileState: 'InventoryDirectory.cs 791 LOC below the 800 LOC ceiling but genuinely multi-responsibility (6 ports + order-supply engine + expired-hold reclaimer + guard + Offer/Catalog tracing helpers)',
  localizationState: 'HARDCODED_TEXT_24_RAW_INVENTORY_CODES_1_CATALOGUED_NO_RESOURCE_SET',
  apiResultPatternState: 'CANONICAL',
  stableErrorCodeState: 'UNREGISTERED_CODES_23_OF_24',
  loggingState: 'CANONICAL',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  cqrsState: 'COMPLIANT_FOR_INTERNAL_ONLY',
  validatorCoverageState: 'NOT_APPLICABLE_INTERNAL_ONLY_ZERO_ENDPOINT_REACHABLE_REQUESTS',
  contractsBoundaryState: 'CLEAN',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY',
  crossModuleContractsReference: 'Tooba.Inventory.Application/Infrastructure -> Tooba.Offer.Contracts (IOfferLookupGateway, OfferErrorCodes) and Tooba.Catalog.Contracts (ICatalogVariantLookup) only',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT',
  endpointOwnershipState: 'NOT_APPLICABLE',
  httpApplicability: 'INTERNAL_ONLY',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_ONLY',
  schemaMigrationState: 'UNCHANGED',
  behaviorPreservationRisk: 'LOW',
  folderGranularityState: 'PROFESSIONAL_SHALLOW_WITH_ONE_DEVIATION',
  structureHandoffState: 'REQUIRED',
  canonicalReferenceUsed: 'BulkInquiry.Contracts/Errors+Resources (catalog+resource set+resx pair) / CustomerProfile Operation seam + Endpoints Errors+Resources / Order+Payment shared-code non-re-registration / Offer IModuleCallTracer decoration / BuildingBlocks',
  blockers: [
    'Never AMSC-certified: no structureCertified manifest entry (modules[] has no Inventory), no AMSC SoT record, no ARCH-COMPLETE-002 durable guard, no AMSC evidence tree.',
    'UNREGISTERED_CODES: 23 of 24 emitted inventory.* machine codes are raw string literals with no ErrorDescriptor, so SafeErrorMapper cannot classify them.',
    'HARDCODED_TEXT: no IErrorResourceSet, no InventoryErrors.resx, no InventoryErrors.fa.resx; the module ships zero localization infrastructure.',
    'MULTI_RESPONSIBILITY_COHESION_VIOLATION: Infrastructure/Directories/InventoryDirectory.cs (791 LOC) implements 6 ports, declares a second type (OpenInventoryUseCaseGuard), and carries the order-supply engine, the expired-hold reclaimer with raw SQL, and the Offer/Catalog tracing helpers.',
    'Mixed fault typing: raw InvalidOperationException("domain.invariant") in StockPosition/InventoryLocation/InventoryDirectory/InventoryReturnGateway beside typed ContractOperationException in StockReservation; no single operation seam converting typed faults to Result.',
    'Application/Ports/InventoryDirectoryPorts.cs mixes a result model (ReservationReceipt), an authorization port (IInventoryUseCaseGuard) and a persistence port (IInventoryDirectory) in one file.',
    'Descriptor-ownership collision already resolved upstream: inventory.reservation.retry_limit_reached is Order-owned and Payment deliberately does not register it; inventory.supply.unavailable has no descriptor owner today and Inventory is the natural owner.'
  ],
  noArchitectureDecisionRequired: true,
  hostTouched: false,
  productionCodeChanged: false,
  commit: 'PENDING_W0_COMMIT',
  waveOutcome: 'MIGRATE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W0/analyze.md'
};

const recordText = JSON.stringify(record, null, 4)
  .split('\n')
  .map((line, index) => (index === 0 ? `    "inventoryModuleAmsc001W0": ${line}` : `    ${line}`))
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
if (!w0 || w0.state !== 'ANALYZE_COMPLETE' || w0.verdict !== 'READY_TO_MIGRATE') {
  throw new Error('inventoryModuleAmsc001W0 record missing or wrong state');
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
console.log('PATCHED docs/architecture/tmar-current-state.json (additive: inventoryModuleAmsc001W0)');
