// TB-TMAR-STORY-AMSC-001-W3-R1 — bounded recovery / SoT reconciliation only.
//   1) repairs two stale W3 self-description fields to the verified truth (guardsAdded 9->8 facts and
//      focusedValidation 9/9->8/8), because the W3 record was written before the guard class was finalised
//      at 8 [Fact]s;
//   2) stamps storyAmsc001W3.commit with the real W3 certification SHA (was PENDING_W3_COMMIT);
//   3) appends the additive storyAmsc001W3R1 reconciliation record.
// Zero production, manifest, schema or frontend change. The repository-global Host root checkpoint is
// preserved exactly and the historical AMC-001 lineage is left untouched.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const rawOriginal = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const W0_COMMIT = '0c73390a3211e0ee9057e9234d62d3e4f14b5e4e';
const W1_COMMIT = '2a09e7bb7f1ab687435007951160b4bcfefb4c18';
const W2_COMMIT = '4cd9a6cc543ccd307d775dfe703459de7b12c95d';
const W3_COMMIT = '39ab324e9c57e342516202bdd8df95953dda6439';

if (rawOriginal.includes('"storyAmsc001W3R1"')) {
  throw new Error('storyAmsc001W3R1 already present — refusing to overwrite');
}
if ((rawOriginal.match(/"PENDING_W3_COMMIT"/g) || []).length !== 1) {
  throw new Error('expected exactly one PENDING_W3_COMMIT marker');
}
if ((rawOriginal.match(/"StoryModuleAmsc001W3CertGuardTests \(9 facts\)"/g) || []).length !== 1) {
  throw new Error('expected exactly one stale "9 facts" guard claim');
}
if ((rawOriginal.match(/StoryModuleAmsc001W3CertGuardTests 9\/9/g) || []).length !== 1) {
  throw new Error('expected exactly one stale "9/9" focused-validation claim');
}

// --- 1) repair the two stale W3 self-description fields (truth only) ------
let original = rawOriginal
  .replace('"guardsAdded": "StoryModuleAmsc001W3CertGuardTests (9 facts)"',
    '"guardsAdded": "StoryModuleAmsc001W3CertGuardTests (8 facts)"')
  .replace('StoryModuleAmsc001W3CertGuardTests 9/9', 'StoryModuleAmsc001W3CertGuardTests 8/8');

// --- 2) stamp the real W3 certification SHA -------------------------------
original = original.replace('"commit": "PENDING_W3_COMMIT"', `"commit": "${W3_COMMIT}"`);

