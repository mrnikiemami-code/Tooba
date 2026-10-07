// TB-TMAR-PAYMENT-AMSC-001-W3-R3 — bounded validation (no builds, no tests, no production edits).
const fs = require('fs');
const path = require('path');
const cp = require('child_process');

function sh(cmd) {
  return cp.execSync(cmd, { encoding: 'utf8' }).trim();
}

const results = [];
function check(name, ok, detail) {
  results.push({ name, ok, detail });
}

// 1. JSON parse SoT
let sot;
try {
  sot = JSON.parse(fs.readFileSync('docs/architecture/tmar-current-state.json', 'utf8').replace(/^\uFEFF/, ''));
  check('SoT JSON parse', true);
} catch (e) {
  check('SoT JSON parse', false, e.message);
  console.log(JSON.stringify(results, null, 2));
  process.exit(1);
}

const HISTORICAL_W1 = 'CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED';

// 2. W1 historical field == original 3-consumed value
const w1 = sot.paymentAmsc001W1;
check('W1 historical field restored to 3-consumed', w1.stableErrorCodeState === HISTORICAL_W1, w1.stableErrorCodeState);

// 3. W1 reconciliation field exists and points to R2 truth
check('W1 reconciliation field exists', typeof w1.stableErrorCodeStateReconciliation === 'string' && w1.stableErrorCodeStateReconciliation.includes('HISTORICAL_W1_TEXT_SAID_3_CONSUMED'), w1.stableErrorCodeStateReconciliation);
check('W1 reconciliation points to R2 truth', w1.stableErrorCodeStateReconciliation.includes('AUTHORITATIVE_COUNT_IS_4_FOREIGN_DECLARED_CONSUMED') && w1.stableErrorCodeStateReconciliation.includes('paymentAmsc001W3R2.stableErrorTruthState'));

// 4. R2 28/27/24 + 4/3/admin-excluded unchanged
const r2 = sot.paymentAmsc001W3R2;
check('R2 declaredStableCodeCount == 28', r2.declaredStableCodeCount === 28, String(r2.declaredStableCodeCount));
check('R2 knownCodeGuardMemberCount == 27', r2.knownCodeGuardMemberCount === 27, String(r2.knownCodeGuardMemberCount));
check('R2 paymentOwnedDescriptorCount == 24', r2.paymentOwnedDescriptorCount === 24, String(r2.paymentOwnedDescriptorCount));
check('R2 foreignOwnedDeclaredConsumedCount == 4', r2.foreignOwnedDeclaredConsumedCount === 4, String(r2.foreignOwnedDeclaredConsumedCount));
check('R2 foreignOwnedKnownGuardCount == 3', r2.foreignOwnedKnownGuardCount === 3, String(r2.foreignOwnedKnownGuardCount));
check('R2 admin.authorization.denied excluded', r2.foreignOwnedNotInKnownGuard === 'admin.authorization.denied', r2.foreignOwnedNotInKnownGuard);
check('R2 stableErrorTruthState authoritative', r2.stableErrorTruthState === 'AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS', r2.stableErrorTruthState);

// 5. W3 authority unchanged
const w3 = sot.paymentAmsc001W3;
check('W3 verdict/lock unchanged', w3.verdict === 'COMPLETE_REFERENCE_PATTERN' && w3.lockVersion === 'ARCH-COMPLETE-002' && w3.structureCertified === true);
check('R2 certification authority unchanged', r2.currentCertificationAuthority === 'TB-TMAR-PAYMENT-AMSC-001-W3' && r2.currentCertifiedCommit === '502d73e0e9ccfb277a06ad397b1f0a511f586921');

