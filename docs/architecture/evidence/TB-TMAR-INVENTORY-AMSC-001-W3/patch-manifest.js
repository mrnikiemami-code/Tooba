// TB-TMAR-INVENTORY-AMSC-001-W3 — manifest reconciliation (docs/architecture/tmar-module-structure-manifests.json).
// Line-based and idempotent: the Inventory record is moved from preCertModules[] into the certified
// modules[] array with structureCertified true and the AMSC-001 note. Every allowlist,
// forbidden-root and forbidden-folder value is carried over verbatim, and every unrelated line keeps
// its exact text (the file is UTF-8 BOM + CRLF, so the write restores both).
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
const raw = fs.readFileSync(file, 'utf8');
const bom = raw.charCodeAt(0) === 0xfeff;
const lines = (bom ? raw.slice(1) : raw).split('\r\n');

const EOL = '\r\n';
const note = 'TB-TMAR-INVENTORY-AMSC-001-W3 (tooba-architecture-certify) certified Inventory under ARCH-COMPLETE-002: verdict COMPLETE_REFERENCE_PATTERN, structureState CERTIFIED, structureCertified true. INTERNAL_ONLY module: no Endpoints project by design (Offer owns the seller stock write route /offers/{offerId:guid}/inventory) and zero endpoint-reachable requests, so the validator matrix is NOT_APPLICABLE_INTERNAL_ONLY by construction. Wave lineage W0 c6917553 -> W1 133d413d -> W2 87101cb4 -> W3 this commit. Structure: capability-first shallow Contracts/{Availability,Cart,Checkout,Errors,Fulfillment,Orders,Resources,Returns,Seller} + Domain/{Aggregates,Events,ValueObjects} + Application/{Checkout,Composition,Orders,Ports} + Infrastructure/{Adapters,DependencyInjection,Directories,Events,Messaging,Persistence/Migrations}; the multi-responsibility Directories/InventoryDirectory.cs was split into cohesive partials (persistence seam + OrderSupply + Availability + Reclaimer + SellerWrite + Lookups) and OpenInventoryUseCaseGuard moved to its own file; every Directories/*.cs <= 500 LOC; path<->namespace EXACT (0 mismatches); root allowlists EMPTY on all five projects; /Modules/Inventory/ solution grouping (5 projects). Errors/localization: single stable-code owner Contracts/Errors/InventoryErrorCodes.cs (29 declared codes) + InventoryErrorCatalogContributor (29 owned descriptors; the Order-owned inventory.reservation.retry_limit_reached and the Order-owned inventory.recovery.* keyspace stay unclaimed) + InventoryErrorResourceSet claiming the inventory. keyspace + the bilingual InventoryErrors.resx / InventoryErrors.fa.resx pair covering every declared code. Faults: Domain/Application/Infrastructure raise typed ContractOperationException with module-owned codes; Application/Composition/InventoryOperation.cs is the single typed-fault-to-Result seam (IsKnown filter), replacing the raw InvalidOperationException and raw inventory.* literals. Microservice-extractable: zero foreign Application/Infrastructure/Domain project edge in any Inventory project; the only foreign references are the legal Contracts seams (Offer.Contracts, Catalog.Contracts); no cross-module join; own inventory schema; 0 migration files touched. Host closure PRESERVED (no Host/Inventory folder, no MapInventoryEndpoints). Durable guards InventoryModuleAmsc001W3CertGuardTests + InventoryModuleAmsc001W2StructureGuardTests + InventoryAmcW1MigrateGuardTests + InventoryArchitectureGuardTests. Stop gate USER_REVIEW_INVENTORY_AMSC_001_W3; automaticNextImplementationTask NONE.';