// --- 3) append the W3-R1 reconciliation record ----------------------------
const record = {
  task: 'TB-TMAR-STORY-AMSC-001-W3-R1',
  parentTask: 'TB-TMAR-STORY-AMSC-001-W3',
  mode: 'RECOVERY_SOT_RECONCILIATION_ONLY',
  skill: 'recovery-reconciliation',
  target: 'docs/architecture (documentation/SoT/evidence truth only)',
  startingHead: W3_COMMIT,
  state: 'STORY_AMSC_001_RECOVERY_RECONCILED',
  verdict: 'COMPLETE_REFERENCE_PATTERN',
  lockVersion: 'ARCH-COMPLETE-002',
  structureCertified: true,
  productionCodeChanged: false,
  productionScopeState: 'RECOVERY_SOT_EVIDENCE_ONLY',
  certifiedCommit: W3_COMMIT,
  certifiedCommitShort: '39ab324e',
  masterRecoveryW3ShaBefore: 'PLACEHOLDER_THIS_COMMIT',
  masterRecoveryW3ShaState: 'RECORDED_39AB324E',
  masterRecoveryState: 'RECONCILED',
  acceptedLineage: {
    w0: '0c73390a',
    w1: '2a09e7bb',
    w2: '4cd9a6cc',
    w3: '39ab324e',
  },
  acceptedLineageFull: {
    w0: W0_COMMIT,
    w1: W1_COMMIT,
    w2: W2_COMMIT,
    w3: W3_COMMIT,
  },
  w3SelfDescriptionRepair: 'guardsAdded 9 facts -> 8 facts and focusedValidation 9/9 -> 8/8, reconciled to the verified StoryModuleAmsc001W3CertGuardTests [Fact] count on disk.',
  gateLiteralRepair: 'TmarCompleteReferenceStructureGateTests.Uncertified_modules_are_explicitly_not_claimed was newly red because the W3 Story promotion made structureLock.certifiedModules 29 while the frozen literal held 28; the literal was extended with Story (Pricing W3-R3 precedent). Proven newly-caused (green at the untouched W2 HEAD) and repaired; no assertion removed and no guard weakened, restoring W2 baseline parity (10 failed / 21 passed on the declared-red family).',
  gateLiteralRepair: 'TmarCompleteReferenceStructureGateTests.Uncertified_modules_are_explicitly_not_claimed was newly red because the W3 Story promotion made structureLock.certifiedModules 29 while the frozen literal held 28; the literal was extended with Story (Pricing W3-R3 precedent). Proven newly-caused (green at the untouched W2 HEAD) and repaired; no assertion removed and no guard weakened, restoring W2 baseline parity (10 failed / 21 passed on the declared-red family).',
  priorAmc001CertificationState: 'HISTORICAL_SUPERSEDED_NOT_REWRITTEN',
  historicalLineageState: 'PRESERVED_HISTORICAL_NOT_REWRITTEN',
  historicalLineageKeys: 'storyModuleAmc001 .. storyModuleAmc001W6Cert (superseded for current Story authority by AMSC-001 W0->W3)',
  currentAuthority: 'STORY_AMSC_001_W0_TO_W3',
  manifestStructuralState: 'NOT_TOUCHED',
  schemaMigrationState: 'UNCHANGED',
  frontendState: 'FROZEN_UNTOUCHED',
  crossModuleBoundaryState: 'NONE_SELF_CONTAINED',
  foreignAppInfraDomainCoupling: 'ZERO',
  microserviceExtractable: true,
  globalHostCheckpointState: 'PRESERVED',
  guardsWeakened: 'NONE',
  guardsWeakenedCount: 0,
  baselinesWidened: 'NONE',
  jsonParseState: 'PASS',
  workflowStop: 'USER_REVIEW_STORY_AMSC_001_W3_R1',
  automaticNextImplementationTask: 'NONE',
  commitState: 'REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3-R1/reconciliation.md',
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storyAmsc001W3R1": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}

const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// --- validate ------------------------------------------------------------
const parsed = JSON.parse(content);

const w0 = parsed.storyAmsc001W0;
const w1 = parsed.storyAmsc001W1;
const w2 = parsed.storyAmsc001W2;
const w3 = parsed.storyAmsc001W3;
const r1 = parsed.storyAmsc001W3R1;

if (!w0 || w0.commit !== W0_COMMIT || w0.state !== 'ANALYZE_COMPLETE') {
  throw new Error('storyAmsc001W0 record disturbed');
}
if (!w1 || w1.commit !== W1_COMMIT || w1.startingHead !== W0_COMMIT) {
  throw new Error('storyAmsc001W1 record disturbed');
}
if (!w2 || w2.commit !== W2_COMMIT || w2.startingHead !== W1_COMMIT) {
  throw new Error('storyAmsc001W2 record disturbed');
}
if (!w3 || w3.commit !== W3_COMMIT || w3.startingHead !== W2_COMMIT
  || w3.state !== 'STORY_AMSC_001_CERTIFIED' || w3.verdict !== 'COMPLETE_REFERENCE_PATTERN'
  || w3.structureState !== 'CERTIFIED' || w3.structureCertified !== true
  || w3.lockVersion !== 'ARCH-COMPLETE-002' || w3.blockingResidualDebt !== 'ZERO'
  || w3.guardsAdded !== 'StoryModuleAmsc001W3CertGuardTests (8 facts)') {
  throw new Error('storyAmsc001W3 record missing or wrong state');
}
if (!r1 || r1.state !== 'STORY_AMSC_001_RECOVERY_RECONCILED'
  || r1.certifiedCommit !== W3_COMMIT || r1.startingHead !== W3_COMMIT
  || r1.structureCertified !== true || r1.productionCodeChanged !== false
  || r1.acceptedLineage.w3 !== '39ab324e') {
  throw new Error('storyAmsc001W3R1 record missing or wrong state');
}

const certified = parsed.structureLock.certifiedModules;
if (certified.length !== 29 || certified.filter((m) => m === 'Story').length !== 1) {
  throw new Error('structureLock.certifiedModules promotion disturbed');
}
if (parsed.completeReferenceModules.length !== 12) {
  throw new Error('completeReferenceModules length changed');
}

for (const key of ['storyModuleAmc001', 'storyModuleAmc001W1', 'storyModuleAmc001W2Cert',
  'storyModuleAmc001W3', 'storyModuleAmc001W4', 'storyModuleAmc001W5', 'storyModuleAmc001W6Cert']) {
  if (!Object.prototype.hasOwnProperty.call(parsed, key)) {
    throw new Error(`historical AMC record ${key} was disturbed`);
  }
}

if (parsed.lastAcceptedTask !== 'TB-TMAR-HOST-ROOT-FINAL-CERT-001'
  || parsed.currentHostCheckpoint !== 'HOST_ROOT_FINAL_CERTIFIED'
  || parsed.workflowStop !== 'USER_REVIEW_HOST_ROOT_FINAL_CERT_001'
  || parsed.automaticNextImplementationTask !== 'NONE') {
  throw new Error('repository-global Host root checkpoint was disturbed');
}

if ((content.match(/"PENDING_W3_COMMIT"/g) || []).length !== 0) {
  throw new Error('PENDING_W3_COMMIT marker not stamped');
}
if (content.includes('PENDING_W3R1_COMMIT')) {
  throw new Error('a self-referential PENDING_W3R1_COMMIT placeholder must not be written');
}
if (!content.includes('REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER')) {
  throw new Error('expected the R1 commitState marker');
}
if (!content.endsWith('}' + NL)) {
  throw new Error('unexpected trailing bytes');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (W3 SHA stamped + storyAmsc001W3R1 appended)');
