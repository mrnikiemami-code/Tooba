const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let raw = fs.readFileSync(p, 'utf8');
const bom = raw.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
if (bom) raw = raw.slice(1);
const j = JSON.parse(raw);

j.mediaModuleAmsc001W0 = {
  task: 'TB-TMAR-MEDIA-AMSC-001-W0',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-analyze',
  target: 'src/backend/Modules/Media/Tooba.Media.*',
  startingHead: '70e55d40',
  parentTask: null,
  state: 'ANALYZE_COMPLETE',
  verdict: 'READY_TO_MIGRATE',
  lockVersion: 'ARCH-COMPLETE-002',
  priorCertificationState: 'AMC_001_W1_W4_CERTIFIED_MEDIA_AMC_001_NOT_AMSC',
  foundationState: 'FOUNDATION_READY',
  ownershipState: 'MUST_SPLIT',
  fileCohesionState: 'MULTI_RESPONSIBILITY_COHESION_VIOLATION',
  oversizedGodFileState: 'NONE — MediaDirectory.cs 209 LOC (two responsibilities); largest production file; no ARCH-SIZE-001 baseline entry for Media',
  localizationState: 'HARDCODED_TEXT',
  apiResultPatternState: 'CANONICAL_WITH_TWO_DOCUMENTED_DEVIATIONS',
  stableErrorCodeState: 'CATALOGUED_7_OF_7_DECLARED_CODES_SINGLE_OWNER',
  loggingState: 'CANONICAL',
  sensitiveLoggingState: 'NONE',
  openTelemetryState: 'CANONICAL',
  correlationTraceState: 'CANONICAL',
  cqrsState: 'PARTIAL',
  validatorCoverageState: 'EXHAUSTIVE_3_OF_3_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED',
  contractsBoundaryState: 'CLEAN',
  crossModuleCouplingState: 'LEGAL_CONTRACTS_ONLY_ZERO_FOREIGN_APP_INFRA_DOMAIN_EDGES',
  crossModuleJoinState: 'NONE',
  persistenceOwnershipState: 'CORRECT_OWN_MEDIA_SCHEMA',
  endpointOwnershipState: 'MODULE_OWNED',
  httpApplicability: 'HTTP_OWNING',
  endpointReachableRequests: 4,
  validatorRequiredCount: 3,
  noValidatorRequiredCount: 1,
  routeCount: 5,
  hostResidueState: 'ALLOWED_COMPOSITION_ROOT_X3_AND_ALLOWED_DEV_MIGRATE_SEAM_X1',
  hostMediaHttpOwnership: 'ZERO',
  schemaMigrationState: 'UNCHANGED',
  behaviorPreservationRisk: 'LOW',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  structureHandoffState: 'REQUIRED',
  canonicalReferenceUsed: 'Localization/Inventory typed-fault seam + declared-code guard + transport validation codes; Offer CQRS/endpoint shape; BuildingBlocks Result+ApiResponseFactory+ErrorDescriptor+IErrorResourceSet+IErrorMessageLocalizer; CustomerProfile/AddressBook capability-first shallow layout',
  blockers: [
    'MUST_SPLIT: Infrastructure/Assets/MediaDirectory.cs mixes the upload pipeline with library queries plus shared private helpers.',
    'MUST_SPLIT: Contracts/Assets/MediaAssetContracts.cs is a generic mixed contract file holding a single IMediaAssetUploadPort behind #pragma disable CS1591.',
    'CQRS gap: Endpoints/Admin/MediaAssetServing.TryServeStoredMediaAsync injects IMediaDirectory + IMediaObjectStore directly instead of ISender, and the helper is dead production code.',
    'Localization gap: MediaAdminEndpoints.ResolveTitle reads MediaErrors.resx with a hard-coded fa culture, bypassing the canonical IErrorMessageLocalizer that ApiResponseFactory already uses.',
    'Fault-mechanism gap: Infrastructure raises the legacy Host-platform PlatformHttpException (6 sites in MediaDirectory, 5 in LocalFileMediaStore) instead of the canonical typed ContractOperationException/SemanticException.',
    'Transport validation gap: all three validators emit the single coarse media.validation.failed code instead of distinct media.validation.* transport-identity codes.'
  ],
  acceptedRetainedNonConformances: [
    'Legacy-Guid SVG fallback: MediaAssetServing.PlaceholderSvg returns HTTP 200 image/svg+xml for a missing asset; pinned by HostMediaEvacuationGuardTests and the MediaDamTests serving contract, preserved unchanged.',
    'SVG aria-label="نمایش موقت رسانه" is presentation asset text inside an image payload, not a localized API message; retained.'
  ],
  noArchitectureDecisionRequired: true,
  hostTouched: false,
  productionCodeChanged: false,
  commit: 'PENDING',
  waveOutcome: 'MIGRATE_UNBLOCKED',
  evidence: 'docs/architecture/evidence/TB-TMAR-MEDIA-AMSC-001-W0/analyze.md'
};

fs.writeFileSync(p, bom + JSON.stringify(j, null, 2) + '\n');
console.log('W0 SoT block written');