const projects = [
  {
    projectName: 'Tooba.Inventory.Contracts',
    justification:
      'Boundary semantics only: capability folders Availability/Cart/Checkout/Orders/Fulfillment/Returns/Seller carry the public ports and DTOs, Errors/ the single stable-code owner plus catalog contributor and resource set, Resources/ the bilingual InventoryErrors.resx pair; no root .cs remains and namespace alignment is exact path-derived equality.',
    forbiddenRootFiles: ['InventoryErrorCodes.cs', 'InventoryContracts.cs'],
    forbiddenTopLevelFolders: [],
  },
  {
    projectName: 'Tooba.Inventory.Domain',
    justification:
      'Aggregates/ (StockPosition, StockReservation, InventoryLocation), Events/ and ValueObjects/ hold the domain rules; no root .cs remains and namespace alignment is exact path-derived equality. AMSC-001 W1 added the Contracts project reference so the aggregates raise typed ContractOperationException faults carrying module-owned stable error codes instead of raw InvalidOperationException, a legal Domain -> own-Contracts boundary edge rather than foreign Application/Infrastructure coupling.',
    forbiddenRootFiles: ['StockPosition.cs', 'StockReservation.cs'],
    forbiddenTopLevelFolders: ['Entities', 'Policies'],
  },
  {
    projectName: 'Tooba.Inventory.Application',
    justification:
      'No root .cs remains. The capabilities are Checkout/ (reservation adapter), Orders/ (lifecycle adapter + order-supply contracts) and Composition/ (the typed-fault-to-Result InventoryOperation seam), with Ports/ holding one capability per file (IInventoryDirectory, IInventoryUseCaseGuard, ReservationReceipt); the mixed InventoryDirectoryPorts.cs dump was deleted and namespace alignment is exact path-derived equality.',
    forbiddenRootFiles: ['InventoryDirectoryPorts.cs', 'InventoryContracts.cs'],
    forbiddenTopLevelFolders: ['Commands', 'Queries', 'Models', 'Validators'],
  },
  {
    projectName: 'Tooba.Inventory.Infrastructure',
    justification:
      'No root .cs remains: the module composition entry lives under DependencyInjection/, the cohesive directory partials and OpenInventoryUseCaseGuard under Directories/, the contract adapters under Adapters/, the outbox registration under Messaging/, integration events under Events/ and the EF model plus migrations under Persistence/Migrations, with exact path-derived namespaces.',
    forbiddenRootFiles: ['InventoryModule.cs', 'InventoryDirectory.cs', 'InventoryDbContext.cs'],
    forbiddenTopLevelFolders: ['Migrations'],
  },
  {
    projectName: 'Tooba.Inventory.Tests',
    justification:
      'Test-only project with Architecture/ durable guards and Behavior/ behavior tests; no production code and no root .cs.',
    forbiddenRootFiles: [],
    forbiddenTopLevelFolders: [],
  },
];

function recordLines() {
  const out = [];
  out.push('    {');
  out.push('      "module": "Inventory",');
  out.push('      "structureCertified": true,');
  out.push('      "lockVersion": "ARCH-COMPLETE-002",');
  out.push(`      "certificationNote": ${JSON.stringify(note)},`);
  out.push('      "projects": [');
  projects.forEach((p, index) => {
    out.push('        {');
    out.push(`          "projectName": ${JSON.stringify(p.projectName)},`);
    out.push('          "rootAllowlist": [],');
    out.push(`          "rootAllowlistJustification": ${JSON.stringify(p.justification)},`);
    if (p.forbiddenRootFiles.length === 0) {
      out.push('          "forbiddenRootFiles": [],');
    } else {
      out.push('          "forbiddenRootFiles": [');
      p.forbiddenRootFiles.forEach((f, i) => {
        out.push(`            ${JSON.stringify(f)}${i === p.forbiddenRootFiles.length - 1 ? '' : ','}`);
      });
      out.push('          ],');
    }
    if (p.forbiddenTopLevelFolders.length === 0) {
      out.push('          "forbiddenTopLevelFolders": []');
    } else {
      out.push('          "forbiddenTopLevelFolders": [');
      p.forbiddenTopLevelFolders.forEach((f, i) => {
        out.push(`            ${JSON.stringify(f)}${i === p.forbiddenTopLevelFolders.length - 1 ? '' : ','}`);
      });
      out.push('          ]');
    }
    out.push(`        }${index === projects.length - 1 ? '' : ','}`);
  });
  out.push('      ]');
  out.push('    },');
  return out;
}

