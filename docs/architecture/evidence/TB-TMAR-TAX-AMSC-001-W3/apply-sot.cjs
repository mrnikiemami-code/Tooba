const fs = require('fs');
const path = require('path');

const repo = process.argv[2] || '.';
const manifestPath = path.join(repo, 'docs/architecture/tmar-module-structure-manifests.json');
const statePath = path.join(repo, 'docs/architecture/tmar-current-state.json');

const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));

// --- 1. Move Tax from preCertModules into the certified modules[] array -----------------------
const preCert = manifest.preCertModules || [];
const taxIndex = preCert.findIndex(m => m.module === 'Tax');
if (taxIndex < 0) {
  throw new Error('Tax not found in preCertModules');
}
const tax = preCert[taxIndex];
preCert.splice(taxIndex, 1);
manifest.preCertModules = preCert;

if (manifest.modules.some(m => m.module === 'Tax')) {
  throw new Error('Tax already certified in modules[]');
}

tax.structureCertified = true;
tax.certificationState = 'TAX_AMSC_001_CERTIFIED';
tax.currentCertificationTask = 'TB-TMAR-TAX-AMSC-001-W3';
tax.currentVerdict = 'COMPLETE_REFERENCE_PATTERN';
tax.currentValidatorMatrixState = 'NOT_APPLICABLE_INTERNAL_ONLY_ZERO_ENDPOINT_REACHABLE_REQUESTS';
tax.certificationNote =
  'CERTIFIED by TB-TMAR-TAX-AMSC-001-W3 (tooba-architecture-certify) after the Structure wave W2 (eff3cf5b). ' +
  'Wave lineage W0 36d243cc -> W1 fa87201a -> W2 eff3cf5b -> W3. Tax is INTERNAL_ONLY: the W0 applicability gate ' +
  'proved 0 module HTTP routes and 0 endpoint-reachable requests, so under the certify 0b applicability gate the ' +
  'endpoint ownership, CQRS and validator matrices are NOT_APPLICABLE_INTERNAL_ONLY while the ceremonial ' +
  'Tooba.Tax.Endpoints project, its empty /v1/tax route group, its Host using/MapTaxModule composition call and its ' +
  'Host/Tests project references were retired by W2 (the certified Inventory/Pricing precedent: four production ' +
  'projects + Tests, presentation registration owned exactly once by the Infrastructure composition root ' +
  'TaxModule.AddServices). All applicable ARCH-COMPLETE-002 gates re-derived from disk and PASS: the single canonical ' +
  'Contracts stable-code home (11 declared tax.* codes) with 11 descriptors registered exactly once by ' +
  'TaxErrorCatalogContributor and 11 EN + 11 FA bilingual resources owned and registered once by TaxErrorResourceSet ' +
  '(checkout.tax.unavailable stays Order-owned and is deliberately absent); the typed TaxOperation seam classifying by ' +
  'typed code only (TaxErrorCodes.IsKnown) with zero message-prose classification; PROFESSIONAL_SHALLOW ' +
  'capability-first structure (Contracts/{Dtos,Errors,Ports,Resources}, Domain/{Aggregates,Enums,Events,Policies} with ' +
  'the single GlobalUsings namespace bridge at the root, Application/{Composition,Ports}, ' +
  'Infrastructure/{Adapters,DependencyInjection,Events,Outbox,Persistence}) with five solution projects under ' +
  '/Modules/Tax/, exact path<->namespace and enforced root allowlists; Contracts-only boundaries with zero foreign ' +
  'Application/Infrastructure/Domain edge, zero cross-module persistence and zero cross-module join; the single ' +
  'migration 20260823190000_InitialTax byte-identical to the W0 baseline; and the preserved ' +
  'HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED / HOST_ROOT_FINAL_CERTIFIED checkpoints. Verdict ' +
  'COMPLETE_REFERENCE_PATTERN; structureCertified true; microserviceExtractable = ' +
  'TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE_EXACT_PATH_NAMESPACE.';
tax.evidence = 'docs/architecture/evidence/TB-TMAR-TAX-AMSC-001-W3/certification.md';
tax.structureAuthorityTask = 'TB-TMAR-TAX-AMSC-001-W2';
tax.structureAuthorityCommit = 'eff3cf5b846d33f90652ee691fa23048ffa45f48';
tax.structureHandoffState = 'READY_FOR_CERTIFY_CONSUMED_BY_W3';
delete tax.structureState;

manifest.modules.push(tax);
fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n');

// --- 2. structureLock.certifiedModules + SoT record -------------------------------------------
const sot = JSON.parse(fs.readFileSync(statePath, 'utf8'));
const lock = sot.structureLock.certifiedModules;
if (!lock.includes('Tax')) {
  lock.push('Tax');
}

