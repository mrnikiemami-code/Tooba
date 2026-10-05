// TB-TMAR-INVENTORY-AMSC-001-W3 — SoT reconciliation (docs/architecture/tmar-current-state.json).
// Text-anchored and idempotent: it does NOT re-serialize the whole document, so unrelated records
// keep their exact formatting/line endings and no unrelated diff is produced. The repository-global
// Host root checkpoint (lastAcceptedTask / nextTask / workflowStop / automaticNextImplementationTask)
// is deliberately NOT touched.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const raw = fs.readFileSync(file, 'utf8');
const bom = raw.charCodeAt(0) === 0xfeff;
let text = bom ? raw.slice(1) : raw;

const crlf = (s) => s.replace(/\n/g, '\r\n');

function replaceOnce(from, to, label) {
    const f = crlf(from);
    const count = text.split(f).length - 1;
    if (count !== 1) {
        throw new Error(`anchor not unique (${count}) for ${label}`);
    }
    text = text.replace(f, crlf(to));
}

// 1. W0 record: reconcile the commit placeholder to the real W0 commit (historical truth, additive).
replaceOnce(
    `        "commit": "PENDING_W0_COMMIT",
        "waveOutcome": "MIGRATE_UNBLOCKED",
        "evidence": "docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W0/analyze.md"`,
    `        "commit": "c6917553",
        "waveOutcome": "MIGRATE_UNBLOCKED",
        "evidence": "docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W0/analyze.md"`,
    'inventoryModuleAmsc001W0 commit');

// 2. structureLock.certifiedModules: add Inventory exactly once, appended after CustomerProfile.
replaceOnce(
    `            "Catalog",
            "CustomerProfile"
        ],`,
    `            "Catalog",
            "CustomerProfile",
            "Inventory"
        ],`,
    'structureLock.certifiedModules');

