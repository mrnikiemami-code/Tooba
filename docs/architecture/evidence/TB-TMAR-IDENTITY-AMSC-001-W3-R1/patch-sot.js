// TB-TMAR-IDENTITY-AMSC-001-W3-R1 — bounded Recovery/SoT-only reconciliation of
// docs/architecture/tmar-current-state.json.
//
// 1. restore the historical identityAmc001 (AMC-001) record to its truthful pre-W3 values;
// 2. add the additive identityModuleAmsc001W3R1 reconciliation record.
//
// Zero production change. Global Host root checkpoint is not touched.

const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
let content = original;

function replaceOnce(haystack, needle, replacement, label) {
    const first = haystack.indexOf(needle);
    if (first < 0) {
        throw new Error(`${label}: anchor not found`);
    }
    if (haystack.indexOf(needle, first + 1) >= 0) {
        throw new Error(`${label}: anchor is not unique`);
    }
    return haystack.slice(0, first) + replacement + haystack.slice(first + needle.length);
}

const NL = '\r\n';

// --- 1. restore historical AMC truth -------------------------------------
const amcBlockOld = [
    '        "structureState": "CERTIFIED",',
    '        "amsc001Certified": true,',
    '        "amsc001CertificationNote": "Re-certified under the AMSC-001 four-wave pipeline (W0 91eec1fd / W1 93a6b192 / W2 7c79f8c6 / W3 this wave). The AMC-001 lineage stays as historical evidence; the AMSC-001 records are authoritative for the current Identity module.",',
    '        "amsc001EvidenceRoot": "docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0..W3/",',
    '        "amsc001StopGate": "USER_REVIEW_IDENTITY_AMSC_001_W3",',
].join(NL);

const amcBlockNew = '        "structureState": "READY_FOR_CERTIFY",';

content = replaceOnce(content, amcBlockOld, amcBlockNew, 'identityAmc001 amsc fields');

const amcValidatorOld = [
    '        "validatorCoverage": "COMPLETE_9_OF_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED",',
    '        "endpointReachableRequests": 13,',
    '        "validatorRequiredCount": 9,',
    '        "noValidatorRequiredCount": 4,',
    '        "validationDiscovery": "ADD_VALIDATORS_FROM_ASSEMBLY_VIA_ADD_TOOBA_CQRS_FOUNDATION",',
].join(NL);

const amcValidatorNew = [
    '        "validatorCoverage": "COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED",',
    '        "endpointReachableRequests": 13,',
    '        "validatorRequiredCount": 6,',
    '        "noValidatorRequiredCount": 7,',
    '        "validationDiscovery": "ADD_VALIDATORS_FROM_ASSEMBLY_VIA_ADD_TOOBA_CQRS_FOUNDATION",',
].join(NL);

content = replaceOnce(content, amcValidatorOld, amcValidatorNew, 'identityAmc001 validator classification');

// --- 2. additive W3-R1 reconciliation record ------------------------------
const r1 = {
    task: 'TB-TMAR-IDENTITY-AMSC-001-W3-R1',
    parentTask: 'TB-TMAR-IDENTITY-AMSC-001-W3',
    mode: 'RECOVERY_SOT_RECONCILIATION_ONLY',
    target: 'docs/architecture (documentation/SoT/guard truth only)',
    startingHead: 'e6d46774',
    state: 'IDENTITY_AMSC_001_RECOVERY_RECONCILED',
    verdict: 'COMPLETE_REFERENCE_PATTERN',
    lockVersion: 'ARCH-COMPLETE-002',
    structureCertified: true,
    productionCodeChanged: false,
    productionScopeState: 'RECOVERY_SOT_EVIDENCE_ONLY',
    certifiedCommit: 'e6d467740dc0305c665d72712fbc5f115ba4b4bf',
    acceptedLineage: 'W0 91eec1fd -> W1 93a6b192 -> W2 7c79f8c6 -> W3 e6d46774',
    historicalAmcState: 'RESTORED_PRE_W3_TRUTH',
    historicalAmcRestoredFields: [
        'identityAmc001.structureState = READY_FOR_CERTIFY',
        'identityAmc001.validatorCoverage = COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED',
        'identityAmc001.validatorRequiredCount = 6',
        'identityAmc001.noValidatorRequiredCount = 7'
    ],
    historicalAmcRemovedW3AddedFields: [
        'amsc001Certified',
        'amsc001CertificationNote',
        'amsc001EvidenceRoot',
        'amsc001StopGate'
    ],
    historicalAmcTaskIdentityPreserved: 'TB-TMAR-IDENTITY-AMC-001',
    historicalAmcImplementationCommit: 'aafd14e0be51bdf3eae2ec2c6c1a09f92c613992',
    historicalAmcDocsStampCommit: 'c7e473cd13d800de9cf3c429d884a10bb68819a1',
    historicalLineageState: 'PRESERVED_AND_SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY',
    historicalLineageKeys: 'identityAmc001',
    currentAuthority: 'IDENTITY_AMSC_001_W0_TO_W3',
    currentAuthorityState: 'PRESERVED_UNCHANGED',
    masterRecoveryW3ShaBefore: 'MISSING',
    masterRecoveryW3ShaState: 'RECORDED_E6D46774',
    masterRecoveryState: 'RECONCILED',
    globalHostCheckpointState: 'PRESERVED',
    manifestStructuralState: 'NOT_TOUCHED',
    schemaMigrationState: 'UNCHANGED',
    frontendState: 'FROZEN_UNCHANGED',
    crossModuleBoundaryState: 'CONTRACTS_ONLY',
    foreignAppInfraDomainCoupling: 'ZERO',
    focusedAmcGuardState: 'PASS',
    focusedAmscGuardState: 'PASS',
    jsonParseState: 'PASS',
    guardsWeakened: 'NONE',
    baselinesWidened: 'NONE',
    evidenceRoot: 'docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/',
    workflowStop: 'USER_REVIEW_IDENTITY_AMSC_001_W3_R1',
    automaticNextImplementationTask: 'NONE'
};