// R3 closure block
const r3 = sot.paymentAmsc001W3R3;
check('R3 closure block present', !!r3 && r3.state === 'PAYMENT_AMSC_001_RECOVERY_FINAL_CLOSED');
check('R3 historical field recorded', r3 && r3.historicalW1StableErrorCodeState === HISTORICAL_W1);
check('R3 historicalW1FieldState restored', r3 && r3.historicalW1FieldState === 'RESTORED_AND_PRESERVED');
check('R3 additiveReconciliationState preserved', r3 && r3.additiveReconciliationState === 'PRESERVED');
check('R3 recovery authority R2', r3 && r3.recoveryAuthority === 'TB-TMAR-PAYMENT-AMSC-001-W3-R2' && r3.recoveryCommit === '83bc22175d92e53b838c3039cc09ee3e790c509c');
check('R3 certification authority W3', r3 && r3.currentCertificationAuthority === 'TB-TMAR-PAYMENT-AMSC-001-W3' && r3.currentCertifiedCommit === '502d73e0e9ccfb277a06ad397b1f0a511f586921');
check('R3 manifest/schema/host preserved', r3 && r3.manifestStructuralState === 'NOT_TOUCHED' && r3.schemaMigrationState === 'UNCHANGED' && r3.globalHostCheckpointState === 'PRESERVED');
check('R3 guards/baselines none', r3 && r3.guardsWeakened === 'NONE' && r3.baselinesWidened === 'NONE');
check('R3 automatic next NONE', r3 && r3.automaticNextImplementationTask === 'NONE');
check('R3 productionCodeChanged false', r3 && r3.productionCodeChanged === false);

// R1 lineage preserved
const r1 = sot.paymentAmsc001W3R1;
check('R1 lineage preserved', r1 && r1.state === 'PAYMENT_AMSC_001_RECOVERY_RECONCILED' && r1.commitFull === 'affdfba4fc5fce402d05e68131bdb4a01899b93c');
check('semantic wave SHAs unchanged', sot.paymentAmsc001W0.commitFull === '6839bb4a5f75a954817719386de04e36ff96c305' && sot.paymentAmsc001W1.commitFull === '2d69d82808178e786f0673dc725baeb238987829' && sot.paymentAmsc001W2.commitFull === 'a138ec61bcbbb719fed2949c60e21fa77104cfd1');
check('global host checkpoint preserved', sot.currentHostCheckpoint === 'HOST_ROOT_FINAL_CERTIFIED', sot.currentHostCheckpoint);

// starting HEAD / origin
const head = sh('git rev-parse HEAD');
const r2Parent = sh('git rev-list --parents -n 1 83bc22175d92e53b838c3039cc09ee3e790c509c').split(/\s+/)[1];
check('R2 parent == affdfba4', r2Parent === 'affdfba4fc5fce402d05e68131bdb4a01899b93c', r2Parent);

// 6. git diff scope proof
const baselinePath = path.join(__dirname, 'preexisting-unrelated-artifacts.txt');
const baseline = fs.existsSync(baselinePath)
  ? fs.readFileSync(baselinePath, 'utf8').replace(/^\uFEFF/, '').split('\n').map((l) => l.replace(/\r$/, '')).filter(Boolean)
  : [];
const baselineSet = new Set(baseline);
const changed = [
  ...sh('git diff --name-only HEAD').split('\n'),
  ...sh('git ls-files --others --exclude-standard').split('\n'),
].map((l) => l.trim().replace(/\\/g, '/')).filter(Boolean);
const allowed = [
  'docs/architecture/tmar-current-state.json',
  'docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md',
];
const unexpected = changed.filter((p) => {
  if (allowed.includes(p)) return false;
  if (p.startsWith('docs/architecture/evidence/TB-TMAR-PAYMENT-AMSC-001-W3-R3/')) return false;
  if (p === 'docs/ai/tasks/TB-TMAR-PAYMENT-AMSC-001-W3-R3.task.md') return false;
  if (baselineSet.has(p)) return false;
  return true;
});
check('no unexpected changed files', unexpected.length === 0, JSON.stringify(unexpected));
const touchedByR3 = changed.filter((p) => !baselineSet.has(p));
check('no production file changed', !touchedByR3.some((p) => /^src\/backend\/(Modules|Host)\/.*\.(cs|csproj)$/.test(p)), JSON.stringify(touchedByR3.filter((p) => /^src\/backend\//.test(p))));
check('no guard/manifest file changed', !touchedByR3.some((p) => /GuardTests\.cs$/.test(p) || p.endsWith('tmar-module-structure-manifests.json')), JSON.stringify(touchedByR3.filter((p) => /GuardTests\.cs$/.test(p) || p.endsWith('tmar-module-structure-manifests.json'))));

let failed = 0;
for (const r of results) {
  if (!r.ok) failed++;
  console.log((r.ok ? 'PASS ' : 'FAIL ') + r.name + (r.detail ? ' :: ' + r.detail : ''));
}
console.log('---');
console.log('TOTAL ' + results.length + ' FAILED ' + failed);
process.exit(failed === 0 ? 0 : 1);
