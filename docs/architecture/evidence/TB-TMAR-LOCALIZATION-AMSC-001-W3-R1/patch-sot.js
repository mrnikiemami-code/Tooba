const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let raw = fs.readFileSync(p, 'utf8');
const bom = raw.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
if (bom) raw = raw.slice(1);
const j = JSON.parse(raw);

// W3's own commit is now known; replace the placeholder without touching any other W3 field.
j.localizationModuleAmsc001W3.commit = 'c3d01dc9';

j.localizationModuleAmsc001W3R1 = {
  task: 'TB-TMAR-LOCALIZATION-AMSC-001-W3-R1',
  parentTask: 'TB-TMAR-LOCALIZATION-AMSC-001-W3',
  mode: 'RECOVERY_SOT_RECONCILIATION_ONLY',
  state: 'LOCALIZATION_AMSC_001_RECOVERY_RECONCILED',
  productionCodeChanged: false,
  certifiedCommit: 'c3d01dc90498bc73b58fe769729e7aa5f9e243a6',
  certifiedCommitShort: 'c3d01dc9',
  acceptedLineage: [
    'TB-TMAR-LOCALIZATION-AMSC-001-W0 Analyze 5a0b882b',
    'TB-TMAR-LOCALIZATION-AMSC-001-W1 Migrate 074fc3a4',
    'TB-TMAR-LOCALIZATION-AMSC-001-W2 Structure 6bc3ee2d',
    'TB-TMAR-LOCALIZATION-AMSC-001-W3 Certify c3d01dc9'
  ],
  masterRecoveryW3ShaBefore: 'PLACEHOLDER_THIS_COMMIT',
  masterRecoveryW3ShaState: 'RECORDED_C3D01DC9',
  historicalAmcLineageState: 'PRESERVED_HISTORICAL_NOT_REWRITTEN',
  globalHostCheckpointState: 'PRESERVED',
  manifestStructuralState: 'NOT_TOUCHED',
  guardsWeakened: 0,
  automaticNextImplementationTask: 'NONE',
  stopGate: 'USER_REVIEW_LOCALIZATION_AMSC_001_W3_R1',
  evidence: 'docs/architecture/evidence/TB-TMAR-LOCALIZATION-AMSC-001-W3-R1/reconciliation.md'
};

fs.writeFileSync(p, bom + JSON.stringify(j, null, 2) + '\n');
console.log('W3-R1 SoT reconciliation written');
