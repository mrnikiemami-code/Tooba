// TB-TMAR-STORY-AMSC-001-W3-R1 — reconcile the Master Recovery Story checkpoint only.
// Replaces the W3 self-reference "(this commit, reported in the Bridge Result only; the Architect
// reconciles the final SHA separately)" with the authoritative W3 SHA and appends the module-local
// W3-R1 recovery checkpoint. No other Story line and no repository-global line is rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'TOOBA-TMAR-MASTER-RECOVERY.md');
const raw = fs.readFileSync(file, 'utf8');
const NL = raw.includes('\r\n') ? '\r\n' : '\n';

const W3_FULL = '39ab324e9c57e342516202bdd8df95953dda6439';
const W3_SHORT = '39ab324e';

if (raw.includes('Story AMSC W3-R1 recovery reconciliation')) {
  throw new Error('W3-R1 reconciliation section already present — refusing to overwrite');
}

const before = '`TB-TMAR-STORY-AMSC-001-W3` Certify *(this commit, reported in the Bridge Result only; the Architect reconciles the final SHA separately)*';
if ((raw.split(before).length - 1) !== 1) {
  throw new Error('W3 self-reference anchor not found exactly once');
}
const after = `\`TB-TMAR-STORY-AMSC-001-W3\` Certify \`${W3_SHORT}\` (\`${W3_FULL}\`)`;

const section = [
  '',
  'Story AMSC W3-R1 recovery reconciliation (authoritative, module-local)',
  '',
  'Recorded by `TB-TMAR-STORY-AMSC-001-W3-R1` (bounded recovery / SoT reconciliation). The W3 certification commit could not contain its own SHA, so the W3 checkpoint recorded it as a self-reference; this wave records the authoritative value and closes the Story AMSC-001 chain.',
  `- Reconciled W3 SHA: \`${W3_SHORT}\` (\`${W3_FULL}\`); \`storyAmsc001W3.commit\` moved from \`PENDING_W3_COMMIT\` to the real SHA and \`storyAmsc001W3R1\` was appended (state \`STORY_AMSC_001_RECOVERY_RECONCILED\`, \`masterRecoveryW3ShaBefore = PLACEHOLDER_THIS_COMMIT\`, \`masterRecoveryW3ShaState = RECORDED_39AB324E\`).`,
  '- W3 self-description repair (truth only): `guardsAdded` 9 facts -> 8 facts and the focused-validation claim 9/9 -> 8/8, reconciled to the verified `StoryModuleAmsc001W3CertGuardTests` `[Fact]` count on disk. No assertion was removed and no guard was weakened.',
  '- Accepted lineage (final): `W0` Analyze `0c73390a` -> `W1` Migrate `2a09e7bb` -> `W2` Structure `4cd9a6cc` -> `W3` Certify `39ab324e` -> `W3-R1` Recovery *(this commit)*.',
  '- Production change: ZERO. Manifest structural state NOT_TOUCHED; schema/migrations UNCHANGED; frontend FROZEN_UNTOUCHED; guards weakened NONE; baselines widened NONE.',
  '- Historical AMC-001 lineage (`storyModuleAmc001` .. `storyModuleAmc001W6Cert`) is preserved verbatim as historical and is superseded for current Story authority by AMSC-001 W0->W3.',
  '- Global recovery lock preserved exactly: `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`, repository-global `workflowStop = USER_REVIEW_HOST_ROOT_FINAL_CERT_001`, `automaticNextImplementationTask = NONE`.',
  '- Evidence root: `docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3-R1/`.',
  '- Stop gate: `USER_REVIEW_STORY_AMSC_001_W3_R1`.',
  '- `automaticNextImplementationTask = NONE`.',
  '',
].join(NL);

let content = raw.replace(before, after);
if (!content.endsWith(NL)) {
  content += NL;
}
content += section;

fs.writeFileSync(file, content, 'utf8');

const check = fs.readFileSync(file, 'utf8');
if (!check.includes(`Certify \`${W3_SHORT}\``)) {
  throw new Error('W3 SHA not recorded in the lineage line');
}
if (!check.includes('Story AMSC W3-R1 recovery reconciliation')) {
  throw new Error('W3-R1 section missing after write');
}
// Scope the self-reference check to the Story lineage line only (sibling modules legitimately keep theirs).
const storyLine = check.split(/\r?\n/).find((line) => line.includes('TB-TMAR-STORY-AMSC-001-W3` Certify'));
if (!storyLine || storyLine.includes('the Architect reconciles the final SHA separately')) {
  throw new Error('Story W3 self-reference survived');
}
console.log('PATCHED docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (W3 SHA reconciled + W3-R1 checkpoint appended)');
