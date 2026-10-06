const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let raw = fs.readFileSync(p, 'utf8');
const bom = raw.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
if (bom) raw = raw.slice(1);
const j = JSON.parse(raw);

j.mediaModuleAmsc001W3 = {
  task: 'TB-TMAR-MEDIA-AMSC-001-W3',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-certify',
  target: 'src/backend/Modules/Media/Tooba.Media.*',
  parentTask: 'TB-TMAR-MEDIA-AMSC-001-W2',
  startingHead: '991551e9',
  state: 'MEDIA_AMSC_001_CERTIFIED',
  verdict: 'COMPLETE_REFERENCE_PATTERN',
  lockVersion: 'ARCH-COMPLETE-002',
  structureState: 'CERTIFIED',
  structureCertified: true,
  manifestCertified: true,
  structureCertifiedUnderArchComplete002: true,
  pathNamespaceState: 'EXACT',
  rootAllowlistState: 'ENFORCED',
  aliasWorkaroundState: 'NONE',
  physicalCopyState: 'CLEAN',
  fileCohesionState: 'COHESIVE',
  largestProductionFileLoc: 135,
  httpApplicability: 'HTTP_OWNING',
  endpointOwnershipState: 'MODULE_ENDPOINTS',
  hostHttpOwnership: 'ZERO',
  routeCount: 5,
  routes: [
    'POST /v1/admin/media/upload',
    'GET /v1/admin/media/',
    'GET /v1/admin/media/{id:guid}',
    'GET /v1/media/{id:guid}',
    'GET /v1/storefront/media/{assetId:guid}'
  ],
  endpointReachableRequests: 4,
  cqrsState: 'MEDIATR_12_5',
  senderOnlyEndpoints: true,
  validatorCoverageState: 'EXHAUSTIVE_3_REQUIRED_PRESENT_0_MISSING_1_NO_VALIDATOR_REQUIRED',
  validatorRequiredCount: 3,
  noValidatorRequiredCount: 1,
  noValidatorRequiredReason: 'GetMediaStorageKeyQuery carries only a route-derived Guid already validated by the paired GetMediaAssetQuery in the same request path',
  stableErrorCodeState: 'CATALOGUED_7_OF_7_DECLARED_SINGLE_OWNER_7_OF_7_DESCRIPTOR_7x2_LOCALIZED',
  declaredCodeGuard: 'MediaErrorCodes.KnownCodes_AND_IsKnown',
  typedFaultSeam: 'ContractOperationException + SemanticException mapped by declared code in MediaOperation',
  localizationState: 'CANONICAL',
  apiResultPatternState: 'CANONICAL',
  loggingState: 'CANONICAL',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  contractsBoundaryState: 'CLEAN',
  crossModuleBoundaryState: 'CONTRACTS_ONLY',
  foreignAppInfraDomainCoupling: 'ZERO',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_OWN_MEDIA_SCHEMA',
  schemaMigrationState: 'UNCHANGED',
  migrationFilesChanged: 0,
  hostFinalClosureState: 'PRESERVED',
  sinkFolderRegressionState: 'ZERO',
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_X3_AND_ALLOWED_DEV_MIGRATE_SEAM_X1',
  hostTouched: false,
  frontendTouched: false,
  manifestChanged: false,
  productionChange: 'ZERO_PRODUCTION_CHANGE_THIS_WAVE_GUARD_SOT_EVIDENCE_ONLY',
  blockingResidualDebt: 'ZERO',
  acceptedRetainedNonConformances: [
    'Legacy-Guid SVG fallback returns HTTP 200 image/svg+xml for a missing asset (locked client contract pinned by HostMediaEvacuationGuardTests).',
    'SVG aria-label Persian presentation text retained (image payload text, not an API message).',
    'MediaOutboxRegistration.GetEventTypeName throws the framework outbox invariant InvalidOperationException with literal text, the repo-wide idiom shared by certified Localization/Content/CustomerProfile; never a user-facing contract and confined to one file.'
  ],
  microserviceExtractable: true,
  durableGuard: 'src/backend/Host/Tooba.Host.Tests/Architecture/MediaModuleAmsc001W3CertGuardTests.cs',
  focusedValidation: 'dotnet test --filter FullyQualifiedName~MediaModuleAmsc001 = 30 passed/0 failed; --filter FullyQualifiedName~Media = 82 passed/3 docker-skipped/0 failed',
  stopGate: 'USER_REVIEW_MEDIA_AMSC_001_W3',
  automaticNextImplementationTask: 'NONE',
  commit: 'PENDING',
  evidence: 'docs/architecture/evidence/TB-TMAR-MEDIA-AMSC-001-W3/certification.md'
};

fs.writeFileSync(p, bom + JSON.stringify(j, null, 2) + '\n');
console.log('W3 SoT block written');
