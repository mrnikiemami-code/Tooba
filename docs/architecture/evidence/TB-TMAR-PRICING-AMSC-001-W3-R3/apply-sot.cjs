// TB-TMAR-PRICING-AMSC-001-W3-R3 — SoT closure for the fresh certification.
// 1. re-add "Pricing" exactly once to structureLock.certifiedModules;
// 2. append the pricingAmsc001W3R3 certification block before the final closing brace.
// Byte-preserving: 2-space indent, no BOM, LF.
const fs = require('fs');

const path = 'docs/architecture/tmar-current-state.json';
const raw = fs.readFileSync(path, 'utf8');
const bom = raw.startsWith('\uFEFF') ? '\uFEFF' : '';
let body = bom ? raw.slice(1) : raw;

const lockStart = body.indexOf('"structureLock": {');
if (lockStart < 0) {
    throw new Error('structureLock not found');
}
const listStart = body.indexOf('"certifiedModules": [', lockStart);
if (listStart < 0) {
    throw new Error('certifiedModules not found inside structureLock');
}
const listEnd = body.indexOf('\n    ],', listStart);
if (listEnd < 0) {
    throw new Error('certifiedModules closing bracket not found');
}

const listText = body.slice(listStart, listEnd);
const names = [...listText.matchAll(/"([A-Z][A-Za-z]*)"/g)].map((m) => m[1]);
if (names.includes('Pricing')) {
    throw new Error('Pricing already present in structureLock.certifiedModules');
}
if (names.length !== 24) {
    throw new Error(`expected 24 certified modules before W3-R3, found ${names.length}`);
}

const updatedList = listText.replace(/\n(\s*)"Notification"/, '\n$1"Notification",\n$1"Pricing"');
if (updatedList === listText) {
    throw new Error('failed to append Pricing to certifiedModules');
}
body = body.slice(0, listStart) + updatedList + body.slice(listEnd);