function findLine(predicate, label) {
  const index = lines.findIndex(predicate);
  if (index < 0) {
    throw new Error(`anchor not found: ${label}`);
  }
  return index;
}

// --- 1. Locate the Inventory pre-cert record inside preCertModules[]. ---
const preCertIndex = findLine((l) => l.trim() === '"preCertModules": [', 'preCertModules');
const invStart = lines.findIndex((l, i) => i > preCertIndex && l.trim() === '"module": "Inventory",');
if (invStart < 0) {
  throw new Error('Inventory record not found');
}
if (lines[invStart - 1].trim() !== '{') {
  throw new Error('unexpected Inventory record opening');
}
// The record ends at the project array close followed by the record close.
let invEnd = -1;
for (let i = invStart; i < lines.length; i += 1) {
  if (lines[i] === '    },' && lines[i - 1] && lines[i - 1].trim() === ']') {
    invEnd = i;
    break;
  }
}
if (invEnd < 0) {
  throw new Error('Inventory record end not found');
}
const invRecord = lines.slice(invStart - 1, invEnd + 1);
if (!invRecord.some((l) => l.includes('"projectName": "Tooba.Inventory.Tests"'))) {
  throw new Error('Inventory record does not carry the expected five projects');
}

// --- 2. Remove it from preCertModules[]. ---
lines.splice(invStart - 1, invEnd - invStart + 2);

// --- 3. Insert the promoted record at the end of the certified modules[] array. ---
const modulesIndex = findLine((l) => l.trim() === '"modules": [', 'modules');
const uncertifiedIndex = findLine((l) => l.trim() === '"uncertifiedHttpOwningModules": [', 'uncertifiedHttpOwningModules');
let modulesClose = -1;
for (let i = uncertifiedIndex - 1; i > modulesIndex; i -= 1) {
  if (lines[i] === '  ],') {
    modulesClose = i;
    break;
  }
}
if (modulesClose < 0) {
  throw new Error('modules array close not found');
}
const record = recordLines();
record[record.length - 1] = '    }'; // last array element carries no trailing comma
if (lines[modulesClose - 1] === '    }') {
  lines[modulesClose - 1] = '    },'; // the previously-last record now has a successor
}
lines.splice(modulesClose, 0, ...record);

const text = (bom ? '\uFEFF' : '') + lines.join(EOL);
fs.writeFileSync(file, text, 'utf8');

// --- 4. Validate. ---
const parsed = JSON.parse(bom ? text.slice(1) : text);
const inventory = parsed.modules.filter((m) => m.module === 'Inventory');
if (inventory.length !== 1) {
  throw new Error(`expected exactly one certified Inventory entry, found ${inventory.length}`);
}
if (inventory[0].structureCertified !== true) {
  throw new Error('Inventory entry is not structureCertified');
}
if (inventory[0].projects.length !== 5) {
  throw new Error(`expected 5 Inventory projects, found ${inventory[0].projects.length}`);
}
if (parsed.preCertModules.some((m) => m.module === 'Inventory')) {
  throw new Error('Inventory still present in preCertModules');
}
if (parsed.modules.length !== 24) {
  throw new Error(`expected 24 certified modules, found ${parsed.modules.length}`);
}
if (parsed.preCertModules.length !== 1 || parsed.preCertModules[0].module !== 'ProductWorkspace') {
  throw new Error('preCertModules must now carry only ProductWorkspace');
}
console.log('Manifest patched: Inventory promoted to the certified modules array (line-anchored, verbatim allowlists).');
