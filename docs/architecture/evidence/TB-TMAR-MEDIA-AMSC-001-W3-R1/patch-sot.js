const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let raw = fs.readFileSync(p, 'utf8');
const bom = raw.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
if (bom) raw = raw.slice(1);
const j = JSON.parse(raw);

// W3-R1 reconciliation: record the actual W3 final SHA instead of the placeholder.
j.mediaModuleAmsc001W3.commit = 'aa6cad925c1134481f4f264c94f4abf2244ae964';
j.mediaModuleAmsc001W3.commitShort = 'aa6cad92';

j.mediaModuleAmsc001W3R1 = {
  task: 'TB-TMAR-MEDIA-AMSC-001-W3-R1',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'recovery-reconciliation',
  target: 'src/backend/Modules/Media/Tooba.Media.*',
  parentTask: 'TB-TMAR-MEDIA-AMSC-001-W3',
  startingHead: 'aa6cad92',
  state: 'MEDIA_AMSC_001_RECOVERY_RECONCILED',
  verdict: 'COMPLETE_REFERENCE_PATTERN',
  lockVersion: 'ARCH-COMPLETE-002',
  productionCodeChanged: false,
  certifiedCommit: 'aa6cad925c1134481f4f264c94f4abf2244ae964',
  certifiedCommitShort: 'aa6cad92',
  masterRecoveryW3ShaBefore: 'PLACEHOLDER_THIS_COMMIT',
  masterRecoveryW3ShaState: 'RECORDED_AA6CAD92',
  acceptedLineage: {
    w0: '06f7de21',
    w1: '0b0fde0a',
    w2: '991551e9',
    w3: 'aa6cad92'
  },
  historicalAmcLineageState: 'HISTORICAL_SUPERSEDED_NOT_REWRITTEN',
  globalHostCheckpointState: 'PRESERVED',
  manifestStructuralState: 'NOT_TOUCHED',
  schemaMigrationState: 'NOT_TOUCHED',
  frontendState: 'FROZEN_UNTOUCHED',
  guardsWeakened: 0,
  stopGate: 'USER_REVIEW_MEDIA_AMSC_001_W3_R1',
  automaticNextImplementationTask: 'NONE',
  commit: 'PENDING',
  evidence: 'docs/architecture/evidence/TB-TMAR-MEDIA-AMSC-001-W3-R1/reconciliation.md'
};

fs.writeFileSync(p, bom + JSON.stringify(j, null, 2) + '\n');
console.log('W3-R1 SoT block written');
