const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let src = fs.readFileSync(p, 'utf8');
const tail = '\r\n}\r\n';
if (!src.endsWith(tail)) { console.error('TAIL_MISMATCH'); process.exit(1); }
const eol = '\r\n';
const lines = [
'    "identityModuleAmsc001W0": {',
'        "task": "TB-TMAR-IDENTITY-AMSC-001-W0",',
'        "mode": "ARCHITECT_DIRECT_AMSC",',
'        "skill": "tooba-architecture-analyze",',
'        "target": "src/backend/Modules/Identity/Tooba.Identity.*",',
'        "startingHead": "290953e8",',
'        "parentTask": null,',
'        "state": "ANALYZE_COMPLETE",',
'        "verdict": "READY_TO_MIGRATE",',
'        "lockVersion": "ARCH-COMPLETE-002",',
'        "priorCertificationState": "CERTIFIED_UNDER_IDENTITY_AMC_001_COMPLETE_REFERENCE_PATTERN_RE_AUDITED_AGAINST_CURRENT_AMSC_BAR",',
'        "foundationState": "FOUNDATION_READY",',
'        "ownershipState": "correct",',
'        "fileCohesionState": "COHESIVE",',
'        "oversizedGodFileState": "NONE",',
'        "largestHandWrittenFileLoc": 546,',
'        "localizationState": "CANONICAL_PRESENTATION_WITH_HARDCODED_DOMAIN_FAULT_TEXT_AND_MISSING_FA_RESX",',
'        "apiResultPatternState": "CANONICAL",',
'        "stableErrorCodeState": "PARTIAL_9_CATALOGUED_3_OTP_DELIVERY_CODES_EMITTED_UNREGISTERED_AS_RAW_STRING_LITERALS",',
'        "loggingState": "CANONICAL",',
'        "sensitiveLoggingState": "NONE",',
'        "openTelemetryState": "CANONICAL",',
'        "correlationTraceState": "CANONICAL",',
'        "cqrsState": "COMPLIANT",',
'        "validatorCoverageState": "GUARDED_EXHAUSTIVE_6_OF_13_REQUIRED_3_RECLASSIFIED_REQUIRED_STRICTER",',
'        "contractsBoundaryState": "CLEAN",',
'        "crossModuleCouplingState": "LEGAL_CONTRACTS_ONLY",',
'        "crossModuleContractsReference": "Tooba.Identity.Application -> Tooba.CustomerProfile.Contracts (ICustomerProfileDirectory) only",',
'        "crossModuleJoinState": "NONE",',
'        "persistenceOwnershipState": "CORRECT",',
'        "endpointOwnershipState": "MODULE_OWNED",',
'        "hostResidueState": "ALLOWED_COMPOSITION_ROOT_PLUS_GLOBAL_HOST_AUTH_PLATFORM_BOUNDARY",',
'        "schemaMigrationState": "UNCHANGED",',
'        "behaviorPreservationRisk": "LOW",',
'        "folderGranularityState": "PROFESSIONAL_SHALLOW_WITH_TWO_DEVIATIONS",',
'        "structureHandoffState": "REQUIRED",',
'        "canonicalReferenceUsed": "BulkInquiry.Contracts Errors+Resources co-location / Offer.Contracts Errors / CustomerProfileOperation ContractOperationException seam / AddressBook+Cart+Content capability-first Application / BuildingBlocks",',
'        "blockers": [',
'            "Contracts/Problems/ is the pre-ARCH-COMPLETE-002 folder name for the stable error-code + catalog + resource-set capability; canonical vocabulary is Contracts/Errors/ (BulkInquiry, Offer, AccessControl, Cart, Catalog, Content, Fulfillment, AddressBook, CustomerProfile, Order, Party, Media).",',
'            "IdentityErrors.fa.resx is missing; all 9 catalogued identity.* codes fall back to English titles while 23 other modules ship a .fa.resx sibling.",',
'            "Three OTP delivery machine codes (rate_limited, invalid_destination, unconfigured) are emitted as raw string literals and have no ErrorDescriptor, so SafeErrorMapper cannot classify them.",',
'            "OtpDeliveryProviderSender throws raw InvalidOperationException with a string code, and RequestOtpLoginCommandHandler plus ChangePasswordCommandHandler classify with local catch blocks instead of the canonical operation seam; two parallel classification paths exist in one module.",',
'            "Six hard-coded Persian fault messages in Domain/Rules/LoginIdentifierNormalizer.cs (never client-facing, but untyped prose in a Domain rule).",',
'            "Application/Validators/IdentityValidationCodes.cs sits in a technical-axis root while all six consumers are Auth/Validators/*.",',
'            "Four unreferenced internal transport response records in Endpoints/Auth/IdentityAuthHttpModels.cs (RegisterResponse, SessionResponse, AcceptedResponse, MeResponse).",',
'            "Twelve duplicate using Tooba.Identity.Contracts.Auth directives across eight files (CS0105 warnings).",',
'            "Validator coverage reclassification: LoginWithPasswordCommand, RefreshAuthSessionCommand and CompleteOtpLoginCommand carry real body transport input but are classified NO_VALIDATOR_REQUIRED."',
'        ],',
'        "noArchitectureDecisionRequired": true,',
'        "hostTouched": false,',
'        "productionCodeChanged": false,',
'        "commit": "PENDING_W0_COMMIT",',
'        "evidence": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0/analyze.md"',
'    }'
];
const out = src.slice(0, src.length - tail.length) + ',' + eol + lines.join(eol) + tail;
fs.writeFileSync(p, out, 'utf8');
JSON.parse(fs.readFileSync(p, 'utf8'));
console.log('OK appended; json valid');
