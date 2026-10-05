// TB-TMAR-INVENTORY-AMSC-001-W2 — add the Inventory structure record to the manifest preCertModules
// array (structureCertified false) with per-project root allowlists. Additive only; the certified
// modules array, uncertifiedHttpOwningModules and every other record are untouched.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

if (original.includes('"module": "Inventory"')) {
  throw new Error('an Inventory manifest record already exists — refusing to overwrite');
}

const record = {
  module: 'Inventory',
  structureCertified: false,
  lockVersion: 'ARCH-COMPLETE-002',
  certificationNote: 'TB-TMAR-INVENTORY-AMSC-001 W0 c6917553 -> W1 133d413d -> W2 (pre-cert structure record). INTERNAL_ONLY module: no Endpoints project (Offer owns /offers/{offerId:guid}/inventory) and zero endpoint-reachable requests, so the validator matrix is NOT_APPLICABLE_INTERNAL_ONLY by construction. Realigned to the capability-first COMPLETE_REFERENCE_PATTERN: Contracts/{Availability,Cart,Checkout,Errors,Fulfillment,Orders,Resources,Returns,Seller}, Domain/{Aggregates,Events,ValueObjects}, Application/{Checkout,Composition,Orders,Ports}, Infrastructure/{Adapters,DependencyInjection,Directories,Events,Messaging,Persistence,Migrations}; the multi-responsibility Directories/InventoryDirectory.cs was split into cohesive partials (persistence seam + OrderSupply + Availability + Reclaimer + SellerWrite + Lookups) and OpenInventoryUseCaseGuard moved to its own file. Single stable-code owner Contracts/Errors/InventoryErrorCodes + InventoryErrorCatalogContributor + InventoryErrorResourceSet with a bilingual Resources resx pair; typed ContractOperationException faults mapped by Application/Composition/InventoryOperation. Microservice-extractable with zero foreign Application/Infrastructure/Domain coupling. Do not promote to structureLock.certifiedModules until TB-TMAR-INVENTORY-AMSC-001-W3.',
  projects: [
    {
      projectName: 'Tooba.Inventory.Contracts',
      rootAllowlist: [],
      rootAllowlistJustification: 'Boundary semantics only: capability folders Availability/Cart/Checkout/Orders/Fulfillment/Returns/Seller carry the public ports and DTOs, Errors/ the single stable-code owner plus catalog contributor and resource set, Resources/ the bilingual InventoryErrors.resx pair; no root .cs remains and namespace alignment is exact path-derived equality.',
      forbiddenRootFiles: ['InventoryErrorCodes.cs', 'InventoryContracts.cs'],
      forbiddenTopLevelFolders: [],
    },
    {
      projectName: 'Tooba.Inventory.Domain',
      rootAllowlist: [],
      rootAllowlistJustification: 'Aggregates/ (StockPosition, StockReservation, InventoryLocation), Events/ and ValueObjects/ hold the domain rules; no root .cs remains and namespace alignment is exact path-derived equality. AMSC-001 W1 added the Contracts project reference so the aggregates raise typed ContractOperationException faults carrying module-owned stable error codes instead of raw InvalidOperationException, a legal Domain -> own-Contracts boundary edge rather than foreign Application/Infrastructure coupling.',
      forbiddenRootFiles: ['StockPosition.cs', 'StockReservation.cs'],
      forbiddenTopLevelFolders: ['Entities', 'Policies'],
    },
    {
      projectName: 'Tooba.Inventory.Application',
      rootAllowlist: [],
      rootAllowlistJustification: 'No root .cs remains. The capabilities are Checkout/ (reservation adapter), Orders/ (lifecycle adapter + order-supply contracts) and Composition/ (the typed-fault-to-Result InventoryOperation seam), with Ports/ holding one capability per file (IInventoryDirectory, IInventoryUseCaseGuard, ReservationReceipt); the mixed InventoryDirectoryPorts.cs dump was deleted and namespace alignment is exact path-derived equality.',
      forbiddenRootFiles: ['InventoryDirectoryPorts.cs', 'InventoryContracts.cs'],
      forbiddenTopLevelFolders: ['Commands', 'Queries', 'Models', 'Validators'],
    },
    {
      projectName: 'Tooba.Inventory.Infrastructure',
      rootAllowlist: [],
      rootAllowlistJustification: 'No root .cs remains: the module composition entry lives under DependencyInjection/, the cohesive directory partials and OpenInventoryUseCaseGuard under Directories/, the contract adapters under Adapters/, the outbox registration under Messaging/, integration events under Events/ and the EF model plus migrations under Persistence/Migrations, with exact path-derived namespaces.',
      forbiddenRootFiles: ['InventoryModule.cs', 'InventoryDirectory.cs', 'InventoryDbContext.cs'],
      forbiddenTopLevelFolders: ['Migrations'],
    },
    {
      projectName: 'Tooba.Inventory.Tests',
      rootAllowlist: [],
      rootAllowlistJustification: 'Test-only project with Architecture/ durable guards and Behavior/ behavior tests; no production code and no root .cs.',
      forbiddenRootFiles: [],
      forbiddenTopLevelFolders: [],
    },
  ],
};

const anchor = `  "uncertifiedHttpOwningModules":`;
const anchorIndex = original.indexOf(anchor);
if (anchorIndex < 0) {
  throw new Error('uncertifiedHttpOwningModules anchor not found');
}

// Insert the Inventory record at the start of the preCertModules array.
const arrayStart = original.indexOf('"preCertModules": [');
if (arrayStart < 0) {
  throw new Error('preCertModules anchor not found');
}
const firstEntryStart = original.indexOf('{', arrayStart);
const recordText = JSON.stringify(record, null, 4)
  .split('\n')
  .map((line, index) => (index === 0 ? `    ${line}` : `    ${line}`))
  .join(NL);

let content = original.slice(0, firstEntryStart)
  + recordText.replace(/^    \{/, '{')
  + `,${NL}    `
  + original.slice(firstEntryStart);

// --- validate -------------------------------------------------------------
const parsed = JSON.parse(content.replace(/^\uFEFF/, ''));
const entry = parsed.preCertModules.find((m) => m.module === 'Inventory');
if (!entry || entry.structureCertified !== false) {
  throw new Error('Inventory preCert record missing or wrongly certified');
}
if (parsed.modules.some((m) => m.module === 'Inventory')) {
  throw new Error('Inventory must not be in the certified modules array before W3');
}
if (parsed.modules.length !== 23 || parsed.preCertModules.length !== 2) {
  throw new Error(`unexpected module counts: certified=${parsed.modules.length} preCert=${parsed.preCertModules.length}`);
}
if (JSON.stringify(parsed.uncertifiedHttpOwningModules) !== JSON.stringify(['Returns', 'Notification', 'Support', 'Wallet', 'Promotion'])) {
  throw new Error('uncertifiedHttpOwningModules was disturbed');
}
for (const module of parsed.modules) {
  if (!module.structureCertified || module.lockVersion !== 'ARCH-COMPLETE-002') {
    throw new Error(`certified module record disturbed: ${module.module}`);
  }
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-module-structure-manifests.json (additive: Inventory preCertModules)');