const w2 = sot.taxAmsc001W2;
if (w2 && w2.commit === 'PENDING_W2_COMMIT') {
  w2.commit = 'eff3cf5b';
  w2.commitFull = 'eff3cf5b846d33f90652ee691fa23048ffa45f48';
}

sot.taxAmsc001W3 = {
  task: 'TB-TMAR-TAX-AMSC-001-W3',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-certify',
  target: 'src/backend/Modules/Tax/Tooba.Tax.*',
  parentTask: 'TB-TMAR-TAX-AMSC-001-W2',
  parentCommit: 'eff3cf5b',
  startingHead: 'eff3cf5b846d33f90652ee691fa23048ffa45f48',
  currentStructureAuthority: 'TB-TMAR-TAX-AMSC-001-W2',
  currentStructureCommit: 'eff3cf5b846d33f90652ee691fa23048ffa45f48',
  currentStructureState: 'READY_FOR_CERTIFY_CONSUMED_BY_W3',
  state: 'TAX_AMSC_001_CERTIFIED',
  verdict: 'COMPLETE_REFERENCE_PATTERN',
  lockVersion: 'ARCH-COMPLETE-002',
  structureCertified: true,
  httpApplicability: 'INTERNAL_ONLY',
  applicabilityGate: 'NOT_APPLICABLE_INTERNAL_ONLY_ENDPOINT_CQRS_VALIDATOR',
  endpointOwnershipState: 'NOT_APPLICABLE_INTERNAL_ONLY',
  moduleOwnedRouteCount: 0,
  hostOwnedRouteCount: 0,
  endpointReachableRequests: 0,
  cqrsState: 'NOT_APPLICABLE_INTERNAL_ONLY',
  validatorMatrixState: 'EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED',
  stableErrorCodeState: 'SINGLE_CANONICAL_CONTRACTS_HOME_11_DECLARED_11_REGISTERED_11_LOCALIZED',
  errorDescriptorOwnershipState: 'UNIQUE_TAX_OWNED_11_NO_FOREIGN_DESCRIPTOR_CLAIMED',
  foreignDescriptorClaimed: 'NONE_CHECKOUT_TAX_UNAVAILABLE_STAYS_ORDER_OWNED',
  typedFaultSeamState: 'CANONICAL_TAXOPERATION_CODE_ONLY',
  presentationRegistrationState: 'INFRASTRUCTURE_COMPOSITION_ROOT_EXACTLY_ONCE',
  localizationState: 'CANONICAL_11_EN_11_FA_COMPOSED_LOCALIZER',
  apiResultPatternState: 'CANONICAL_NOT_APPLICABLE_NO_HTTP_ROUTE',
  loggingState: 'CANONICAL_ZERO_CALL_SITES',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  pathNamespaceState: 'EXACT',
  rootAllowlistState: 'ENFORCED',
  aliasWorkaroundState: 'NONE',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  physicalCopyState: 'CLEAN',
  fileCohesionState: 'COHESIVE',
  foreignApplicationInfrastructureDomainEdges: 'ZERO',
  crossModuleCouplingState: 'NONE',
  crossModuleJoinState: 'ZERO',
  persistenceOwnershipState: 'CORRECT_OWN_TAX_SCHEMA_OWN_OUTBOX',
  schemaState: 'UNCHANGED',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT',
  hostFinalClosureState: 'HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED',
  hostAuthorityState: 'ZERO_BUSINESS_ZERO_HTTP',
  manifestCertificationState: 'PROMOTED_TO_CERTIFIED_MODULES_31_TAX_ENTRY_ADDED',
  structureLockState: 'TAX_PRESENT_EXACTLY_ONCE',
  preCertPromotionState: 'MOVED_NOT_COPIED_PRE_CERT_ENTRY_REMOVED',
  microserviceExtractable: true,
  microserviceExtractableState: 'TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE_EXACT_PATH_NAMESPACE',
  guardTest: 'src/backend/Host/Tooba.Host.Tests/Architecture/TaxModuleAmsc001W3CertGuardTests.cs',
  guardTestResult: '8_OF_8_PASSED',
  productionCodeChanged: false,
  schemaChange: 'NONE',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  evidenceRoot: 'docs/architecture/evidence/TB-TMAR-TAX-AMSC-001-W3/',
  workflowStop: 'USER_REVIEW_TAX_AMSC_001_W3',
  stopGate: 'USER_REVIEW_TAX_AMSC_001_W3',
  automaticNextImplementationTask: 'NONE',
  evidence: 'docs/architecture/evidence/TB-TMAR-TAX-AMSC-001-W3/certification.md',
  commit: 'PENDING_W3_COMMIT',
  commitFull: 'PENDING_W3_COMMIT',
};

fs.writeFileSync(statePath, JSON.stringify(sot, null, 2) + '\n');
console.log('manifest modules:', manifest.modules.length, 'preCert:', manifest.preCertModules.length);
console.log('certifiedModules:', lock.length);