// 3. Append the W3 certification record after the W2 record.
const w3 = `    "inventoryModuleAmsc001W3": {
        "task": "TB-TMAR-INVENTORY-AMSC-001-W3",
        "mode": "ARCHITECT_DIRECT_AMSC",
        "skill": "tooba-architecture-certify",
        "target": "src/backend/Modules/Inventory/Tooba.Inventory.*",
        "parentTask": "TB-TMAR-INVENTORY-AMSC-001-W2",
        "startingHead": "87101cb4",
        "state": "INVENTORY_AMSC_001_CERTIFIED",
        "verdict": "COMPLETE_REFERENCE_PATTERN",
        "lockVersion": "ARCH-COMPLETE-002",
        "structureCertified": true,
        "structureState": "CERTIFIED",
        "structureGateSource": "TB-TMAR-INVENTORY-AMSC-001-W2",
        "httpApplicability": "INTERNAL_ONLY",
        "endpointOwnership": "NOT_APPLICABLE",
        "endpointOwnershipState": "NOT_APPLICABLE_INTERNAL_ONLY",
        "cqrsState": "INTERNAL_USE_CASE_BOUNDARIES",
        "endpointsProjectState": "ABSENT_BY_DESIGN_INTERNAL_ONLY",
        "endpointReachableRequests": 0,
        "validatorCoverageState": "NOT_APPLICABLE_INTERNAL_ONLY",
        "validatorCoverageDetail": "Inventory owns no HTTP surface by design, so it has zero endpoint-reachable requests and therefore no REQUIRED validator. The seller stock write route /offers/{offerId:guid}/inventory is Offer-owned (Offer.Endpoints -> ISender -> SetOfferInventoryCommand) and consumes the Inventory Contracts port ISellerOfferInventoryGateway; the Offer-side validator stays Offer-owned. No ceremonial Inventory validator was added.",
        "offerHttpSurfaceState": "PRESERVED_OFFER_OWNED",
        "sellerStockWriteRoute": "/offers/{offerId:guid}/inventory (Tooba.Offer.Endpoints, ISender -> SetOfferInventoryCommand -> ISellerOfferInventoryGateway)",
        "folderGranularityState": "PROFESSIONAL_SHALLOW",
        "solutionExplorerState": "CANONICAL",
        "solutionFolder": "/Modules/Inventory/",
        "solutionProjectEntries": 5,
        "pathNamespaceState": "EXACT",
        "rootAllowlistState": "ENFORCED",
        "fileCohesionState": "COHESIVE",
        "technicalAxisFirstState": "ZERO",
        "overFolderingState": "NONE",
        "singleFileRequestLeafState": "ZERO",
        "rootDumpState": "ZERO",
        "aliasWorkaround": "ZERO",
        "typeForwardedToWorkaround": "ZERO",
        "staleDuplicateCopyState": "NONE",
        "capabilityLayout": "Contracts/{Availability,Cart,Checkout,Errors,Fulfillment,Orders,Resources,Returns,Seller} + Domain/{Aggregates,Events,ValueObjects} + Application/{Checkout,Composition,Orders,Ports} + Infrastructure/{Adapters,DependencyInjection,Directories,Events,Messaging,Persistence/Migrations}",
        "cohesiveDirectoryState": "InventoryDirectory split into persistence seam + OrderSupply + Availability + Reclaimer + SellerWrite + Lookups partials with OpenInventoryUseCaseGuard in its own file; every Directories/*.cs <= 500 LOC",
        "localizationState": "BOTH_CULTURE_RESX_PRESENT",
        "hardcodedTextState": "ZERO",
        "errorCodeDescriptorOwnership": "Tooba.Inventory.Contracts/Errors/InventoryErrorCatalogContributor.cs",
        "stableCodeOwner": "Tooba.Inventory.Contracts/Errors/InventoryErrorCodes.cs",
        "declaredCodeCount": 29,
        "registeredDescriptorCount": 29,
        "unregisteredDeclaredCode": "NONE",
        "duplicateErrorDescriptorState": "ZERO",
        "foreignOwnedDescriptorNonClaim": "inventory.reservation.retry_limit_reached stays Order-owned and inventory.recovery.* stays outside the Inventory keyspace; neither is declared or registered by Inventory.",
        "resourceSetState": "InventoryErrorResourceSet claims localizationKey.StartsWith(\\"inventory.\\") and delegates to InventoryErrorResources.Manager; both resx files carry an entry for every declared code.",
        "typedFaultMechanismState": "CODE_CARRYING_CONTRACTOPERATIONEXCEPTION",
        "faultCompositionSeam": "Tooba.Inventory.Application/Composition/InventoryOperation.cs",
        "domainFaultCodeMechanism": "CONTRACTOPERATIONEXCEPTION_WITH_STABLE_CODE",
        "domainToOwnContractsEdge": "Tooba.Inventory.Domain -> Tooba.Inventory.Contracts (own module only), a legal self-module boundary edge for stable codes; no foreign module edge added.",
        "legacyRawFaultState": "ZERO",
        "apiResultPatternState": "CANONICAL",
        "loggingState": "CANONICAL",
        "sensitiveLoggingState": "NONE",
        "openTelemetryState": "CANONICAL",
        "correlationTraceState": "CANONICAL",
        "crossModuleBoundaryState": "CONTRACTS_ONLY",
        "crossModuleBoundaryDetail": "Tooba.Inventory.Application/Infrastructure -> Tooba.Offer.Contracts (IOfferLookupGateway, OfferErrorCodes) and Tooba.Catalog.Contracts (ICatalogVariantLookup) only; zero foreign Application/Infrastructure/Domain/Endpoints project edge in any Inventory project.",
        "crossModuleJoinState": "ZERO",
        "crossModulePersistenceState": "ZERO",
        "foreignAppInfraDomainCoupling": "ZERO",
        "persistenceOwnershipState": "CORRECT",
        "schemaMigrationState": "UNCHANGED",
        "migrationFilesChanged": 0,
        "behaviorChange": "NONE",
        "routesChanged": "NONE",
        "statusCodesChanged": "NONE",
        "errorCodesChanged": "NONE",
        "dtoShapeChanged": "NONE",
        "productionProjects": 5,
        "manifestEntryCount": 1,
        "manifestDiskReconciliation": "PASS",
        "microserviceExtractable": true,
        "hostFinalClosureState": "PRESERVED",
        "hostHttpOwnershipState": "ZERO",
        "hostBusinessAuthorityState": "NONE",
        "hostPersistenceAuthorityState": "NONE",
        "durableGuards": ["InventoryModuleAmsc001W3CertGuardTests", "InventoryModuleAmsc001W2StructureGuardTests", "InventoryAmcW1MigrateGuardTests", "InventoryArchitectureGuardTests", "InventoryReleaseIdempotencyTests", "ErrorCatalogUniqueCodeGuardTests", "CartLifetimeSeparationTests", "OrderSupplyFoundationTests", "PaidOrderReservationLifecycleTests", "OrderInventoryRecoveryTests", "CheckoutImplW4InventoryLifecycleTests"],
        "guardsWeakened": "NONE",
        "baselinesWidened": "NONE",
        "blockingResidualDebt": "ZERO",
        "nonBlockingWatch": ["TmarCompleteReferenceStructureGateTests keeps stale hardcoded certified-module lists and also fails on an unrelated Catalog Contracts/Cart namespace mismatch, so a list-only repair inside this Inventory-scoped task could not make the fact green; recorded as inherited drift, not repaired.", "TmarDurableGuardTests pins the repository-global Host root recovery checkpoint (currentHostCheckpoint, Master-Recovery grid wording and a stale 16-module certified list) and is red for reasons unrelated to Inventory; this module-local task must not displace the Host root checkpoint.", "InventoryOutboxRegistration fail-closed unmapped-event branch matches the already-certified Offer/Catalog/Party/Pricing/Tax outbox registrations and is a composition guard, not a boundary fault."],
        "preExistingDriftProof": "The full Host suite has 82 pre-existing failures at the W3 starting HEAD 87101cb4, none Inventory-related (Host/Admin StoreAppearance count guards, Grid/Catalog/Party/Reviews module guards, Fulfillment/Tax/Pricing/Promotion domain tests, the TmarDurableGuardTests Host-root recovery pins, the stale TmarCompleteReferenceStructureGateTests lists and the source-size baseline guards). Inventory appears in none of them and none was touched or weakened.",
        "evidenceRoot": "docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W0..W3/",
        "workflowStop": "USER_REVIEW_INVENTORY_AMSC_001_W3",
        "automaticNextImplementationTask": "NONE"
    }
}
`;

