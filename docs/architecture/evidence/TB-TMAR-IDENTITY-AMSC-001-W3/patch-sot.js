// TB-TMAR-IDENTITY-AMSC-001-W3 — SoT reconciliation (docs/architecture/tmar-current-state.json).
// Text-anchored and idempotent: it does NOT re-serialize the whole document, so unrelated records
// keep their exact formatting/line endings and no unrelated diff is produced.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
let text = fs.readFileSync(file, 'utf8');

// The document is CRLF; author patterns in LF and normalize to the on-disk ending.
const crlf = (s) => s.replace(/\n/g, '\r\n');

function replaceOnce(from, to, label) {
    const f = crlf(from);
    const count = text.split(f).length - 1;
    if (count !== 1) {
        throw new Error(`anchor not unique (${count}) for ${label}`);
    }
    text = text.replace(f, crlf(to));
}

// 1. Reconcile the pre-existing AMC-001 record to the AMSC-001 certification (no duplicate record).
replaceOnce(
    `        "structureCertifiedUnderArchComplete002": true,
        "structureState": "READY_FOR_CERTIFY",
        "folderGranularity": "PROFESSIONAL_SHALLOW",
        "solutionExplorer": "CANONICAL_MODULES_IDENTITY",`,
    `        "structureCertifiedUnderArchComplete002": true,
        "structureState": "CERTIFIED",
        "amsc001Certified": true,
        "amsc001CertificationNote": "Re-certified under the AMSC-001 four-wave pipeline (W0 91eec1fd / W1 93a6b192 / W2 7c79f8c6 / W3 this wave). The AMC-001 lineage stays as historical evidence; the AMSC-001 records are authoritative for the current Identity module.",
        "amsc001EvidenceRoot": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0..W3/",
        "amsc001StopGate": "USER_REVIEW_IDENTITY_AMSC_001_W3",
        "folderGranularity": "PROFESSIONAL_SHALLOW",
        "solutionExplorer": "CANONICAL_MODULES_IDENTITY",`,
    'identityAmc001 structureState');

replaceOnce(
    `        "validatorCoverage": "COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED",
        "endpointReachableRequests": 13,
        "validatorRequiredCount": 6,
        "noValidatorRequiredCount": 7,`,
    `        "validatorCoverage": "COMPLETE_9_OF_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED",
        "endpointReachableRequests": 13,
        "validatorRequiredCount": 9,
        "noValidatorRequiredCount": 4,`,
    'identityAmc001 validator coverage');

// 2. W0 record: reconcile the commit placeholder to the real W0 commit.
replaceOnce(
    `        "commit": "PENDING_W0_COMMIT",
        "evidence": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0/analyze.md"`,
    `        "commit": "91eec1fd",
        "waveOutcome": "MIGRATE_UNBLOCKED",
        "evidence": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0/analyze.md"`,
    'identityModuleAmsc001W0 commit');

