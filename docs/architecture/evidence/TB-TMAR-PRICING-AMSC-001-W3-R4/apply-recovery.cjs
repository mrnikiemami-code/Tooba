// TB-TMAR-PRICING-AMSC-001-W3-R4 — final recovery lineage reconciliation (SoT only).
// Additive, in-place: record each historical wave's own commit SHA, classify the evidence-only hop,
// and append the additive pricingAmsc001W3R4 closure block. No field is deleted or rewritten except
// the W3-R3 certificationCommitState replacement mandated by the task.
const fs = require('fs');

const SOT = 'docs/architecture/tmar-current-state.json';
const raw = fs.readFileSync(SOT, 'utf8');
const bom = raw.charCodeAt(0) === 0xfeff;
const json = JSON.parse(bom ? raw.slice(1) : raw);

const SHAS = {
    w0: '08d47b6a4af3e4e2102712b93f27185ac15a23ac',
    w1: '069f77d2fa5b2c2cec3bb078147c3559a571bd64',
    w2: 'f7f6abfec455b771952852e8627df4c57b698caf',
    supersededW3: '3c2cc61e7c61813ac72773ccdb8bb16317cafe70',
    w3R1: '2e664bb336f45b8304818b7e5754f9b0fc364f20',
    w3R2: '7159c8f773c1faa9b4b6d425b19067f50ca27572',
    w3R3: 'a1ca9b5afab181da74fe6db0efbb38f43a3a9721',
    w3R3Evidence: '127c596aba7abefe6bdd0a1edb7c69c064267762',
    w3R3Evidence2: '549f1ac535e37e4e8c9e3c44b88768c575dfeb5e',
};

const before = JSON.parse(JSON.stringify({
    w0: json.pricingAmsc001W0,
    w1: json.pricingAmsc001W1,
    w2: json.pricingAmsc001W2,
    r1: json.pricingAmsc001W3R1,
    r2: json.pricingAmsc001W3R2,
    r3: json.pricingAmsc001W3R3,
}));

// 1) W0 — replace the self-referential placeholders with the real wave commit.
json.pricingAmsc001W0.commit = SHAS.w0;
json.pricingAmsc001W0.commitFull = SHAS.w0;
json.pricingAmsc001W0.selfCommitPlaceholderState = 'RESOLVED_BY_TB-TMAR-PRICING-AMSC-001-W3-R4';

// 2) W1 / W2 — record their own final commit alongside the preserved parentCommit.
json.pricingAmsc001W1.commit = SHAS.w1;
json.pricingAmsc001W1.commitFull = SHAS.w1;
json.pricingAmsc001W2.commit = SHAS.w2;
json.pricingAmsc001W2.commitFull = SHAS.w2;

// 3) Historical W3-R1 — record its own final commit, preserving certifiedCommit.
json.pricingAmsc001W3R1.commit = SHAS.w3R1;
json.pricingAmsc001W3R1.commitFull = SHAS.w3R1;

// 4) W3-R2 — record its own final commit (structure authority SHA).
json.pricingAmsc001W3R2.commit = SHAS.w3R2;
json.pricingAmsc001W3R2.commitFull = SHAS.w3R2;

// 5) W3-R3 — record the certification commit and classify the evidence-only hop. Only the previous
//    bridge-only certificationCommitState is replaced; architecture certification facts are untouched.
json.pricingAmsc001W3R3.commit = SHAS.w3R3;
json.pricingAmsc001W3R3.commitFull = SHAS.w3R3;
json.pricingAmsc001W3R3.certificationCommitState = 'RECORDED_A1CA9B5A';
json.pricingAmsc001W3R3.postCertificationEvidenceCommit = SHAS.w3R3Evidence;
json.pricingAmsc001W3R3.postCertificationEvidenceCommitState =
    'EVIDENCE_ONLY_NOT_CERTIFICATION_AUTHORITY';
json.pricingAmsc001W3R3.postCertificationEvidenceDetail =
    'a1ca9b5a -> 127c596a (recovery-debt.md only) -> 549f1ac5 (RESULT.bridge.txt + post-result.js only). '
    + 'Both hops are evidence-only documentation commits; neither is certification authority and neither '
    + 'alters architecture certification facts. Classified by TB-TMAR-PRICING-AMSC-001-W3-R4.';