replaceOnce(
    `        "structureHandoffState": "READY_FOR_W3_CERTIFY",
        "waveOutcome": "CERTIFY_UNBLOCKED",
        "evidence": "docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W2/structure.md"
    }
}
`,
    `        "structureHandoffState": "READY_FOR_W3_CERTIFY",
        "waveOutcome": "CERTIFY_UNBLOCKED",
        "evidence": "docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W2/structure.md"
    },
` + w3,
    'append inventoryModuleAmsc001W3 record');

fs.writeFileSync(file, (bom ? '\uFEFF' : '') + text, 'utf8');

const parsed = JSON.parse(text);
if (parsed.inventoryModuleAmsc001W3.verdict !== 'COMPLETE_REFERENCE_PATTERN') {
    throw new Error('W3 record missing after patch');
}
if (parsed.inventoryModuleAmsc001W3.structureCertified !== true) {
    throw new Error('W3 record is not structureCertified');
}
if (parsed.inventoryModuleAmsc001W0.commit !== 'c6917553') {
    throw new Error('W0 commit placeholder not reconciled');
}
const certified = parsed.structureLock.certifiedModules;
if (certified.filter((m) => m === 'Inventory').length !== 1) {
    throw new Error('Inventory must appear exactly once in structureLock.certifiedModules');
}
if (parsed.lastAcceptedTask !== 'TB-TMAR-HOST-ROOT-FINAL-CERT-001') {
    throw new Error('the repository-global Host root checkpoint must stay untouched');
}
console.log('SoT patched: inventoryModuleAmsc001W3 + W0 commit reconciliation + structureLock.certifiedModules (text-anchored).');
