// TB-TMAR-IDENTITY-AMSC-001-W3-R1 — bounded Recovery/SoT-only reconciliation of
// docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md.
//
// 1. record the explicit final W3 SHA (e6d46774) in the Identity AMSC checkpoint;
// 2. add the W3-R1 module-local recovery reconciliation block.
//
// Zero production change. Global Host root checkpoint is not touched.

const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'TOOBA-TMAR-MASTER-RECOVERY.md');
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

// --- 1. explicit final W3 SHA --------------------------------------------
content = replaceOnce(
    content,
    '`TB-TMAR-IDENTITY-AMSC-001-W3` Certify `this wave`.',
    '`TB-TMAR-IDENTITY-AMSC-001-W3` Certify `e6d46774` (`e6d467740dc0305c665d72712fbc5f115ba4b4bf`).',
    'identity W3 sha');

// --- 2. W3-R1 reconciliation block ---------------------------------------
const anchor = '- Stop gate: `USER_REVIEW_IDENTITY_AMSC_001_W3`.' + NL;

const block = [
    '',
    'Identity AMSC W3-R1 recovery reconciliation (module-local)',
    '',
    'Recorded by `TB-TMAR-IDENTITY-AMSC-001-W3-R1` (recovery/SoT/evidence reconciliation only; zero production change). W3 certification is preserved unchanged.',
    '- Historical-truth correction: `identityAmc001` (`TB-TMAR-IDENTITY-AMC-001`, implementation `aafd14e0` / docs stamp `c7e473cd`) had been rewritten by W3 into current AMSC truth. It is restored to its truthful pre-W3 values: `structureState = READY_FOR_CERTIFY`, `validatorCoverage = COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED`, `validatorRequiredCount = 6`, `noValidatorRequiredCount = 7`; the W3-appended AMSC fields (`amsc001Certified`, `amsc001CertificationNote`, `amsc001EvidenceRoot`, `amsc001StopGate`) are removed. The historical AMC record is preserved, not erased.',
    '- Current Identity authority preserved unchanged: `identityModuleAmsc001W0..W3` remains the authoritative current lineage (`state = IDENTITY_AMSC_001_CERTIFIED`, `structureState = CERTIFIED`, `validatorCoverageState = EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED`, 13 endpoint-reachable requests, `microserviceExtractable = true`).',
    '- Historical Identity lineage is explicitly marked `HISTORICAL / SUPERSEDED FOR CURRENT IDENTITY MODULE RECOVERY` — AMC-001 remains truthful historical evidence; the AMSC-001 W0→W3 lineage is authoritative for the current Identity module certification, while the repository-global Host root checkpoint is NOT superseded or displaced.',
    '- W3 final commit SHA recorded explicitly: `e6d46774` (`e6d467740dc0305c665d72712fbc5f115ba4b4bf`). Accepted lineage preserved exactly: W0 `91eec1fd` → W1 `93a6b192` → W2 `7c79f8c6` → W3 `e6d46774`.',
    '- Certified commit: `e6d467740dc0305c665d72712fbc5f115ba4b4bf`; `automaticNextImplementationTask = NONE`.',
    '- Evidence root: `docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/`.',
    '- Stop gate: `USER_REVIEW_IDENTITY_AMSC_001_W3_R1`.',
    '',
].join(NL);

content = replaceOnce(content, anchor, anchor + block, 'identity stop gate anchor');

if (content === original) {
    throw new Error('no change produced');
}

// --- 3. validate ---------------------------------------------------------
if (!content.includes('`TB-TMAR-IDENTITY-AMSC-001-W3` Certify `e6d46774`')) {
    throw new Error('W3 SHA not recorded');
}
if (!content.includes('USER_REVIEW_IDENTITY_AMSC_001_W3_R1')) {
    throw new Error('R1 stop gate not recorded');
}
if (!content.includes('HISTORICAL / SUPERSEDED FOR CURRENT IDENTITY MODULE RECOVERY')) {
    throw new Error('historical marker not recorded');
}
if (!content.includes('USER_REVIEW_HOST_ROOT_FINAL_CERT_001')) {
    throw new Error('global Host root checkpoint stop gate lost');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md');