json.pricingAmsc001W3R3.postResultEvidenceCommit = SHAS.w3R3Evidence2;
json.pricingAmsc001W3R3.postResultEvidenceCommitState = 'EVIDENCE_ONLY_NOT_CERTIFICATION_AUTHORITY';
json.pricingAmsc001W3R3.postCertRecoveryState = 'POST_CERT_RECOVERY_RECONCILIATION_REQUIRED_CLOSED_BY_W3_R4';
json.pricingAmsc001W3R3.postCertRecoveryDetail =
    'RECOVERY_DEBT_CLOSED_BY_TB-TMAR-PRICING-AMSC-001-W3-R4: the pricingAmsc001W0 commit/commitFull '
    + 'placeholders are resolved, pricingAmsc001W1/W2 and the historical pricingAmsc001W3R1/W3R2 now carry '
    + 'their own final commit/commitFull, and the W3-R3 certification commit plus the evidence-only hop are '
    + 'recorded. Certification semantics, manifest truth and schema/migration state are unchanged.';

// 6) Additive W3-R4 closure block (no self-referential R4 commit placeholder).
json.pricingAmsc001W3R4 = {
    task: 'TB-TMAR-PRICING-AMSC-001-W3-R4',
    parentTask: 'TB-TMAR-PRICING-AMSC-001-W3-R3',
    mode: 'FINAL_RECOVERY_LINEAGE_RECONCILIATION',
    skill: 'tooba-architecture-certify',
    target: 'src/backend/Modules/Pricing/Tooba.Pricing.*',
    startingHead: '127c596a',
    startingHeadFull: SHAS.w3R3Evidence,
    commitState: 'REPORTED_IN_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_SOT_SHA',
    state: 'PRICING_AMSC_001_RECOVERY_FINAL_CLOSED',
    productionCodeChanged: false,
    currentCertificationAuthority: 'TB-TMAR-PRICING-AMSC-001-W3-R3',
    currentCertifiedCommit: SHAS.w3R3,
    currentStructureAuthority: 'TB-TMAR-PRICING-AMSC-001-W3-R2',
    currentStructureCommit: SHAS.w3R2,
    w0Commit: SHAS.w0,
    w1Commit: SHAS.w1,
    w2Commit: SHAS.w2,
    supersededW3Commit: SHAS.supersededW3,
    historicalW3R1Commit: SHAS.w3R1,
    w3R2StructureCommit: SHAS.w3R2,
    w3R3CertificationCommit: SHAS.w3R3,
    w3R3EvidenceOnlyCommit: SHAS.w3R3Evidence,
    w3R3PostResultEvidenceCommit: SHAS.w3R3Evidence2,
    evidenceOnlyCommitClassification: 'EVIDENCE_ONLY_NOT_AUTHORITY',
    actualParentChainState: 'RECONCILED',
    actualParentChain: '08d47b6a <- a1d9ecfb; 069f77d2 <- 08d47b6a; f7f6abfe <- 069f77d2; '
        + '3c2cc61e <- f7f6abfe; 2e664bb3 <- 3c2cc61e; 7159c8f7 <- 2e664bb3; a1ca9b5a <- 7159c8f7; '
        + '127c596a <- a1ca9b5a; 549f1ac5 <- 127c596a',
    certificationAuthorityState: 'W3_R3_A1CA9B5A',
    structureAuthorityState: 'W3_R2_7159C8F7',
    httpApplicability: 'NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER',
    endpointProjectState: 'ABSENT',
    structureState: 'CERTIFIED',
    manifestStructuralState: 'NOT_TOUCHED_THIS_WAVE',
    schemaMigrationState: 'UNCHANGED',
    globalHostCheckpointState: 'PRESERVED',
    guardsWeakened: 'NONE',
    baselinesWidened: 'NONE',
    recoveryDebtState: 'CLOSED',
    workflowStop: 'USER_REVIEW_PRICING_AMSC_001_W3_R4',
    automaticNextImplementationTask: 'NONE',
    evidenceRoot: 'docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R4/',
    evidence: 'docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R4/recovery-reconciliation.md',
};

const out = JSON.stringify(json, null, 2) + '\n';
fs.writeFileSync(SOT, (bom ? '\uFEFF' : '') + out, 'utf8');

const after = JSON.parse(fs.readFileSync(SOT, 'utf8').replace(/^\uFEFF/, ''));