const block = `  "pricingAmsc001W3R3": {
    "task": "TB-TMAR-PRICING-AMSC-001-W3-R3",
    "parentTask": "TB-TMAR-PRICING-AMSC-001-W3-R2",
    "mode": "FRESH_CERTIFY_AFTER_INTERNAL_ONLY_STRUCTURE_REPAIR",
    "skill": "tooba-architecture-certify",
    "target": "src/backend/Modules/Pricing/Tooba.Pricing.*",
    "startingHead": "7159c8f773c1faa9b4b6d425b19067f50ca27572",
    "certificationCommitState": "REPORTED_IN_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_SOT_SHA",
    "state": "PRICING_AMSC_001_RECERTIFIED",
    "verdict": "COMPLETE_REFERENCE_PATTERN",
    "lockVersion": "ARCH-COMPLETE-002",
    "structureCertified": true,
    "currentStructureAuthority": "TB-TMAR-PRICING-AMSC-001-W3-R2",
    "currentStructureCommit": "7159c8f773c1faa9b4b6d425b19067f50ca27572",
    "structureGateSource": "TB-TMAR-PRICING-AMSC-001-W3-R2 (READY_FOR_CERTIFY, independently re-verified against disk in this wave)",
    "supersededCertificationTask": "TB-TMAR-PRICING-AMSC-001-W3",
    "supersededCertificationCommit": "3c2cc61e7c61813ac72773ccdb8bb16317cafe70",
    "httpApplicability": "NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER",
    "endpointProjectState": "ABSENT",
    "endpointOwnershipState": "NOT_APPLICABLE_INTERNAL_ONLY",
    "moduleOwnedRouteCount": 0,
    "hostOwnedRouteCount": 0,
    "endpointReachableRequests": 0,
    "cqrsState": "NOT_APPLICABLE_INTERNAL_ONLY",
    "validatorCoverageState": "NOT_APPLICABLE_INTERNAL_ONLY",
    "solutionProjectCount": 5,
    "folderGranularityState": "PROFESSIONAL_SHALLOW",
    "pathNamespaceState": "EXACT",
    "physicalCopyState": "CLEAN",
    "rootAllowlistState": "ENFORCED",
    "solutionExplorerState": "CANONICAL",
    "stableCodeHome": "Tooba.Pricing.Contracts.Errors.PricingErrorCodes",
    "declaredCodeCount": 11,
    "registeredDescriptorCount": 11,
    "resourceKeyCountEn": 11,
    "resourceKeyCountFa": 11,
    "presentationRegistrationState": "INFRASTRUCTURE_MODULE_EXACTLY_ONCE",
    "presentationRegistrationDetail": "The single IErrorCatalogContributor -> PricingErrorCatalogContributor and IErrorResourceSet -> PricingErrorResourceSet registrations live exactly once in PricingModule.AddServices (Infrastructure/DependencyInjection); the concrete descriptor/resource types stay Contracts-owned in Tooba.Pricing.Contracts.Errors, matching the certified Inventory INTERNAL_ONLY precedent.",
    "typedFaultSeam": "Tooba.Pricing.Application.Composition.PricingOperation",
    "typedFaultMechanisms": "ContractOperationException (by declared code) + SemanticException",
    "messageClassification": "ZERO",
    "crossModuleBoundaryState": "LEGAL_CONTRACTS_ONLY_BOTH_DIRECTIONS",
    "foreignAppInfraDomainCoupling": "ZERO",
    "crossModuleJoinState": "NONE",
    "persistenceOwnershipState": "CORRECT_OWN_PRICING_SCHEMA_OWN_OUTBOX",
    "schemaMigrationState": "UNCHANGED",
    "migrationFilesChanged": 0,
    "microserviceExtractable": true,
    "hostAuthorityState": "ZERO_BUSINESS_ZERO_HTTP",
    "hostFinalClosure": "PRESERVED",
    "hostFinalClosureDetail": "No Host/Pricing folder; no Host file owns PricingDbContext/AuthoredPrice/IPriceDirectory; Program.cs maps no Pricing route group and registers no Pricing presentation extension; Host keeps the composition-root ProjectReference only. currentHostCheckpoint HOST_ROOT_FINAL_CERTIFIED and lastAcceptedTask TB-TMAR-HOST-ROOT-FINAL-CERT-001 preserved.",
    "productionCodeChanged": false,
    "guardsWeakened": "NONE",
    "baselinesWidened": "NONE",
    "guardsAdded": "PricingModuleAmsc001W3R3CertGuardTests",
    "durableGuard": "src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3R3CertGuardTests.cs",
    "manifestCertificationState": "PROMOTED_TO_CERTIFIED_MODULES_26_PRICING_ENTRY_REFRESHED",
    "structureLockState": "PRICING_PRESENT_EXACTLY_ONCE",
    "postCertRecoveryState": "POST_CERT_RECOVERY_RECONCILIATION_REQUIRED",
    "postCertRecoveryDetail": "Deliberately not repaired in this Certify wave: pricingAmsc001W0 still carries PENDING_THIS_COMMIT placeholders in commit/commitFull, pricingAmsc001W1 and pricingAmsc001W2 carry parentCommit only with no final commit/commitFull field, and the historical pricingAmsc001W3R1 block has no final commit SHA of its own.",
    "evidenceRoot": "docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/",
    "workflowStop": "USER_REVIEW_PRICING_AMSC_001_W3_R3",
    "automaticNextImplementationTask": "NONE",
    "evidence": "docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/certification.md"
  }
}
`;

const tail = '  }\n}\n';
if (!body.endsWith(tail)) {
    throw new Error('SoT does not end with the expected final block closing brace');
}
body = body.slice(0, body.length - tail.length) + '  },\n' + block;

const parsed = JSON.parse(body);
if (parsed.structureLock.certifiedModules.filter((x) => x === 'Pricing').length !== 1) {
    throw new Error('Pricing must appear exactly once in certifiedModules');
}

fs.writeFileSync(path, bom + body, 'utf8');
console.log('certifiedModules', parsed.structureLock.certifiedModules.length);
console.log('pricingAmsc001W3R3', parsed.pricingAmsc001W3R3.state);
