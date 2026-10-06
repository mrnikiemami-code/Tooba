const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let raw = fs.readFileSync(p, 'utf8');
const bom = raw.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
if (bom) raw = raw.slice(1);
const j = JSON.parse(raw);

j.mediaModuleAmsc001W1 = {
  task: 'TB-TMAR-MEDIA-AMSC-001-W1',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-migrate',
  target: 'src/backend/Modules/Media/Tooba.Media.*',
  parentTask: 'TB-TMAR-MEDIA-AMSC-001-W0',
  startingHead: '06f7de21',
  state: 'MIGRATE_COMPLETE',
  verdict: 'READY_FOR_CERTIFICATION',
  lockVersion: 'ARCH-COMPLETE-002',
  foundationState: 'FOUNDATION_READY',
  ownershipState: 'correct',
  fileCohesionState: 'COHESIVE',
  cohesionSplitPerformed: [
    'Infrastructure/Assets/MediaDirectory.cs -> MediaDirectory.cs (shared seam) + MediaDirectory.Upload.cs + MediaDirectory.Queries.cs',
    'Contracts/Assets/MediaAssetContracts.cs -> Contracts/Assets/IMediaAssetUploadPort.cs'
  ],
  localizationState: 'CANONICAL',
  apiResultPatternState: 'CANONICAL',
  stableErrorCodeState: 'CATALOGUED_7_OF_7_DECLARED_CODES_SINGLE_OWNER',
  declaredCodeGuard: 'MediaErrorCodes.KnownCodes_AND_IsKnown',
  typedFaultSeam: 'ContractOperationException + SemanticException mapped by declared code in MediaOperation',
  legacyPlatformFaultState: 'ZERO_PLATFORM_HTTP_EXCEPTION_IN_MEDIA',
  cqrsState: 'COMPLIANT',
  deadEndpointPortBypassRemoved: 'MediaAssetServing.TryServeStoredMediaAsync removed; ServeAsync (ISender) is the single serving path',
  validatorCoverageState: 'EXHAUSTIVE_3_OF_3_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED',
  validationCodes: 'MediaValidationCodes_NOT_REGISTERED_AS_CATALOG_DESCRIPTORS_MAPPED_THROUGH_FOUNDATION_VALIDATION_FAILED',
  endpointReachableRequests: 4,
  validatorRequiredCount: 3,
  noValidatorRequiredCount: 1,
  hardCodedCultureRemoved: 'MediaAdminEndpoints.ResolveTitle now uses IErrorMessageLocalizer + IRequestLocaleResolver',
  contractsBoundaryState: 'CLEAN',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY_ZERO_FOREIGN_APP_INFRA_DOMAIN_EDGES',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_OWN_MEDIA_SCHEMA',
  endpointOwnershipState: 'MODULE_OWNED',
  hostResidueState: 'UNCHANGED_ALLOWED_COMPOSITION_ROOT_X3_AND_ALLOWED_DEV_MIGRATE_SEAM_X1',
  schemaMigrationState: 'UNCHANGED',
  migrationFilesChanged: 0,
  behaviorPreservationRisk: 'LOW',
  acceptedObservableDeltas: [
    'media.storage.unavailable now maps to its already-registered canonical catalog status 503 instead of the inconsistent inline 400 used only for an unreachable server-derived storage key.',
    'Per-field validation identity is now media.validation.* mapped through the foundation validation.failed descriptor; HTTP 400 and the errorCode/errors envelope are unchanged.',
    'Per-item batch title now resolves through the request locale instead of a hard-coded fa culture.',
    'Upload-failure log uses a structured template with the same event name instead of a message-equals-code call.'
  ],
  acceptedRetainedNonConformances: [
    'Legacy-Guid SVG fallback returns HTTP 200 image/svg+xml for a missing asset (locked client contract).',
    'SVG aria-label Persian presentation text retained (image payload text, not an API message).'
  ],
  hostTouched: false,
  frontendTouched: false,
  durableGuard: 'src/backend/Host/Tooba.Host.Tests/Architecture/MediaModuleAmsc001W1MigrateGuardTests.cs',
  focusedValidation: 'build Media.Infrastructure/Media.Endpoints/Tooba.Host/Tooba.Host.Tests + dotnet test --filter FullyQualifiedName~Media = 65 passed, 3 docker-skipped, 0 failed; related focused guards = 50 passed, 3 skipped, 0 failed',
  structureHandoffState: 'REQUIRED',
  commit: 'PENDING',
  waveOutcome: 'STRUCTURE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-MEDIA-AMSC-001-W1/migrate.md'
};

fs.writeFileSync(p, bom + JSON.stringify(j, null, 2) + '\n');
console.log('W1 SoT block written');