// Verification: every requested SHA is present, no placeholder survives, no unrelated field changed.
const expect = [
    [after.pricingAmsc001W0.commit, SHAS.w0], [after.pricingAmsc001W0.commitFull, SHAS.w0],
    [after.pricingAmsc001W1.commit, SHAS.w1], [after.pricingAmsc001W1.commitFull, SHAS.w1],
    [after.pricingAmsc001W2.commit, SHAS.w2], [after.pricingAmsc001W2.commitFull, SHAS.w2],
    [after.pricingAmsc001W3R1.commit, SHAS.w3R1], [after.pricingAmsc001W3R1.commitFull, SHAS.w3R1],
    [after.pricingAmsc001W3R2.commit, SHAS.w3R2], [after.pricingAmsc001W3R2.commitFull, SHAS.w3R2],
    [after.pricingAmsc001W3R3.commit, SHAS.w3R3], [after.pricingAmsc001W3R3.commitFull, SHAS.w3R3],
];
for (const [actual, wanted] of expect) {
    if (actual !== wanted) throw new Error(`SHA mismatch: ${actual} != ${wanted}`);
}
// Only the wave-own commit/commitFull fields must be placeholder-free. The historical
// pricingAmsc001W3R1.masterRecoveryW3ShaBefore field deliberately stays "PENDING_THIS_COMMIT"
// because it records the pre-reconciliation state and is pinned by the W3-R2 repair guard.
for (const block of ['pricingAmsc001W0', 'pricingAmsc001W1', 'pricingAmsc001W2',
                     'pricingAmsc001W3', 'pricingAmsc001W3R1', 'pricingAmsc001W3R2',
                     'pricingAmsc001W3R3']) {
    for (const field of ['commit', 'commitFull']) {
        if (after[block][field] === 'PENDING_THIS_COMMIT') {
            throw new Error(`a self-referential placeholder survived: ${block}.${field}`);
        }
    }
}
if (after.pricingAmsc001W3R1.masterRecoveryW3ShaBefore !== 'PENDING_THIS_COMMIT') {
    throw new Error('the preserved W3-R1 pre-reconciliation marker was altered');
}
if (after.pricingAmsc001W3R3.certificationCommitState !== 'RECORDED_A1CA9B5A') {
    throw new Error('W3-R3 certificationCommitState not replaced');
}
if (after.pricingAmsc001W3R4.commit !== undefined) {
    throw new Error('R4 must not carry a self-referential commit field');
}
// Preserved-fact spot checks against the pre-edit snapshot.
if (after.pricingAmsc001W0.state !== before.w0.state
    || after.pricingAmsc001W0.verdict !== before.w0.verdict
    || after.pricingAmsc001W0.startingHead !== before.w0.startingHead) {
    throw new Error('W0 historical fields were altered');
}
if (after.pricingAmsc001W1.parentCommit !== before.w1.parentCommit
    || after.pricingAmsc001W1.state !== before.w1.state) {
    throw new Error('W1 historical fields were altered');
}
if (after.pricingAmsc001W2.parentCommit !== before.w2.parentCommit
    || after.pricingAmsc001W2.state !== before.w2.state) {
    throw new Error('W2 historical fields were altered');
}
if (after.pricingAmsc001W3R1.certifiedCommit !== before.r1.certifiedCommit
    || after.pricingAmsc001W3R1.masterRecoveryW3ShaBefore !== before.r1.masterRecoveryW3ShaBefore
    || after.pricingAmsc001W3R1.masterRecoveryW3ShaState !== before.r1.masterRecoveryW3ShaState) {
    throw new Error('W3-R1 lineage fields were altered');
}
if (after.pricingAmsc001W3R2.supersededCertificationCommit !== before.r2.supersededCertificationCommit
    || after.pricingAmsc001W3R2.state !== before.r2.state) {
    throw new Error('W3-R2 historical fields were altered');
}
if (after.pricingAmsc001W3R3.verdict !== before.r3.verdict
    || after.pricingAmsc001W3R3.structureCertified !== before.r3.structureCertified
    || after.pricingAmsc001W3R3.declaredCodeCount !== before.r3.declaredCodeCount
    || after.pricingAmsc001W3R3.currentStructureCommit !== before.r3.currentStructureCommit) {
    throw new Error('W3-R3 architecture certification facts were altered');
}
// Global recovery lock untouched.
for (const key of ['lastAcceptedTask', 'lastAcceptedCommit', 'latestAcceptedImplementationWave',
                   'currentHostCheckpoint', 'nextHostFolder', 'workflowStop',
                   'automaticNextImplementationTask']) {
    if (after[key] !== json[key]) {
        throw new Error(`global recovery lock field changed: ${key}`);
    }
}
if (JSON.stringify(after.structureLock.certifiedModules) !== JSON.stringify(json.structureLock.certifiedModules)) {
    throw new Error('structureLock.certifiedModules changed');
}

console.log('SOT_OK');
console.log('W0', after.pricingAmsc001W0.commitFull);
console.log('W1', after.pricingAmsc001W1.commitFull);
console.log('W2', after.pricingAmsc001W2.commitFull);
console.log('W3R1', after.pricingAmsc001W3R1.commitFull);
console.log('W3R2', after.pricingAmsc001W3R2.commitFull);
console.log('W3R3', after.pricingAmsc001W3R3.commitFull);
console.log('W3R3CertificationCommitState', after.pricingAmsc001W3R3.certificationCommitState);
console.log('W3R4State', after.pricingAmsc001W3R4.state);
console.log('TOP_LEVEL_KEYS', Object.keys(after).length);