// 3. Append the W1 / W2 / W3 records after the W0 record.
const w1w2w3 = `    "identityModuleAmsc001W1": {
        "task": "TB-TMAR-IDENTITY-AMSC-001-W1",
        "mode": "ARCHITECT_DIRECT_AMSC",
        "skill": "tooba-architecture-migrate",
        "target": "src/backend/Modules/Identity/Tooba.Identity.*",
        "startingHead": "91eec1fd",
        "parentTask": "TB-TMAR-IDENTITY-AMSC-001-W0",
        "state": "MIGRATE_COMPLETE",
        "verdict": "READY_FOR_STRUCTURE",
        "stableCodeOwner": "Tooba.Identity.Contracts/Errors/IdentityErrorCodes.cs",
        "declaredCodeCount": 12,
        "registeredDescriptorCount": 12,
        "previouslyUnregisteredCodes": "identity.otp.delivery.rate_limited (429 Platform), identity.otp.delivery.invalid_destination (400 Business), identity.otp.delivery.unconfigured (400 Business)",
        "typedFaultMechanismState": "CODE_CARRYING_CONTRACTOPERATIONEXCEPTION",
        "faultCompositionSeam": "Tooba.Identity.Application/Composition/IdentityOperation.cs",
        "domainFaultCodeMechanism": "CONTRACTOPERATIONEXCEPTION_WITH_STABLE_CODE",
        "domainContractsReference": "PRESERVED_OWN_MODULE_CONTRACTS_REFERENCE_FOR_IdentityErrorCodes",
        "legacyRawExceptionThrowState": "ZERO",
        "parallelClassificationPathState": "ZERO",
        "hardcodedFaultTextState": "ZERO",
        "errorResourcesState": "BOTH_CULTURE_RESX_PRESENT_IdentityErrors.resx_PLUS_fa",
        "duplicateUsingState": "ZERO",
        "behaviorChange": "NONE",
        "routesChanged": "NONE",
        "statusCodesChanged": "NONE",
        "errorCodesChanged": "NONE",
        "dtoShapeChanged": "NONE",
        "schemaChange": "NONE",
        "guardsWeakened": "NONE",
        "focusedValidation": "Identity/Auth/Otp filter: 62 passed / 6 skipped / 0 failed",
        "commit": "93a6b192",
        "evidence": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W1/migrate.md"
    },
    "identityModuleAmsc001W2": {
        "task": "TB-TMAR-IDENTITY-AMSC-001-W2",
        "mode": "ARCHITECT_DIRECT_AMSC",
        "skill": "tooba-architecture-structure",
        "target": "src/backend/Modules/Identity/Tooba.Identity.*",
        "startingHead": "93a6b192",
        "parentTask": "TB-TMAR-IDENTITY-AMSC-001-W1",
        "state": "STRUCTURE_READY_FOR_CERTIFY",
        "verdict": "READY_FOR_CERTIFY",
        "structureState": "READY_FOR_CERTIFY",
        "folderGranularityState": "PROFESSIONAL_SHALLOW",
        "solutionExplorerState": "CANONICAL",
        "solutionFolder": "/Modules/Identity/",
        "solutionProjectEntries": 5,
        "pathNamespaceState": "EXACT",
        "physicalCopyState": "CLEAN",
        "rootAllowlistState": "ENFORCED",
        "fileCohesionState": "COHESIVE",
        "technicalAxisFirstState": "ZERO",
        "singleFileRequestLeafState": "ZERO",
        "rootDumpState": "ZERO",
        "contractsErrorsFolderState": "CANONICAL",
        "contractsProblemsFolderState": "ABSENT",
        "contractsLayout": "Auth/ + Actors/ + Contacts/ + Errors/{IdentityErrorCodes,IdentityErrorCatalogContributor,IdentityErrorResourceSet,IdentityDuplicateIdentifierFault} + Resources/{IdentityErrors.resx,IdentityErrors.fa.resx}",
        "applicationLayout": "Auth/{Commands,Queries,Models,Validators} + Composition/IdentityOperation.cs + shared Models/Options/Ports; no technical-axis root Validators/",
        "deadTransportResidueRemoved": "RegisterResponse, SessionResponse, AcceptedResponse, MeResponse (zero references)",
        "validatorCoverageState": "EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED",
        "validatorRequiredCount": 9,
        "noValidatorRequiredCount": 4,
        "shapeOnlyValidatorsAdded": "LoginWithPasswordCommandValidator, RefreshAuthSessionCommandValidator, CompleteOtpLoginCommandValidator",
        "enumerationSafetyNote": "LoginWithPasswordCommandValidator deliberately omits IdentifierKind so an unknown kind still reaches the handler and collapses to identity.authentication.failed (401) instead of identity.validation.failed (400).",
        "hostFinalClosureState": "PRESERVED",
        "manifestState": "CERTIFIED_MODULE_ENTRY_ALREADY_PRESENT_PROJECT_NOTE_RECONCILED_IN_W3",
        "durableGuards": ["IdentityModuleAmsc001W2StructureGuardTests"],
        "guardsRepaired": ["IdentityModuleAmcW1SolutionGuardTests.Identity_projects_group_under_Modules_Identity_in_slnx (flat /Modules/ folder assertion replaced by a strictly stronger nested-folder + no-leak assertion)", "HostCustomerProfileEvacuationGuardTests.Parent_R1_solution_grouping_remains_exact (same class of repair)"],
        "guardsWeakened": "NONE",
        "focusedValidation": "Identity + HostCustomerProfileEvacuationGuardTests + HostAdminCanon007/HostAdminCanonicalCertificationGuardTests + AuthenticationV2CanonicalizationGuardTests: 93 passed / 6 skipped / 0 failed; build 0 errors and 92 warnings (down from 127)",
        "structureHandoffState": "READY_FOR_CERTIFY",
        "commit": "7c79f8c6",
        "evidence": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W2/structure.md"
    },
    "identityModuleAmsc001W3": {
        "task": "TB-TMAR-IDENTITY-AMSC-001-W3",
        "mode": "ARCHITECT_DIRECT_AMSC",
        "skill": "tooba-architecture-certify",
        "target": "src/backend/Modules/Identity/Tooba.Identity.*",
        "startingHead": "7c79f8c6",
        "parentTask": "TB-TMAR-IDENTITY-AMSC-001-W2",
        "state": "IDENTITY_AMSC_001_CERTIFIED",
        "verdict": "COMPLETE_REFERENCE_PATTERN",
        "lockVersion": "ARCH-COMPLETE-002",
        "structureCertified": true,
        "structureState": "CERTIFIED",
        "structureGateSource": "TB-TMAR-IDENTITY-AMSC-001-W2",
        "folderGranularityState": "PROFESSIONAL_SHALLOW",
        "solutionExplorerState": "CANONICAL",
        "solutionFolder": "/Modules/Identity/",
        "solutionProjectEntries": 5,
        "pathNamespaceState": "EXACT",
        "physicalCopyState": "CLEAN",
        "rootAllowlistState": "ENFORCED",
        "fileCohesionState": "COHESIVE",
        "technicalAxisFirstState": "ZERO",
        "singleFileRequestLeafState": "ZERO",
        "rootDumpState": "ZERO",
        "aliasWorkaround": "ZERO",
        "typeForwardedToWorkaround": "ZERO",
        "namespaceAliasWorkaround": "ZERO",
        "endpointOwnershipState": "MODULE_OWNED",
        "moduleOwnedRoutes": 13,
        "hostHttpOwnershipState": "ZERO",
        "hostBusinessAuthorityState": "NONE",
        "hostPersistenceAuthorityState": "NONE",
        "hostFinalClosureState": "PRESERVED",
        "hostAuthPlatformRetained": "MIDDLEWARE_SESSION_THROTTLE",
        "cqrsState": "MEDIATR_12_5",
        "mediatrVersion": "12.5.0",
        "endpointReachableRequests": 13,
        "requestHandlerPairs": 13,
        "validatorCoverageState": "EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED",
        "validatorCoverageDetail": "9 VALIDATOR_REQUIRED requests resolve to their exact concrete validators via AddToobaCqrsFoundation; 4 NO_VALIDATOR_REQUIRED requests (LogoutSessionCommand, LogoutAllSessionsCommand, RequestPasswordResetCommand, GetAuthMeQuery) carry no transport body and register none. LoginWithPasswordCommandValidator stays shape-only and deliberately omits IdentifierKind to preserve the enumeration-safe 401 collapse.",
        "apiResultPatternState": "CANONICAL",
        "rawResultsState": "ONLY_INTENTIONAL_201_REGISTER_RAW_DTO",
        "messageTextClassificationState": "ZERO",
        "localizationState": "BOTH_CULTURE_RESX_PRESENT",
        "hardcodedTextState": "ZERO_CLIENT_FACING",
        "errorCodeDescriptorOwnership": "Tooba.Identity.Contracts/Errors/IdentityErrorCatalogContributor.cs",
        "declaredCodeCount": 12,
        "registeredDescriptorCount": 12,
        "unregisteredDeclaredCode": "NONE",
        "duplicateErrorDescriptorState": "ZERO",
        "typedFaultMechanismState": "CODE_CARRYING_CONTRACTOPERATIONEXCEPTION",
        "faultCompositionSeam": "Tooba.Identity.Application/Composition/IdentityOperation.cs",
        "domainFaultCodeMechanism": "CONTRACTOPERATIONEXCEPTION_WITH_STABLE_CODE",
        "legacyMapperState": "ZERO",
        "loggingState": "CANONICAL",
        "sensitiveLoggingState": "NONE",
        "openTelemetryState": "CANONICAL",
        "correlationTraceState": "CANONICAL",
        "crossModuleBoundaryState": "CONTRACTS_ONLY",
        "crossModuleBoundaryDetail": "Tooba.Identity.Application -> Tooba.CustomerProfile.Contracts (ICustomerProfileDirectory) only; no foreign Application/Domain/Infrastructure project edge in any Identity project.",
        "crossModuleJoinState": "ZERO",
        "crossModulePersistenceState": "ZERO",
        "foreignAppInfraDomainCoupling": "ZERO",
        "persistenceOwnershipState": "CORRECT",
        "schemaMigrationState": "UNCHANGED",
        "migrationFilesChanged": 0,
        "behaviorChange": "NONE",
        "statusCodesChanged": "NONE",
        "routesChanged": "NONE",
        "errorCodesChanged": "NONE",
        "dtoShapeChanged": "NONE",
        "productionProjects": 5,
        "manifestEntryCount": 1,
        "manifestDiskReconciliation": "PASS",
        "microserviceExtractable": true,
        "durableGuards": ["IdentityModuleAmsc001W3CertGuardTests", "IdentityModuleAmsc001W2StructureGuardTests", "IdentityModuleAmcW5CertGuardTests", "IdentityModuleAmcW2StructureGuardTests", "IdentityModuleAmcW1SolutionGuardTests", "IdentityValidatorCoverageGuardTests", "IdentityLifecycleTests", "IdentityFoundationTests", "AuthenticationHttpTests", "AuthSecurityHttpTests", "AuthenticationV2CanonicalizationGuardTests", "OtpDeliveryProviderTests", "StorefrontAccountIdentityTests", "CheckoutIdentityContractTests", "HostAdminAmcCheckoutIdentityGuardTests"],
        "guardsWeakened": "NONE",
        "baselinesWidened": "NONE",
        "blockingResidualDebt": "ZERO",
        "nonBlockingWatch": ["The English IdentityErrors.resx carries the 9 pre-existing catalogued keys; the three OTP-delivery codes added in W1 are registered with stable English descriptor titles and localized in IdentityErrors.fa.resx. Adding English resx entries would change the English title for those codes, so it is deliberately out of scope for a behavior-preserving certification.", "Infrastructure/Persistence/IdentityOutboxRegistration.cs throws InvalidOperationException(\\"Unmapped Identity integration event type.\\") exactly like the certified Offer/Catalog/Party/Pricing/Tax outbox registrations; it is a fail-closed composition guard on an unreachable branch, not a boundary fault, so it is out of scope."],
        "preExistingDriftProof": "The full Host suite has 82 pre-existing failures at the W3 starting HEAD 7c79f8c6, all unrelated to Identity (Host/Admin StoreAppearance count guards, Grid/Catalog/Party/Reviews module guards, Fulfillment/Tax/Pricing/Promotion domain tests, TmarDurableGuardTests Master-Recovery history pins, source-size baseline guards). Identity appears in none of them and none was touched.",
        "evidenceRoot": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3/",
        "workflowStop": "USER_REVIEW_IDENTITY_AMSC_001_W3",
        "automaticNextImplementationTask": "NONE"
    }
}
`;

replaceOnce(
    `        "evidence": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0/analyze.md"
    }
}
`,
    `        "evidence": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0/analyze.md"
    },
` + w1w2w3,
    'append W1/W2/W3 records');

fs.writeFileSync(file, text, 'utf8');

const parsed = JSON.parse(text);
if (parsed.identityModuleAmsc001W3.verdict !== 'COMPLETE_REFERENCE_PATTERN') {
    throw new Error('W3 record missing after patch');
}

console.log('SoT patched: identityModuleAmsc001W0/W1/W2/W3 + identityAmc001 reconciliation (text-anchored).');