const r1Text = JSON.stringify(r1, null, 4)
    .split('\n')
    .map((line, index) => (index === 0 ? `    "identityModuleAmsc001W3R1": ${line}` : `    ${line}`))
    .join(NL);

const terminator = `    }${NL}}${NL}`;
const lastIndex = content.lastIndexOf(terminator);
if (lastIndex < 0) {
    throw new Error('record terminator not found');
}
content = content.slice(0, lastIndex)
    + `    },${NL}`
    + r1Text + NL
    + `}${NL}`;

// --- 3. validate ---------------------------------------------------------
const parsed = JSON.parse(content);
const amc = parsed.identityAmc001;
const expected = {
    structureState: 'READY_FOR_CERTIFY',
    validatorCoverage: 'COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED',
    validatorRequiredCount: 6,
    noValidatorRequiredCount: 7,
    task: 'TB-TMAR-IDENTITY-AMC-001',
    implementationCommit: 'aafd14e0be51bdf3eae2ec2c6c1a09f92c613992',
    docsStampCommit: 'c7e473cd13d800de9cf3c429d884a10bb68819a1'
};
for (const [key, value] of Object.entries(expected)) {
    if (amc[key] !== value) {
        throw new Error(`identityAmc001.${key} = ${JSON.stringify(amc[key])}, expected ${JSON.stringify(value)}`);
    }
}
for (const forbidden of ['amsc001Certified', 'amsc001CertificationNote', 'amsc001EvidenceRoot', 'amsc001StopGate']) {
    if (Object.prototype.hasOwnProperty.call(amc, forbidden)) {
        throw new Error(`identityAmc001.${forbidden} must not exist`);
    }
}

const w3 = parsed.identityModuleAmsc001W3;
if (w3.state !== 'IDENTITY_AMSC_001_CERTIFIED' || w3.structureState !== 'CERTIFIED') {
    throw new Error('current AMSC W3 authority was disturbed');
}
if (w3.validatorCoverageState !== 'EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED'
    || w3.endpointReachableRequests !== 13) {
    throw new Error('current AMSC W3 validator classification was disturbed');
}
const w2 = parsed.identityModuleAmsc001W2;
if (w2.validatorCoverageState !== 'EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED'
    || w2.validatorRequiredCount !== 9 || w2.noValidatorRequiredCount !== 4) {
    throw new Error('current AMSC W2 validator classification was disturbed');
}
if (parsed.identityModuleAmsc001W3R1.state !== 'IDENTITY_AMSC_001_RECOVERY_RECONCILED') {
    throw new Error('R1 record missing');
}
for (const pointer of ['lastAcceptedTask', 'lastAcceptedCommit', 'latestAcceptedImplementationWave',
    'currentHostCheckpoint', 'workflowStop', 'automaticNextImplementationTask']) {
    if (!Object.prototype.hasOwnProperty.call(parsed, pointer)) {
        throw new Error(`global pointer ${pointer} missing`);
    }
}
if (parsed.lastAcceptedTask !== 'TB-TMAR-HOST-ROOT-FINAL-CERT-001'
    || parsed.currentHostCheckpoint !== 'HOST_ROOT_FINAL_CERTIFIED'
    || parsed.workflowStop !== 'USER_REVIEW_HOST_ROOT_FINAL_CERT_001'
    || parsed.automaticNextImplementationTask !== 'NONE') {
    throw new Error('repository-global Host root checkpoint was disturbed');
}

if (content === original) {
    throw new Error('no change produced');
}
fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json');
