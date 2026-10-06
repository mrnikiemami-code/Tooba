const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let raw = fs.readFileSync(p, 'utf8');
const bom = raw.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
if (bom) raw = raw.slice(1);
const j = JSON.parse(raw);
j.localizationModuleAmsc001W1 = {
  task: 'TB-TMAR-LOCALIZATION-AMSC-001-W1',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-migrate',
  target: 'src/backend/Modules/Localization/Tooba.Localization.*',
  parentTask: 'TB-TMAR-LOCALIZATION-AMSC-001-W0',
  startingHead: '5a0b882b',
  state: 'MIGRATE_COMPLETE',
  verdict: 'MIGRATED',
  lockVersion: 'ARCH-COMPLETE-002',
  foundationState: 'FOUNDATION_READY',
  foundationCreated: 'NONE_REQUIRED',
  faultTypingState: 'SINGLE_TYPED_MECHANISM_BY_LAYER',
  faultTypingAction: 'Application/Composition/LanguageMappings.cs ParseDirection/ParseCalendar now throw ContractOperationException(LanguageErrorCodes.X) instead of a Domain-style SemanticException; LocalizationOperation maps ContractOperationException and SemanticException by declared typed code only.',
  declaredCodeGuard: 'LanguageErrorCodes.KnownCodes + IsKnown(string?) added (InventoryErrorCodes precedent) so codes owned by another module propagate untouched to the global exception boundary.',
  ownedCodeMappingPreserved: '19_OF_19_DECLARED_CODES_REMAIN_THE_MAPPED_SET',
  validatorCodeSeparation: 'ADDED_LOCALIZATIONVALIDATIONCODES_7_TRANSPORT_CODES',
  validatorCodeRegistration: 'NOT_REGISTERED_BY_DESIGN_FOUNDATION_VALIDATION_FAILED_DESCRIPTOR',
  validatorCodeConsumerCount: 3,
  acceptedObservableDelta: 'Transport-shape rejections now surface precise localization.validation.* codes through the foundation validation.failed descriptor instead of the coarse business codes; HTTP 400 unchanged; domain-level Invalid* codes remain owned and registered.',
  localizationState: 'CANONICAL_19_DECLARED_CODES_19_OWNED_DESCRIPTORS_BILINGUAL_RESX_AND_RESOURCE_SET',
  apiResultPatternState: 'CANONICAL',
  stableErrorCodeState: 'REGISTERED_SINGLE_OWNER_PER_CODE',
  loggingState: 'CANONICAL',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  cqrsState: 'COMPLIANT',
  validatorCoverageState: 'EXHAUSTIVE_4_OF_4_REQUIRED_PRESENT',
  contractsBoundaryState: 'CLEAN',
  crossModuleCouplingState: 'NONE_ZERO_FOREIGN_MODULE_EDGES',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT',
  schemaMigrationState: 'UNCHANGED',
  migrationFilesChanged: 0,
  behaviorChange: 'NONE_EXCEPT_DOCUMENTED_VALIDATION_CODE_PRECISION',
  routesChanged: 'NONE',
  statusCodesChanged: 'NONE',
  declaredErrorCodesChanged: 'NONE',
  dtoShapeChanged: 'NONE',
  filesMoved: 0,
  filesRenamed: 0,
  productionFilesChanged: 7,
  productionFilesCreated: 1,
  durableGuard: 'src/backend/Host/Tooba.Host.Tests/Architecture/LocalizationModuleAmsc001W1MigrateGuardTests.cs (7 facts)',
  verification: {
    localizationApplicationBuild: 'succeeded 0 warnings 0 errors',
    hostTestsBuild: 'succeeded 0 errors',
    focusedLocalizationFilter: '36 passed / 0 failed / 0 skipped',
    preExistingUnrelatedFailure: 'TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment fails on Tooba.Catalog.Contracts/Cart namespace mismatch (tracked at HEAD, last touched 56692b7c); Catalog is outside authorized scope and was not repaired.'
  },
  outOfScopeResidual: [
    'Catalog.Infrastructure reads localization.languages with raw SQL (CatalogDirectory.LoadPreferredLanguageIdsAsync + migration 20260909130000_AddQuantityFoundation); Catalog-owned violation against the Localization schema, outside this task scope.',
    'LocalizationErrorResourceSet.Owns() matches the whole localization. prefix while only localization.language.* is declared; currently harmless, recorded as watch.',
    'AddLocalizationEndpointPresentation() is an empty Host registration seam retained for behavior.'
  ],
  structureHandoffState: 'REQUIRED',
  waveOutcome: 'STRUCTURE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-LOCALIZATION-AMSC-001-W1/migrate.md'
};
fs.writeFileSync(p, bom + JSON.stringify(j, null, 2) + '\n');
console.log('W1 SoT block written');
